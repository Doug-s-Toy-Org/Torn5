﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torn
{
	/// <summary>
	/// This class models a fixture: a list of games, some or all of which are not yet played.
	/// For each game, it lists the teams involved in that game (and perhaps their colours).
	/// </summary>
	public class Fixture
	{
		public FixtureTeams Teams { get; set; }
		public FixtureGames Games { get; set; }

		public Fixture()
		{
			Teams = new FixtureTeams();
			Games = new FixtureGames();
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

	public class FixtureTeams: List<LeagueTeam>
	{
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

	public class FixtureGames: List<FixtureGame>
	{
		// A game will be a time or date/time, followed by a separator, followed
		// by a separated list of numbers, which are the teams in that game. e.g.:
		// 8:00	1	2	3
		public void Parse(string s, FixtureTeams teams, char separator = '\t', string colours = "1,2,3,4,5")
		{
			string[] lines = s.Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string line in lines)
			{
				FixtureGame fg = new FixtureGame();
				
				string[] fields = line.Split('\t');
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
							string[] colors = colours.Split(',');
							if (fields.Length <= 5)  // If there are five or less teams per game,
								fg.Teams.Add(ft, (Colour)Int32.Parse(colors[i-1]));  // assign colours to teams.
							else
								fg.Teams.Add(ft, Colour.None);
						}
					}

				Add(fg);
			}
		}

		// Import past games from a league.
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

		// This parses a grid. Each game will be a column; each team will be a row. Each character in the grid is a letter
		// representing that team's colour in that game, or is a non-colour character if that team does not play in that game.
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
				for (int col = 0; col < minLength; col ++)
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

		// This ToString() is used to persist fixtures to settings. 
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (FixtureGame fg in this)
			{
				sb.Append(fg.Time);
				sb.Append('\t');

				for (var i = Colour.Red; i <= Colour.White; i++)
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
	}

	public class FixtureGame: IComparable
	{
		public DateTime Time { get; set; }
		public Dictionary<LeagueTeam, Colour> Teams { get; set; }
		
		public FixtureGame()
		{
			Teams = new Dictionary<LeagueTeam, Colour>();
		}

		int IComparable.CompareTo(object obj)
		{
			return DateTime.Compare(this.Time, ((FixtureGame)obj).Time);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("FixtureGame: ");
			if (Time != default)
			{
				sb.Append(Time);
				sb.Append('\t');
			}

			for (var i = Colour.Red; i <= Colour.White; i++)
			{
				var ft = Teams.FirstOrDefault(x => x.Value == i).Key;
				if (ft != null)
				{
					if (ft.TeamId == -1)
						sb.Append(ft.Name);
					else
						sb.Append(ft.TeamId);
					sb.Append('\t');
				}
			}

			foreach (var kv in Teams.Where(t => t.Value == Colour.None))
			{
				if (kv.Key.TeamId == -1)
					sb.Append(kv.Key.Name);
				else
					sb.Append(kv.Key.TeamId);
				sb.Append('\t');
			}

			sb.Length--;

			return sb.ToString();
		}
	}
}
