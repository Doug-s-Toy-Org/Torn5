using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace Torn
{
	/// <summary>
	/// This class models a fixture: a list of games, some or all of which are not yet played.
	/// For each game, it lists the teams involved in that game (and perhaps their colours).
	/// </summary>
	public class Fixture
	{
		public string Title { get; set; }
		public FixtureTeams Teams { get; private set; }
		public FixtureGames Games { get; private set; }

		public Fixture()
		{
			Teams = new FixtureTeams();
			Games = new FixtureGames();
		}

		public Fixture Clone()
		{
			return new Fixture
			{
				Title = Title,
				Teams = Teams.Clone(),
				Games = Games.Clone()
			};
		}

		public void CopyFrom(Fixture source)
		{
			Title = source.Title;
			Teams = source.Teams.Clone();
			Games = source.Games.Clone();
		}

		public FixtureGame BestMatch(Game game, out double score)
		{
			score = 0;
			if (Games.Count == 0)
				return null;

			FixtureGame bestMatch = null;
			double bestScore = 0.0;
			var games = Games.OrderBy(fg => Math.Abs(fg.Time.Subtract(game.Time).TotalSeconds));

			foreach (var fg in games)
			{
				double thisScore = Match(game, fg);
				if (bestScore < thisScore)
				{
					bestScore = thisScore;
					bestMatch = fg;
				}
			}

			score = bestScore;
			return bestMatch ?? (games.Any() ? games.First() : null);
		}

		public FixtureGame BestMatch(Game game)
		{
			return BestMatch(game, out _);
		}

		/// <summary>Rate how well a game and a fixture game match. 0.0 is no match; 1.0 is perfect match.</summary>
		double Match(Game game, FixtureGame fg)
		{
			// Check how many fixture game teams are in the game.
			int a = 0;
			foreach (var kv in fg.Teams)
			{
				var ft = kv.Key;
				var matches = game.Teams.Where(gt => gt.TeamId == ft.TeamId);  // Should be 1 item in the collection if this fixture team is in this game. 
				if (matches.Any())
				{
					a++;
					if (matches.Any(y => y.Colour == kv.Value))
						a++;
				}
			}

			// Check how many game teams are in the fixture game.
			int b = 0;
			foreach (var team in game.Teams)
				foreach (var kv in fg.Teams)
					if (team.TeamId == kv.Key.TeamId)
					{
						b++;
						if (team.Colour == kv.Value)
							b++;
					}

			return 0.25 * a / fg.Teams.Count + 0.25 * b / game.Teams.Count;
		}
	}

	public class FixtureTeams : List<LeagueTeam>
	{
		/// <summary>Shallow clone: does not make copies of the underlying league teams.</summary>
		public FixtureTeams Clone()
		{
			var clone = new FixtureTeams();
			clone.AddRange(this);
			return clone;
		}

		/// <summary>Ensure all the league's teams are present in the list of fixture teams.</summary>
		public void Populate(List<LeagueTeam> teams)
		{
			foreach (LeagueTeam lt in teams)
				if (!Exists(ft => ft == lt))
					Add(lt);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (LeagueTeam ft in this)
			{
				sb.Append(ft.Name);
				sb.Append("\r\n");
			}
			if (sb.Length > 0)
				sb.Length -= 2;
			return sb.ToString();
		}
	}

	public class FixtureGames : List<FixtureGame>
	{
		public FixtureGames Clone()
		{
			var clone = new FixtureGames();
			clone.AddRange(this.Select(fg => fg.Clone()));
			return clone;
		}

		/// <summary>Parse string s into FixtureGames.
		/// A game will be a time or date/time, followed by a separator, followed by a separated list of numbers, which are the teams in that game. e.g.:
		/// 8:00	1	2	3</summary>
		public void Parse(string s, FixtureTeams teams, char separator = '\t')
		{
			var bestColours = ColourExtensions.BestFirst();
			string[] lines = s.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string line in lines)
			{
				FixtureGame fg = new FixtureGame();

				string[] fields = line.Split(separator);
				fg.Time = DateTime.Parse(fields[0]);
				for (int i = 1; i < fields.Length; i++)
					if (!string.IsNullOrEmpty(fields[i]))
					{
						LeagueTeam ft;
						if (int.TryParse(fields[i], out int teamnum))
						{
							ft = teams.Find(t => t.TeamId == teamnum);
						}
						else
							ft = teams.Find(x => x.Name == fields[i]);

						if (ft == null)
							ft = new LeagueTeam
							{
								Name = "Team " + fields[i]
							};

						if (!fg.Teams.ContainsKey(ft))
						{
							if (fields.Length <= 6)  // If there are five or less teams per game,
								fg.Teams.Add(ft, bestColours[i - 1]);  // assign colours to teams.
							else
								fg.Teams.Add(ft, Colour.None);
						}
					}

				Add(fg);
			}
		}

		/// <summary>Import past games from a league.</summary>
		public void Parse(League league, FixtureTeams teams)
		{
			foreach (Game lg in league.Games(false))
			{
				FixtureGame fg = new FixtureGame
				{
					Time = lg.Time
				};

				foreach (GameTeam gt in lg.Teams)
				{
					LeagueTeam ft = teams.Find(x => x == league.LeagueTeam(gt));
					if (ft != null && !fg.Teams.ContainsKey(ft))
						fg.Teams.Add(ft, gt.Colour);
				}

				Add(fg);
			}
		}

		/// <summary>Parses a grid. Each game will be a column; each team will be a row. Each character in the grid is a letter
		/// representing that team's colour in that game, or is a non-colour character if that team does not play in that game.</summary>
		public string[] Parse(string[] lines, FixtureTeams teams, DateTime? firstGame, TimeSpan? duration)
		{
			int minLength = int.MaxValue;

			for (int row = 0; row < lines.Length && row < teams.Count; row++)
			{
				int pos = lines[row].LastIndexOf('\t');
				while (pos > -1)
				{
					lines[row] = lines[row].Remove(pos, 1);
					pos = lines[row].LastIndexOf('\t');
				}
				minLength = Math.Min(minLength, lines[row].Length);
			}

			if (minLength < int.MaxValue)
				for (int col = 0; col < minLength; col++)
				{
					var game = new FixtureGame();
					for (int row = 0; row < lines.Length && row < teams.Count; row++)
					{
						Colour colour = ColourExtensions.ToColour(lines[row][col]);
						if (colour != Colour.None)
							game.Teams.Add(teams[row], colour);
						else if (colour == Colour.None && char.IsLetter(lines[row][col]))
							game.Teams.Add(teams[row], Colour.None);
					}
					if (firstGame != null)
					{
						game.Time = (DateTime)firstGame;
						firstGame += duration ?? TimeSpan.Zero;
					}
					Add(game);
				}

			if (firstGame != null)
				Sort();

			return lines;
		}

		/// <summary>Creates the format expected by Parse(string, teams, separator), above.</summary>
		/// Each game will be a date/time, followed by a tab, followed by a tab-separated list of teamId's in that game. e.g.:
		/// 8:00	1	2	3</summary>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (FixtureGame fg in this)
			{
				sb.Append(fg.Time);
				sb.Append('\t');

				for (var i = Colour.Red; i <= Colour.Referee; i++)
				{
					var ft = fg.Teams.FirstOrDefault(x => x.Value == i).Key;
					if (ft != null)
					{
						if (ft.TeamId == -1)  // If a team exists in FixtureTeams but not in League.Teams it may never have had a TeamId set.
							sb.Append(ft.Name);
						else
							sb.Append(ft.TeamId);
						sb.Append('\t');
					}
				}
				foreach (var kv in fg.Teams.Where(t => t.Value == Colour.None))
				{
					if (kv.Key.TeamId == -1)
						sb.Append(kv.Key.Name);
					else
						sb.Append(kv.Key.TeamId);
					sb.Append('\t');
				}

				sb.Length--;
				sb.Append("\r\n");
			}

			return sb.ToString();
		}

		/// <summary>Creates the format expected by Parse(string[], teams, firstGame, duration), above. Each game will be a column; each team will be a row.</summary>
		public string[] ToGrid(FixtureTeams teams)
		{
			int teamsCount = Math.Max(teams.Count, this.Count == 0 ? 0 : this.Max(fg => fg.Teams.Count == 0 ? 0 : fg.Teams.Max(ft => ft.Key.TeamId)));
			var lines = new string[teamsCount];

			for (int col = 0; col < Count; col++)
			{
				var fg = this[col];
				for (int row = 0; row < teamsCount; row++)
				{
					if (lines[row] == null)
						lines[row] = "";

					if (row < teams.Count && fg.Teams.ContainsKey(teams[row]))
						lines[row] += fg.Teams[teams[row]].ToChar();
					else
						lines[row] += '.';
				}
			}
			return lines;
		}

		public void ToXml(XmlDocument doc, XmlNode node)
		{
			XmlNode fixtureGamesNode = doc.CreateElement("fixturegames");
			node.AppendChild(fixtureGamesNode);

			foreach (var fixtureGame in this)
				fixtureGame.ToXml(doc, fixtureGamesNode);
		}

		public void FromXml(XmlNode node, List<LeagueTeam> teams)
		{
			foreach (XmlNode x in node.ChildNodes)
			{
				var fixtureGame = new FixtureGame();
				fixtureGame.FromXml(x, teams);
				Add(fixtureGame);
			}
		}
	}

	public class FixtureGame : IComparable
	{
		public DateTime Time { get; set; }
		public Dictionary<LeagueTeam, Colour> Teams { get; set; }

		public FixtureGame()
		{
			Teams = new Dictionary<LeagueTeam, Colour>();
		}

		int IComparable.CompareTo(object obj)
		{
			return DateTime.Compare(Time, ((FixtureGame)obj).Time);
		}

		public FixtureGame Clone()
		{
			return new FixtureGame
			{
				Time = Time,
				Teams = new Dictionary<LeagueTeam, Colour>(Teams)
			};
		}

		public void ToXml(XmlDocument doc, XmlNode node)
		{
			XmlNode fixtureGameNode = doc.CreateElement("fixturegame");
			node.AppendChild(fixtureGameNode);
			doc.AppendNode(fixtureGameNode, "time", Time.ToString("yyyy-MM-dd HH:mm"));

			foreach (var kv in Teams.ToList().OrderBy(kv => kv.Value))
			{
				XmlNode teamNode = doc.CreateElement("team");
				fixtureGameNode.AppendChild(teamNode);

				if (kv.Key.TeamId == -1)
					doc.AppendNode(teamNode, "teamname", kv.Key.Name);
				else
					doc.AppendNode(teamNode, "teamid", kv.Key.TeamId);

				doc.AppendNode(teamNode, "colour", kv.Value.ToString());
			}
		}

		public void FromXml(XmlNode node, List<LeagueTeam> teams)
		{
			var	time = node.SelectSingleNode("time");
			Time = DateTime.Parse(time.InnerText, CultureInfo.InvariantCulture);

			int nextTeamId = 1;
			var	xteams = node.SelectNodes("team");

			foreach (XmlNode xteam in xteams)
			{
				var leagueTeam = new LeagueTeam();

				var xTeamId = xteam.SelectSingleNode("teamid");
				if (xTeamId != null)
				{
					var teamId = int.Parse(xTeamId.InnerText, CultureInfo.InvariantCulture);
					leagueTeam = teams.FirstOrDefault(lt => lt.TeamId == teamId);
				}

				if (leagueTeam == null)
				{
					var xTeamName = xteam.SelectSingleNode("teamname");
					leagueTeam = xTeamName == null ? new LeagueTeam() { Name = xTeamName.InnerText } : teams.FirstOrDefault(lt => lt.Name == xTeamName.InnerText);
				}

				if (leagueTeam == null)
				{
					while (teams.Exists(lt => lt.TeamId == nextTeamId ))
						nextTeamId++;

					leagueTeam = new LeagueTeam() { TeamId = nextTeamId, Name = "New Team " + nextTeamId.ToString() };
				}

				Teams.Add(leagueTeam, ColourExtensions.ToColour(xteam.GetString("colour")));
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("FixtureGame: ");
			if (Time != default)
			{
				sb.Append(Time);
				sb.Append(", ");
			}

			var pairs = Teams.ToList().OrderBy(t => t.Value);

			foreach (var pair in pairs.Where(t => t.Value != Colour.None))
			{
				sb.Append(pair.Key.Name);
				sb.Append(", ");
			}

			foreach (var pair in pairs.Where(t => t.Value == Colour.None))
			{
				sb.Append(pair.Key.Name);
				sb.Append(", ");
			}

			sb.Length -= 2;

			return sb.ToString();
		}
	}
}
