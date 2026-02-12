using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torn.Grids
{
	/// <summary>Create fixtures experimentally via hill-climbing.</summary>
	class GridFinder
	{
		public readonly List<Colour> Colours = new List<Colour>();
		public readonly ScoreScalers ScoreScalers = new ScoreScalers();
		public ShuffleType ShuffleType = ShuffleType.Referees | ShuffleType.Games | ShuffleType.WithinGames;
		public Sessions Sessions = new Sessions();
		public bool Rings;
		public bool Changed = false;
		public string Details;

		private Plays existingPlays;
		private Improver oneImprover;  // Used for non-threaded non-parallel testing.
		private GamesResult best = new GamesResult() { Score = double.MaxValue };
		private readonly StringBuilder errors = new StringBuilder();
		private bool hasExistingGames;

		/// <summary>Set up from fresh or add onto existing.</summary>
		public void Setup(League league, Fixture fixture, List<LeagueTeam> teams, int gamesPerTeam, int refereesPerGame)
		{
			fixture.Teams.Clear();
			fixture.Teams.Populate(teams);

			existingPlays = ScoreScalers.PastTimesPlayed ? new Plays(league, teams) : new Plays(0);
			hasExistingGames = league.Games().Any();

			if (!Colours.Any(c => c != Colour.Referee))
				Colours.InsertRange(0, new List<Colour> { Colour.Red, Colour.Blue, Colour.Green });

			int teamsPerGame = Colours.Count(c => c != Colour.Referee);
			int games = (int)Math.Ceiling(1.0 * teams.Count * gamesPerTeam / teamsPerGame);

			best.Score = double.MaxValue;
			if (best.PlayGames == null || best.RefGames == null || oneImprover == null)  // Initialise.
			{
				var setupPlayGames = new SetupGames() { Teams = teams, GameCount = games, TeamsPerGame = teamsPerGame };
				best.PlayGames = ScoreScalers.SameDifficulty >= ScoreScalers.CascadeDifficulty ? setupPlayGames.Setup() : setupPlayGames.SetupCascade();

				best.RefGames = new SetupGames() { Teams = teams, GameCount = games, TeamsPerGame = refereesPerGame }.Setup();

				oneImprover = new Improver(best.PlayGames, ScoreScalers) { RefGames = best.RefGames, ExistingPlays = existingPlays, Sessions = Sessions, Colours = Colours, ShuffleType = ShuffleType };
			}
			else  // Append to existing games.
			{
				oneImprover.Sessions = Sessions;
				oneImprover.Colours = Colours;
				oneImprover.ShuffleType = ShuffleType;

				if (games > best.PlayGames.Count)
					new SetupGames() { Games = best.PlayGames, Teams = teams, GameCount = games, TeamsPerGame = teamsPerGame }.Append();

				if (games > best.RefGames.Count)
					new SetupGames() { Games = best.RefGames, Teams = teams, GameCount = games, TeamsPerGame = refereesPerGame }.Append();

				// Add/remove referee teams to/from games if required.
				int teamIndex = 0;
				bool changed = false;

				foreach (var game in best.RefGames.Where(g => g.Count != refereesPerGame))
				{
					while (game.Count < refereesPerGame)
					{
						game.Add(teamIndex++);
						if (teamIndex >= teams.Count)
							teamIndex = 0;
						changed = true;
					}

					if (game.Count > refereesPerGame)
					{
						game.RemoveRange(refereesPerGame, game.Count - refereesPerGame);
						changed = true;
					}
				}

				if (changed)
					oneImprover.RefGames = best.RefGames;
			}

			ToFixtureGames(best, fixture);
		}

		/// <summary>Used when improving Ring fixtures.</summary>
		public void SetupFromFixture(Fixture fixture)
		{
			best.PlayGames = new IndexGames();
			best.RefGames = new IndexGames();
			existingPlays = new Plays(0);

			if (!fixture.Games.Any())
				return;

			// Make a dictionary from TeamId's to indexes. Necessary because TeamId's start from 1 and may not be contiguous; but indexes must start from 0 and be contiguous.
			var teamsInOrder = new Dictionary<int, int>();  // Key: teamID's from fixture. Value: new index into what will be new IndexGames's.
			foreach (var team in fixture.Teams)
				if (!teamsInOrder.ContainsKey(team.TeamId))
					teamsInOrder.Add(team.TeamId, teamsInOrder.Count);

			foreach (var fg in fixture.Games)
			{
				var plays = new IndexGame();
				var refs = new IndexGame();
				var teamColours = fg.Teams.OrderBy(kv => kv.Value).ToList();
				foreach (var kv in teamColours)
				{
					if (kv.Value == Colour.Referee)
						refs.Add(teamsInOrder[kv.Key.TeamId]);
					else
						plays.Add(teamsInOrder[kv.Key.TeamId]);
				}

				best.PlayGames.Add(plays);
				best.RefGames.Add(refs);
			}

			best.Score = double.MaxValue;
			oneImprover = new Improver(best.PlayGames, ScoreScalers) { RefGames = best.RefGames, ExistingPlays = new Plays(0), Sessions = Sessions, Colours = Colours, ShuffleType = ShuffleType, Rings = Rings };

			ToFixtureGames(best, fixture);
			var result = oneImprover.Scored();
			Details = (result.ScoreString + errors.ToString() + result.Details);
		}

		/// <summary>Improve a team fixture via hill-climbing.</summary>
		public void Improve(Fixture fixture)
		{
			if (ShuffleType != ShuffleType.None)
				Improved(oneImprover.Improve(), fixture);
		}

		/// <summary>Improve team fixtures via hill-climbing, in parallel.</summary>
		public void ImproveParallel(Fixture fixture)
		{
			if (ShuffleType == ShuffleType.None)
				return;

			var numTasks = Math.Max(Environment.ProcessorCount - 1, 1);
			var tasks = new Task<GamesResult>[numTasks];
			var results = new List<GamesResult>();

			for (int i = 0; i < tasks.Length; i++)
			{
				tasks[i] = Task<GamesResult>.Factory.StartNew((Object obj) => {
					Bundle bundle = obj as Bundle;
					if (bundle == null)
						return null;

					return Improver.Improve(bundle);
                },
                new Bundle() { PlayGames = best.PlayGames.Clone(), RefGames = best.RefGames.Clone(), ScoreScalers = ScoreScalers, ExistingPlays = existingPlays, Sessions = Sessions, Colours = Colours, ShuffleType = ShuffleType, Rings = Rings } );
			}

			Task.WaitAll(tasks);

			for (int i = 0; i < tasks.Length; i++)
				if (tasks[i].Result != null)
					results.Add(tasks[i].Result);

			if (results.Any(r => r != null))
			{
				var bestScore = results.Min(r => r == null ? double.MaxValue : r.Score);
				var bestResult = results.FirstOrDefault(r => r?.Score == bestScore);
				if (best.Score >= bestResult.Score)
					Improved(bestResult, fixture);
			}
		}

		private void Improved(GamesResult result, Fixture fixture)
		{
			if (result == null)
				return;

			best = result;
			ToFixtureGames(result, fixture);
			Details = result.ScoreString + errors.ToString() + result.Details;
			Changed = true;
		}

		/// <summary>Convert IndexGames to FixtureGames.</summary>
		/// <param name="result">Indexes of teams playing and refereeing in each game. Each integer in these is an index into fixture.Teams.</param>
		/// <param name="fixture">Each game in playGames and refGames will be appended into this fixture.</param>
		private void ToFixtureGames(GamesResult result, Fixture fixture)
		{
			fixture.Games.Clear();
			errors.Clear();

			var playGames = result.PlayGames;
			var refGames = result.RefGames;

			// If we don't care about the past, and we don't care about difficulty, we can tidy so that teams each play their first game in order.
			if (!(hasExistingGames && ScoreScalers.PastTimesPlayed) && ScoreScalers.SameDifficulty == 0 && ScoreScalers.CascadeDifficulty == 0)
			{
				var indexesInOrder = new List<int>();
				AddIndexesInOrderFromGames(playGames, indexesInOrder);
				AddIndexesInOrderFromGames(refGames, indexesInOrder);

				if (Rings && refGames.Any(g => g.Count > 0))
				{
					// Translate teamIndexes, but ensure that players who are marked as not refereeing (i.e. their team name ends in '*') get assigned to a non-refereeing slot (and everyone else gets assigned a refereeing slot).
					// In the degenerate case where no teams end in '*', this produces the same output as the 'else' case below.

					var canRef = fixture.Teams.Select(t => t.Name.EndsWith("*") ? 0 : 1).ToList();
					var canRefOrNot = new List<int>[2];  // Array from 0 to 1.
					canRefOrNot[0] = new List<int>();  // First element of the array: list of team indexes that can't referee.
					canRefOrNot[1] = new List<int>();  // Second element of the array: list of team indexes that can referee.

					for (int i = 0; i < fixture.Teams.Count; i++)
						canRefOrNot[canRef[i]].Add(i);

					var indexOrder = new Dictionary<int, int>();  // Key: teamIndex from old playGames and refGames. Value: new index into what will be new IndexGames's.
					var nexts = new int[2];  // Indexes into canRefOrNot[0] and canRefOrNot[1].
					for (int i = 0; i < indexesInOrder.Count; i++)
					{
						var thisTeamCanRef = canRef[indexesInOrder[i]];
						indexOrder.Add(indexesInOrder[i], canRefOrNot[thisTeamCanRef][nexts[thisTeamCanRef]++]);
					}

					TranslateGames(playGames, indexOrder);
					TranslateGames(refGames, indexOrder);
				}
				else
				{
					// Translate teamIndexes so that the first game contains team indexes 0, 1, 2, etc.
					var indexOrder = new Dictionary<int, int>();  // Key: teamIndex from old playGames and refGames. Value: new index into what will be new IndexGames's.
					for (int i = 0; i < indexesInOrder.Count; i++)
						indexOrder.Add(indexesInOrder[i], i);

					TranslateGames(playGames, indexOrder);
					TranslateGames(refGames, indexOrder);
				}
			}

			var time = Sessions.First().Start;
			int session = 0;
			for (int g = 0; g < playGames.Count; g++)
			{
				if (g > Sessions[session].Last && session < Sessions.Count - 1)
				{
					session++;
					time = Sessions[session].Start;
				}

				var fg = new FixtureGame { Time = time };

				if (playGames[g].Count > Colours.Count(c => c != Colour.Referee))
					Error("Too many teams ({0}) in game {1}. Oh no! ", playGames[g].Count, time);
				else
					for (int t = 0; t < playGames[g].Count; t++)
					{
						var teamIndex = playGames[g][t];
						if (!fixture.Teams.Valid(teamIndex))
							Error("Invalid team index in game {0}, spot {1}, value {2}! Oh no! ", g, t, teamIndex);
						else
						{
							LeagueTeam team = fixture.Teams[teamIndex];

							if (fg.Teams.ContainsKey(team))
								Error("Team {0} (TeamId {1}, Colour {2}) is already in game {3}. Oh no! Try increasing Tuning ▸ Play Self. ", team.ToString(), team.TeamId, Colours[t], time);
							else
								fg.Teams.Add(team, Colours[t]);
						}
					}

				for (int r = 0; r < refGames[g].Count; r++)
				{
					var teamIndex = refGames[g][r];
					if (!fixture.Teams.Valid(teamIndex))
						Error("Invalid team index reffing game {0}, spot {1}, value {2}! Oh no!", g, r, teamIndex);
					else
					{
						LeagueTeam team = fixture.Teams[teamIndex];

						if (fg.Teams.ContainsKey(team))
							Error("Team {0} (TeamId {1}, reffing) is already in game {2}. Oh no! Try increasing Tuning ▸ Play Self. ", team.ToString(), team.TeamId, time);
						else
							fg.Teams.Add(team, Colour.Referee);
					}
				}

				fixture.Games.Add(fg);
				time = time.Add(Sessions[session].Between);
			}

			if (errors.Length > 0)
			{
				errors.Length--;
				errors.Append("\r\n");
			}
		}

		private void AddIndexesInOrderFromGames(IndexGames games, List<int> teamsInOrder)
		{
			foreach (var game in games)
				foreach (var teamIndex in game)
					if (!teamsInOrder.Contains(teamIndex))
						teamsInOrder.Add(teamIndex);
		}

		private void TranslateGames(IndexGames games, Dictionary<int, int> teamsInOrder)
		{
			foreach (var game in games)
				for (int i = 0; i < game.Count; i++)
					game[i] = teamsInOrder[game[i]];
		}

		private void Error(string format, params object[] arg)
		{
			string s = string.Format(format, arg);
			Console.WriteLine(s, arg);
			errors.Append(s);
		}
	}

	/// <summary>Fill IndexGames with the appropriate number of games, teams, teams per game, games per team.</summary>
	class SetupGames
	{
		public List<LeagueTeam> Teams;
		/// <summary>Desired number of games.</summary>
		public int GameCount;
		public int TeamsPerGame;
		public IndexGames Games;

		/// <summary>Create IndexGames as a starting point. They may be very suboptimal, but we'll improve later.</summary>
		public IndexGames Setup()
		{
			if (Games == null)
				Games = new IndexGames();

			var gamesPerTeam = 1.0 * GameCount * TeamsPerGame / Teams.Count;

			if (TeamsPerGame == 4 && gamesPerTeam == 4)
				MarkN(0, 1, 4, 6);

			else if (TeamsPerGame != 3)
				Append();

			// Groups of digits in comments below are distances; but calls to Mark() show marks.
			// So comment "123" matches up with Mark(1, 3) which is Golomb ruler { 0 1 3 },
			// having marks at 0, 1 and 3; and having distances 0 - 1 = 1, 3 - 1 = 2, and 3 - 0 = 3.

			// These cases are for partial round robins, where we play every other team 0 or 1 times:
			else if (Teams.Count >= 7 && GameCount <= Teams.Count) // 7-team grid: 123.
				Mark3(1, 3);
			else if (Teams.Count >= 15 && GameCount == Teams.Count * 2)  // 13-team grid: 134, 257 (is missing 6)
				Mark3s(1, 4, 2, 7);
			else if (Teams.Count >= 21 && GameCount == Teams.Count * 3)  // 19-team grid: 145, 268, 37a (is missing 9)
				Mark3s(1, 5, 2, 8, 3, 10);
			else if (Teams.Count >= 25 && GameCount == Teams.Count * 4)  // 25-team grid: 156, 28a, 39c, 47b
				Mark3s(1, 6, 2, 10, 3, 12, 4, 11);
			else if (Teams.Count >= 31 && GameCount == Teams.Count * 5)  // 31-team grid: 156, 2ac, 3be, 49d, 78f
				Mark3s(1, 6, 2, 12, 3, 14, 4, 13, 7, 15);

			// These cases are for a variety of complete round-robins.
			else if (Teams.Count == 5 && GameCount <= Teams.Count * 2)  // 5-team grid: 112, 122
				Mark3s(1, 2, 1, 3);
			else if (Teams.Count == 6 && GameCount <= 10)  // 6-team grid: 123, half 112, one sixth 222
			{
				Mark3(1, 3);  // 123
				Game(1, 2, 3);  // 112
				Game(3, 4, 5);  // 112
				Game(5, 1, 0);  // 112
				Game(2, 0, 4);  // 222
			}
			else if (Teams.Count == 7 && GameCount <= Teams.Count * 2)  // 7-team grid: 123, 123
				Mark3s(1, 3, -3, -1);
			else if (Teams.Count == 8 && GameCount <= Teams.Count * 6)  // 7-team grid: 112, 123, 123, 134, 134, 224, 233
				Mark3s(1, 2, 1, 3, 1, 4, 2, 4, 2, 5, 1, 3, 1, 4);
			//  else if (Teams.Count <= 10&& Games <= Teams.Count * 3)  // 10-team grid: 123, half 134, half 145, half 235, half 244
			//
			else if (Teams.Count <= 11 && GameCount <= Teams.Count * 5)  // 11-team grid: 123, 134, 145, 235, 245
				Mark3s(1, 3, -1, 3, 2, 5, 1, 5, 2, -5);
			else if (Teams.Count == 12 && GameCount <= 28)   // imperfect 12-team grid: 123, 156, 4
				Mark3s(1, 3, 1, 6, 4, 8);
			else if (Teams.Count == 12 && GameCount <= 44)  // 12-team grid: 123, 156, 235, 44
				Mark3s(1, 3, 1, 6, 2, 5, 4, 8);
			else if (Teams.Count <= 13 && GameCount <= Teams.Count * 2)  // 13-team grid: 134, 256
				Mark3s(1, 4, 2, -6);
			else if (Teams.Count <= 13 && GameCount <= Teams.Count * 4)  // 13-team grid: 134, 156, 235, 246
				Mark3s(1, 4, 1, 6, 2, 5, 2, 6);
			else if (Teams.Count <= 15 && GameCount <= Teams.Count * 3)  // 15-team grid: 134, 267, 5
				Mark3s(1, 4, 2, 8, 5, 10);
			else if (Teams.Count <= 19 && GameCount <= Teams.Count * 3)  // 19-team grid: 134, 279, 568
				Mark3s(1, 4, 2, 9, 5, 11);
			else if (Teams.Count <= 19 && GameCount <= Teams.Count * 5)  // 19-team grid: 112, 235, 347, 469, 568,
				Mark3s(1, 2, 2, 5, 3, 7, 4, 10, 5, 11);
			else if (Teams.Count <= 20 && GameCount <= Teams.Count * 4)  // 20-team grid: 112, 257, 389, 46a
				Mark3s(1, 2, 2, 7, 3, 11, 4, 10);
			else if (Teams.Count <= 20 && GameCount <= Teams.Count * 5)  // 20-team grid: 112, 268, 347, 369, 55a
				Mark3s(1, 2, 2, 8, 3, 7, 3, 9, 5, 10);
			else if (Teams.Count == 21 && GameCount == 70)  // 21-team grid: 145, 28a, 369, 7
				Mark3s(1, 5, 2, 10, 3, 9, 7, 14);
			else

			if (Teams.Count == 24 && GameCount == 88)  // 24-team grid: 1ab, 369, 57c, 48.  Imperfect: we're missing a '2', and the 'c' is effectively doubled because this is an even number.  Only solution would be to add more rulers.
			{
				Mark3s(1, 11, 3, 9, 5, 12);
				for (int i = 0; i < 4; i++)  // 448
					Game(i + 0, i + 4, i + 8);
				for (int i = 0; i < 4; i++)  // 448
					Game(i + 8, i + 12, i + 16);
				for (int i = 0; i < 4; i++)  // 448
					Game(i + 20, i + 16, i + 0);
				for (int i = 0; i < 4; i++)  // 8
					Game(i + 4, i + 20, i + 12);
			}

			else if (Teams.Count <= 24 && GameCount <= Teams.Count * 4)  // 24-team grid: 123, 48c, 59a, 67b.
				Mark3s(1, 3, 4, 12, 5, 14, 6, 13);
			else if (Teams.Count == 25 && GameCount <= Teams.Count * 4)  // 25-team grid: 123, 47b, 58c, 69a.
				Mark3s(1, 3, 4, 11, 5, 13, 6, 15);
			else if (Teams.Count == 26 && GameCount <= 130)  // 26-team grid: 167, 2bd, 39c, 448, 55a.
				Mark3s(1, 7, 2, 13, 3, 12, 4, 8, 5, 10);
			else if (Teams.Count == 27 && GameCount <= 117)  // 27-team grid: 123, 47b, 5ac, 68d, 9.
				Mark3s(1, 3, 4, 11, 5, 15, 6, 14, 9, 18);
			else if (Teams.Count == 28 && GameCount <= 140)  // 28-team grid: 112, 369, 4ae, 5bc, 78d.
				Mark3s(1, 2, 3, 9, 4, 14, 5, 16, 7, 15);
			else if (Teams.Count == 29 && GameCount <= 145)  // 29-team grid: 1ee, 235, 47b, 6ad, 89c.
				Mark3s(1, 15, 2, 5, 4, 11, 6, 16, 8, 17);
			else if (Teams.Count == 30 && GameCount <= 150)  // 30-team grid: 123, 48c, 5be, 69f, 7ad.
				Mark3s(1, 3, 4, 12, 5, 16, 6, 15, 7, 17);
			else if (14 <= Teams.Count && Teams.Count <= 31 && GameCount <= 155)  // 31-team grid: 123, 47b, 5af, 6cd, 89e.
				Mark3s(1, 3, 4, 11, 5, 15, 6, -13, 8, 17);
			else if (16 <= Teams.Count && Teams.Count <= 32 && GameCount <= 192)  // 32-team grid: 123, 459, 6bf, 7cd, 8ae, 88g.
				Mark3s(1, 3, 4, 9, 6, -15, 7, -13, 8, -14, 8, 16);
			else if (16 <= Teams.Count && Teams.Count <= 33 && GameCount <= 176)  // 33-team grid: 123, 46a, 5df, 7ce, 89g, b.
				Mark3s(1, 3, 6, 10, 5, -15, 7, -14, 8, 17, 11, 22);

			else  // This is the fall-through case: just list every team gamesPerTeam times in a simple 112 way.
				Append();

			Games.ClearCache();
			return Games;
		}

		public IndexGames SetupCascade()
		{
			Games = new IndexGames();
			var gamesPerTeam = 1.0 * GameCount * TeamsPerGame / Teams.Count;

			if (gamesPerTeam == 6 && Teams.Count > 6)  // Classic cascade
			{
				// Build the 5-game head of the cascade.
				Game(2, 4, 0);
				Game(3, 1, 5);
				Game(4, 0, 6);
				Game(0, 1, 2);
				Game(1, 0, 3);

				// Build the 2-game repeating middle section.
				for (int i = 0; i < GameCount / 2 - 7; i++)
				{
					Game(i + 5, i + 2, i + 0);  // 235

					if (i % 3 == 0)
						Game(i + 0, i + 7, i + 1);  // 167
					else
						Game(i + 0, i + 1, i + 7); // 167 (different colour arrangement)
				}

				// Build the 9-game tail.
				Game(Teams.Count - 2, Teams.Count - 5, Teams.Count - 7);
				Game(Teams.Count - 7, Teams.Count - 2, Teams.Count - 3);
				Game(Teams.Count - 1, Teams.Count - 4, Teams.Count - 6);
				Game(Teams.Count - 2, Teams.Count - 3, Teams.Count - 1);
				Game(Teams.Count - 1, Teams.Count - 6, Teams.Count - 5);
				Game(Teams.Count - 4, Teams.Count - 1, Teams.Count - 2);
				Game(Teams.Count - 5, Teams.Count - 3, Teams.Count - 4);
				Game(Teams.Count - 6, Teams.Count - 4, Teams.Count - 2);
				Game(Teams.Count - 3, Teams.Count - 1, Teams.Count - 5);
			}

			else if (gamesPerTeam % 3 == 0 && Teams.Count >= 6)
			{
				int remainder = Teams.Count % 3 + 3;
				int repeats = (int)gamesPerTeam / 3;
				if (remainder == 4)  // Make a starting block where 4 teams play each other.
					for (int i = 0; i < repeats; i++)
					{
						Game(0, 1, 3);
						Game(1, 2, 0);
						Game(2, 4, 1);
						Game(3, 0, 2);
					}
				else if (remainder == 5)  // Make a starting block where 5 teams play each other.
					for (int i = 0; i < repeats; i++)
					{
						Game(0, 1, 2);
						Game(1, 2, 3);
						Game(2, 3, 5);
						Game(3, 4, 0);
						Game(4, 0, 1);
					}
				else
					remainder = 0;  // It's a multiple of 3: no starting block needed.

				// Fill in the rest with groups of 3 teams playing each other.
				for (int i = 0; i < repeats; i++)
					for (int j = remainder; j < Teams.Count; j += 3)
					{
						Game(j + 0, j + 1, j + 2);
						Game(j - 1, j + 3, j + 1);
						Game(j + 1, j + 2, j + 0);
					}

				if (remainder == 0)
					Games[1][0] = 0;

				Games[GameCount - 2][1] = Teams.Count - 1;
			}
			else
				Append();

			Games.ClearCache();
			return Games;
		}

		/// <summary>Set up an IndexGames for any number of TeamsPerGame. The first TeamsPerGame's worth of teams go in the first game; the next TeamsPerGame's worth in the next game, etc.
		/// If there are already some games in the IndexGames, we'll just add games as required -- existing games are kept.</summary>
		public void Append()
		{
			// How many times do we need to add each team?
			int plays = TeamsPerGame * GameCount - Games.Plays;

			// If we've got more teams than games, then there are a lot of teams in each game, so focus on giving all teams the same number of games.
			// If we've got more games than teams, then there are just a few teams in each game, so focus on putting the same number of teams in each game.
			int remainingPlays = Teams.Count > GameCount ? plays : TeamsPerGame * (GameCount - Games.Count);

			int nextPlay = 0;
			bool added = false;
			for (int g = Games.Count; g < GameCount; g++)
			{
				double teamsPerGame = 1.0 * remainingPlays / (GameCount - Games.Count);
				var game = Games.AddNewGame();
				for (int t = 0; t < Math.Ceiling(teamsPerGame); t++)
				{
					game.Add(nextPlay++);
					if (nextPlay >= Teams.Count)
						nextPlay = 0;
					remainingPlays--;
				}
				added = true;
			}

			if (added)
				Games.ClearCache();
		}

		/// <summary>Create one game, containing the specified teams. If the number passed would take us off the end of the list of teams, modulo back to a valid team.</summary>
		private void Game(int mark1, int mark2, int mark3)
		{
			if (Games.Count < GameCount)
			{
				var game = Games.AddNewGame();
				game.Add((mark1 + Teams.Count) % Teams.Count);
				game.Add((mark2 + Teams.Count) % Teams.Count);
				game.Add((mark3 + Teams.Count) % Teams.Count);
			}
		}

		/// <summary>
		/// Create _n_ games, where _n_ = Teams.Count. Only supports TeamsPerGame = 3.
		/// Parameters mark2 and mark3 form the second and third marks on a three-mark Golomb ruler: { 0 mark2 mark3 }.
		/// They are also the first and third distances:
		/// distanceA = mark2 - 0; distanceB = mark3 - mark2; distanceC = mark3 - 0.
		/// ( In general, to convert n marks to n(n-1)/2 distances: d(i,j) = abs(m(i) - m(j)) ).
		/// </summary>
		private void Mark3(int mark2, int mark3)
		{
			for (int g = 0; g < Teams.Count; g++)
				Game(g + 0, g + mark2, g + mark3);
		}

		/// <summary>Create multiple sets of marks. Only supports TeamsPerGame = 3.</summary>
		private void Mark3s(params int[] marks)
		{
			for (int group = 0; group < marks.Length - 1; group += 2)
				Mark3(marks[group], marks[group + 1]);
		}

		/// <summary>Create _n_ games, where _n_ = Teams.Count. In each game, make a mark for each passed paramenter. Requires that TeamsPerGame == marks.Length.</summary>
		private void MarkN(params int[] marks)
		{
			for (int g = 0; g < Teams.Count && Games.Count < GameCount; g++)
			{
				var game = Games.AddNewGame();
				for (int m = 0; m < marks.Length; m++)
					game.Add((g + marks[m] + Teams.Count) % Teams.Count);
			}
		}
	}

	/// <summary>Just the hill-climbing bit.</summary>
	class Improver
	{
		public IndexGames PlayGames;
		public IndexGames RefGames;
		public Plays ExistingPlays;
		public Sessions Sessions;
		public ScoreScalers ScoreScalers;
		public List<Colour> Colours;
		public ShuffleType ShuffleType;
		public double Score;
		public bool Rings;  // True if this is a Lord of the Ring style game, where all players on each colour are their own sub-game.
		public bool Running = false;

		private readonly Plays plays;
		private readonly OnSite onSite;
		private readonly List<double> difficulties = new List<double>();  // [teamIndex]
		private readonly List<double> targetDifficulties = new List<double>();  // [teamIndex]
		private readonly List<List<int>> colourCounts = new List<List<int>>();  // [teamIndex][colour]. Number of times each teamIndex plays on each colour.
		private readonly List<int> refCount = new List<int>();  // [teamIndex]. Total number of times each teamIndex referees.
		private readonly List<DayOrNightLength> shortNights = new List<DayOrNightLength>();
		private readonly List<DayOrNightLength> longDays = new List<DayOrNightLength>();
		private readonly Random random = new Random();
		private int attemptsThisSecond;
		private int successes = 0;
		private DateTime lastSuccessTime;
		private SwapRecord lastSuccessfulSwap = new SwapRecord();
		private Dictionary<ShuffleType, int> swaps = new Dictionary<ShuffleType, int>();
		private bool allColoursPerfect = true;

		public static GamesResult Improve(Bundle b)
		{
			var imp = new Improver(b.PlayGames, b.ScoreScalers) { RefGames = b.RefGames.Clone(), ExistingPlays = b.ExistingPlays, Sessions = b.Sessions.Clone(), Colours = b.Colours, ShuffleType = b.ShuffleType, Rings = b.Rings };
			return imp.Improve();
		}

		public Improver(IndexGames playGames, ScoreScalers scoreScalers)
		{
			PlayGames = playGames;
			ScoreScalers = scoreScalers;
			plays = new Plays(PlayGames.MaxIndex + 1);
			onSite = new OnSite(PlayGames);

			foreach (ShuffleType st in Enum.GetValues(typeof(ShuffleType)))
				swaps.Add(st, 0);

			for (int teamIndex = 0; teamIndex <= PlayGames.MaxIndex; teamIndex++)
			{
				colourCounts.Add(Enumerable.Repeat(0, PlayGames.MaxTeamsPerGame).ToList());

				refCount.Add(0);
			}

			if (ScoreScalers.SameDifficulty >= ScoreScalers.CascadeDifficulty)
				for (int i = 0; i <= PlayGames.MaxIndex; i++)  // Set up difficulties for a round robin: all the same.
					targetDifficulties.Add((PlayGames.MaxIndex + 1.0) / 2.0);
			else
			{  // Set up difficulties for a cascade. For teams in the middle of the cascade, target difficulty equals their own rank.
				for (int i = 0; i <= PlayGames.MaxIndex; i++)
					targetDifficulties.Add(i);

				if (PlayGames.MaxIndex >= 11)
				{  // Adjust ends to be of less slope.
					for (int i = 0; i < 5; i++)
					{
						targetDifficulties[i] = i / 2.5 + 3.0;
						targetDifficulties[PlayGames.MaxIndex - 5 + i] = PlayGames.MaxIndex - 5.6 + i / 2.5;
					}
				}
			}
		}

		/// <summary>Make changes to PlayGames and/or RefGames, see if they're better. If so, keep them. Iterate. After 1 second, stop.</summary>
		/// <returns>If we made an improvement (or at least didn't make things worse), return the new result. If not, return null.</returns>
		public GamesResult Improve()
		{
			var finish = DateTime.Now.AddSeconds(1);
			Score = ScoreGames(PlayGames, RefGames);

			attemptsThisSecond = 0;
			bool changed = false;
			double playRefRatio = 1.0 * PlayGames.Plays / (PlayGames.Plays + RefGames.Plays);

			do
			{
				if (!ShuffleType.HasFlag(ShuffleType.Referees) || ((ShuffleType != ShuffleType.Referees) && random.NextDouble() < playRefRatio))
				{
					(IndexGames newPlayGames, SwapRecord swapped) = Shuffle(PlayGames, ShuffleType);
					changed |= KeepBest(newPlayGames, ref PlayGames, ScoreGames(newPlayGames, RefGames), swapped);
				}
				else
				{
					(IndexGames newRefGames, SwapRecord swapped) = Shuffle(RefGames, ShuffleType.BetweenGames);
					swapped.ShuffleType = ShuffleType.Referees;
					changed |= KeepBest(newRefGames, ref RefGames, ScoreGames(PlayGames, newRefGames), swapped);
				}
			} while (DateTime.Now < finish);

			return changed ? new GamesResult() { PlayGames = PlayGames.Clone(), RefGames = RefGames.Clone(), Score = Score, ScoreString = ScoreString(), Details = Details() } : null;
		}

		/// <summary>Return a GamesResult showing the score and details of this set of games, but don't try to make improvements.</summary>
		public GamesResult Scored()
		{
			Score = ScoreGames(PlayGames, RefGames);
			lastSuccessTime = DateTime.Now;
			return new GamesResult() { PlayGames = PlayGames.Clone(), RefGames = RefGames.Clone(), Score = Score, ScoreString = ScoreString(), Details = Details() };
		}

		/// <summary>Swap teams around.</summary>
		private (IndexGames, SwapRecord) Shuffle(IndexGames games, ShuffleType shuffleType)
		{
			IndexGames newGames = games.Clone();
			SwapRecord sr;
			
			if (shuffleType.HasFlag(ShuffleType.BetweenGames))
				sr = SwapBetweenGames(newGames);
			else if (shuffleType.HasFlag(ShuffleType.Games) && shuffleType.HasFlag(ShuffleType.WithinGames))
			{
				if (allColoursPerfect || random.NextDouble() < 0.5)
					sr = SwapWholeGames(newGames);
				else
					sr = SwapWithinGame(newGames);
			}
			else if (shuffleType.HasFlag(ShuffleType.Games))
				sr = SwapWholeGames(newGames);
			else if (shuffleType.HasFlag(ShuffleType.Rings))
				sr = SwapRings(newGames);
			else if (shuffleType.HasFlag(ShuffleType.WithinGames))
				sr = SwapWithinGame(newGames);
			else
				sr = new SwapRecord() { ShuffleType = ShuffleType.None };

			if (random.NextDouble() < 0.1)  // Occasionally, do two shuffles at once. This is to help get past local maxima. Very occasionally, the recursive call will do its _own_ double, meaning a triple, etc.
			{
				(newGames, _) = Shuffle(newGames, shuffleType);
				sr = new SwapRecord() { ShuffleType = ShuffleType.Multiple };
			}
			
			return (newGames, sr);
		}

		private SwapRecord SwapBetweenGames(IndexGames newGames)
		{
			int game1 = random.Next(newGames.Count);
			int game2 = random.Next(newGames.Count - 1);
			if (game2 >= game1) game2++;

			int team1 = random.Next(newGames[game1].Count);
			int team2 = random.Next(newGames[game2].Count);

			(newGames[game1][team1], newGames[game2][team2]) = (newGames[game2][team2], newGames[game1][team1]);

			return new SwapRecord() { ShuffleType = ShuffleType.BetweenGames, Game1 = game1, Game2 = game2, Team1 = team1, Team2 = team2 };
		}

		private SwapRecord SwapWholeGames(IndexGames newGames)
		{
			int game1 = random.Next(newGames.Count);
			int game2 = random.Next(newGames.Count - 1);
			if (game2 >= game1) game2++;

			for (int t = 0; t < Math.Min(newGames[game1].Count, newGames[game2].Count); t++)
				(newGames[game1][t], newGames[game2][t]) = (newGames[game2][t], newGames[game1][t]);

			return new SwapRecord() { ShuffleType = ShuffleType.Games, Game1 = game1, Game2 = game2 };
		}

		private SwapRecord SwapRings(IndexGames newGames)
		{
			int game1 = random.Next(newGames.Count);
			int game2 = random.Next(newGames.Count - 1);
			if (game2 >= game1) game2++;

			int team1 = random.Next(newGames[game1].Count / 3) * 3;
			int team2 = random.Next(newGames[game2].Count / 3) * 3;

			(newGames[game1][team1], newGames[game2][team2]) = (newGames[game2][team2], newGames[game1][team1]);
			(newGames[game1][team1 + 1], newGames[game2][team2 + 1]) = (newGames[game2][team2 + 1], newGames[game1][team1 + 1]);
			(newGames[game1][team1 + 2], newGames[game2][team2 + 2]) = (newGames[game2][team2 + 2], newGames[game1][team1 + 2]);

			return new SwapRecord() { ShuffleType = ShuffleType.Rings, Game1 = game1, Game2 = game2, Team1 = team1 / 3, Team2 = team2 / 3 };
		}

		private SwapRecord SwapWithinGame(IndexGames newGames)
		{
			int game = random.Next(newGames.Count);
			int team1 = random.Next(newGames[game].Count);
			int team2 = random.Next(newGames[game].Count - 1);
			if (team2 >= team1) team2++;

			(newGames[game][team1], newGames[game][team2]) = (newGames[game][team2], newGames[game][team1]);

			return new SwapRecord() { ShuffleType = ShuffleType.WithinGames, Game1 = game, Game2 = game, Team1 = team1, Team2 = team2 };
		}

		/// <summary>If new is at least as good, keep it. Update metrics accordingly.</summary>
		private bool KeepBest(IndexGames neww, ref IndexGames old, double newScore, SwapRecord swapped)
		{
			bool changed = false;
			if (newScore <= Score)  // It's at least as good.
			{
				old.CopyFrom(neww);
				changed = true;
				if (newScore < Score)  // It's better.
				{
					successes++;
					lastSuccessTime = DateTime.Now;
					lastSuccessfulSwap = swapped;
					swaps[swapped.ShuffleType]++;
				}
				Score = newScore;
			}
			attemptsThisSecond++;
			return changed;
		}

		/// <summary>Calculate how "good" these playGames and refGames are. Lower scores are better. This is "expensive" -- it takes something like 0.1 to 1 millisecond to run.</summary>
		private double ScoreGames(IndexGames playGames, IndexGames refGames)
		{
			plays.Calc(playGames, ExistingPlays, Colours, Rings);
			onSite.CalcOnSite(playGames, refGames, Sessions); // Set up OnSite.

			double timesPlayedScore = TimesPlayedScore(playGames.AveragePlays + ExistingPlays.Average());
			int playSelfScore = PlaySelfScore(playGames, refGames);
			double backToBackScore = BackToBackScore(playGames, refGames);
			double timeOnSite = TimeOnSiteScore(playGames);
			(double teamsOnSite, int maxOnSite) = TeamsOnSiteScore(playGames);
			double difficultyError = DifficultyScore(playGames);
			double colourError = ColourScore(playGames, refGames);
			int refereeClustering = RefereeClustering(refGames);

			return timesPlayedScore + playSelfScore + backToBackScore + timeOnSite + teamsOnSite + maxOnSite + difficultyError + colourError + refereeClustering;
		}

		/// <summary>Score teams for how many times they play each other.</summary>
		private double TimesPlayedScore(double averagePlays)
		{
			if (ScoreScalers.TimesPlayed == 0)
				return 0;

			double score = 0;
			for (int team1 = 0; team1 < plays.Size; team1++)
				for (int team2 = team1 + 1; team2 < plays.Size; team2++)
					if (plays[team1][team2] > 1)
						score += Math.Pow(plays[team1][team2] - averagePlays, 4);

			return score * ScoreScalers.TimesPlayed;
		}

		/// <summary>Score teams for how many times they play twice in the same game, or both play and referee a game.</summary>
		private int PlaySelfScore(IndexGames playGames, IndexGames refGames)
		{
			if (ScoreScalers.PlaySelf == 0)
				return 0;

			int score = 0;
			for (int team1 = 0; team1 < plays.Size; team1++)
				if (plays[team1][team1] > 0)
					score += plays[team1][team1];

			if (refGames != null)
				for (int game = 0; game < playGames.Count; game++)
					for (int t1 = 0; t1 < refGames[game].Count; t1++)  // for each team refereeing in this game
					{
						for (int t2 = t1 + 1; t2 < refGames[game].Count; t2++)  // for each team ref'ing in this game
							if (refGames[game][t1] == refGames[game][t2])  // if they are ref'ing twice in this game,
								score++;  // that's bad.

						for (int t2 = 0; t2 < playGames[game].Count; t2++)  // for each team playing in this game
							if (refGames[game][t1] == playGames[game][t2])  // if they are both playing and ref'ing,
								score++;  // that's bad.
					}

			return score * ScoreScalers.PlaySelf;
		}

		/// <summary>Score teams for playing two consecutive games, or playing and refereeing consecutive games.</summary>
		private int BackToBackScore(IndexGames playGames, IndexGames refGames)
		{
			if (ScoreScalers.BackToBack == 0)
				return 0;

			int score = 0;

			for (int game = 0; game < playGames.Count - 1; game++)  // for each consecutive pair of games,
				if (!Sessions.Any(s => s.Last == game))  // If this game is the Last in a Session, there is no next game, so don't look.
				{
					foreach (var teamIndex1 in playGames[game])
					{
						if (playGames[game + 1].Any(teamIndex2 => teamIndex2 == teamIndex1))  // team plays then plays in the next game
							score += ScoreScalers.BackToBack;

						if (refGames[game + 1].Any(teamIndex2 => teamIndex2 == teamIndex1))  // team plays then referees the next game
							score += ScoreScalers.PlayThenRef;
					}

					foreach (var teamIndex1 in refGames[game])
						if (playGames[game + 1].Any(teamIndex2 => teamIndex2 == teamIndex1))  // team referees then plays in the next game
							score += ScoreScalers.RefThenPlay;
				}

			return score;
		}

		/// <summary>Return a human-friendly list of teams that play two consecutive games, or play and referee consecutive games.</summary>
		private StringBuilder GetBackToBacks(IndexGames playGames, IndexGames refGames)
		{
			var sb = new StringBuilder();

			if (ScoreScalers.BackToBack == 0)
				return sb;

			for (int game = 0; game < playGames.Count - 1; game++)  // For each consecutive pair of games (other than the very last),
				if (!Sessions.Any(s => s.Last == game))  // If this game is the Last in a Session, there is no next game, so don't look.
				{
					foreach (int teamIndex in playGames[game])  // for each team playing this game
					{
						if (playGames[game + 1].Any(teamIndex2 => teamIndex2 == teamIndex) && ScoreScalers.BackToBack > 0)  // same team plays in the next game
							sb.AppendFormat("Team ID {0} plays games {1} and {2}. ", teamIndex + 1, game + 1, game + 2);

						if (refGames[game + 1].Any(teamIndex2 => teamIndex2 == teamIndex) && ScoreScalers.PlayThenRef > 0)  // same team referees the next game
							sb.AppendFormat("Team ID {0} plays game {1} then refs. ", teamIndex + 1, game + 1);
					}

					foreach (int teamIndex in refGames[game])  // for each team refereeing this game
						if (playGames[game + 1].Any(teamIndex2 => teamIndex2 == teamIndex) && ScoreScalers.RefThenPlay > 0)  // same team plays in the next game
							sb.AppendFormat("Team ID {0} refs game {1} then plays. ", teamIndex + 1, game + 1);
				}

			return sb;
		}

		private double TimeOnSiteScore(IndexGames playGames)
		{
			if (ScoreScalers.DayLength == 0 && ScoreScalers.NightLength == 0 && ScoreScalers.NoGamesInSession == 0 && ScoreScalers.OneGameInSession == 0)
				return 0;

			shortNights.Clear();
			longDays.Clear();
			double score = 0;

			for (int teamIndex = 0; teamIndex <= playGames.MaxIndex; teamIndex++)
			{
				if (Sessions.Count <= 1)
				{
					// Score for length of day.
					int first = onSite.FirstGame(teamIndex, 0, playGames.Count - 1);
					int last = onSite.LastGame(teamIndex, 0, playGames.Count - 1);
					score += Math.Pow(Math.Max(1.0 * (last - first - playGames.PlaysPerTeam), 0.0), 2) / playGames.Count * ScoreScalers.DayLength;
				}
				else
				{
					int startGame = 0;  // Beginning of the current day.
					int last = 0;
					int session = Sessions.FindIndex(s => s.First <= startGame);  // Session that begins the current day.

					while (startGame < playGames.Count)
					{
						int endSession = Sessions.NextEndOfDayIndex(session);  // Session that ends the current day.
						int endGame = Sessions[endSession].Last;  // End of the current day.

						int first = onSite.FirstGame(teamIndex, startGame, endGame);
						if (first == -1)
							score += ScoreScalers.NoGamesInSession;  // Team has no games on this day.
						else
						{
							if (last != 0 && last < first && first - last < 17)  // If overnight break less than 16 games
							{
								double nightLengthScore = Math.Pow(17 - first + last, 2);  // Score for length of overnight break: longer night is better.
								score += nightLengthScore * ScoreScalers.NightLength;
								if (nightLengthScore >= 1)
									shortNights.Add(new DayOrNightLength() { TeamIndex = teamIndex, FirstGame = first, LastGame = last });
							}

							last = onSite.LastGame(teamIndex, startGame, endGame);
							double dayLengthScore = Math.Pow(Math.Max(1.0 * (last - first + (endSession - session) * 32 - playGames.PlaysPerTeam * 2), 0), 2) / playGames.Count;
							score += dayLengthScore * ScoreScalers.DayLength;  // Score for length of day: shorter is better.
							if (dayLengthScore >= 1)
								longDays.Add(new DayOrNightLength() { TeamIndex = teamIndex, FirstGame = first, LastGame = last });
						}

						if (first == last)  // This team has just one game on this day -- that's bad.
							score += ScoreScalers.OneGameInSession;

						session = Sessions.NextSessionIndex(endGame);
						startGame = endGame + 1;
					}  // while startGame is valid
				}  // if Sessions.Count > 1
			}  // for teamIndex

			return score;
		}

		/// <summary>Return a score for the average number of teams on site, and the maximum number of teams on site.</summary>
		private (double, int) TeamsOnSiteScore(IndexGames playGames)
		{
			int teamsOnSite = 0;
			int maxOnSite = 0;
			for (int game = 0; game < playGames.Count; game++)
			{
				int onSiteNow = 0;
				for (int teamIndex = 0; teamIndex <= playGames.MaxIndex; teamIndex++)
				{
					if (onSite[game][teamIndex])
					{
						teamsOnSite++;
						onSiteNow++;
					}
				}
				maxOnSite = Math.Max(maxOnSite, onSiteNow);
			}

			return (teamsOnSite * ScoreScalers.TeamsOnSite / 10.0, maxOnSite);
		}

		private double DifficultyScore(IndexGames playGames)
		{
			if (ScoreScalers.SameDifficulty == 0 && ScoreScalers.CascadeDifficulty == 0)
				return 0;

			int numTeams = playGames.MaxIndex + 1;
			difficulties.Clear();
			double score = 0;
			for (int team = 0; team < numTeams; team++)  // TODO: calculate team difficulty using Plays instead of playGames.
			{
				double difficulty = 0;
				int count = 0;
				foreach (var game in playGames)
				{
					if (game.HasTeam(team))
					{
						for (int i = 0; i < game.Count; i++)
							if (game[i] != team)
								difficulty += game[i];
						count += game.Count - 1;
					}
				}

				// Score teams for their difficulty factor, assuming try to get equal difficulties for round robins.
				if (count == 0)
					difficulties.Add(numTeams / 2.0);
				else
				{
					difficulties.Add(difficulty / count);
					score += ScoreScalers.SameDifficulty * Math.Abs(Math.Pow(difficulty / count - (numTeams + 1) / 2, 4));
				}
			}

			if (ScoreScalers.CascadeDifficulty != 0)
				for (int team = 0; team < numTeams; team++)
				{
					// Score teams for the error in their difficulty, as compared to the next team: cascade difficulties.
					//double difficultyError = (difficulties[team + 1] - targetDifficulties[team + 1]) - (difficulties[team] - targetDifficulties[team]);
					double difficultyError = difficulties[team] - targetDifficulties[team];
					score += Math.Pow(difficultyError, 4) * ScoreScalers.CascadeDifficulty;

					if (team < numTeams - 1)
					{
						if (difficulties[team] > difficulties[team + 1])  // add an out-of-order penalty
							score += 100 * ScoreScalers.CascadeDifficulty;
						else if (difficulties[team] == difficulties[team + 1])  // add a not-in-order penalty
							score += 11 * ScoreScalers.CascadeDifficulty;
					}
				}

			return score;
		}

		private double ColourScore(IndexGames playGames, IndexGames refGames)
		{
			// Clear out ColourCounts.
			for (int teamIndex = 0; teamIndex < colourCounts.Count; teamIndex++)
			{
				for (int c = 0; c < colourCounts[teamIndex].Count; c++)
					colourCounts[teamIndex][c] = 0;
				refCount[teamIndex] = 0;
			}

			// For all games, for all teams within each game, increment that teamIndex's relevant colour.
			for (int g = 0; g < playGames.Count; g++)
				for (int c = 0; c < playGames[g].Count; c++)
					colourCounts[playGames[g][c]][c]++;

			double averagePlaysOnEachColour = 1.0 * playGames.PlaysPerTeam / playGames.TeamsPerGame;
			double colourError = 0;
			allColoursPerfect = true;

			for (int teamIndex = 0; teamIndex <= playGames.MaxIndex; teamIndex++)  // Score each team for how many times they play each colour.
			{
				for (int i = 0; i < colourCounts[teamIndex].Count; i++)
					colourError += Math.Pow(Math.Abs(colourCounts[teamIndex][i] - averagePlaysOnEachColour), 1.42);

				int colourMin = int.MaxValue;
				int colourMax = 0;
				foreach (var count in colourCounts[teamIndex])
				{
					colourMin = Math.Min(colourMin, count);
					colourMax = Math.Max(colourMax, count);
				}
				
				if (colourMin == Math.Floor(averagePlaysOnEachColour) && colourMax == Math.Ceiling(averagePlaysOnEachColour))
					colourError += ScoreScalers.PerfectColour;
				else
					allColoursPerfect = false;
			}

			// For all games, for all teams refereeing that game, increment that teamIndex's ref count.
			for (int g = 0; g < refGames.Count; g++)
				for (int c = 0; c < refGames[g].Count; c++)
					refCount[refGames[g][c]]++;

			for (int teamIndex = 0; teamIndex <= refGames.MaxIndex; teamIndex++)  // Score each team for how many times they referee.
				colourError += Math.Pow(Math.Abs(refCount[teamIndex] - refGames.PlaysPerTeam), 1.42);

			return colourError;
		}

		private int RefereeClustering(IndexGames refGames)
		{
			int numteams = refGames.MaxIndex;

			if (numteams == 0 || ScoreScalers.RefereeClustering == 0)
				return 0;

			int score = 0;

			var isReffing = new OnSite(refGames);
			isReffing.CalcInGame(refGames);
			var sessionLasts = Sessions.Select(s => s.Last);

			for (int team1 = 0; team1 < numteams; team1++)
			{
				int game = 0;

				while (game < refGames.Count)
				{
					int grouplength = 0;

					// Find a game this team is refereeing.
					while (game < refGames.Count && !isReffing[game][team1])
						game++;

					// Walk along to see if this team refs several games in a row.
					while (game < refGames.Count && isReffing[game][team1])
					{
						grouplength++;
						game++;

						if (sessionLasts.Contains(game))
							break;
					}

					if (game < refGames.Count || isReffing.Last()[team1])  // Did we leave the above walk-along loop because we got to the end of the games, or because we found a group of games? If we found a group,
						switch (grouplength)  // apply some score based on group length. Reffing 3 or 4 games in a row is best.
						{
							case 1: score += 8; break;
							case 2: score += 2; break;
							case 3: case 4: break;
							case 5: score += 1; break;
							default: score += grouplength; break;
						}
				}
			}

			return score * ScoreScalers.RefereeClustering;
		}

		/// <summary>Sibling of ScoreGames() in that it calculates the same scores; but returns the score as detailed text.</summary>
		private string ScoreString()
		{
			plays.Calc(PlayGames, ExistingPlays, Colours, Rings);
			onSite.CalcOnSite(PlayGames, RefGames, Sessions); // Set up OnSite.

			double timesPlayedScore = TimesPlayedScore(PlayGames.AveragePlays + ExistingPlays.Average());  // Score teams for how many times they play each other.
			int playSelfScore = PlaySelfScore(PlayGames, RefGames);
			double backToBackScore = BackToBackScore(PlayGames, RefGames);
			double timeOnSite = TimeOnSiteScore(PlayGames);
			(double teamsOnSite, int maxOnSite) = TeamsOnSiteScore(PlayGames);
			double difficultyError = DifficultyScore(PlayGames);
			double colourError = ColourScore(PlayGames, RefGames);
			int refereeClustering = RefereeClustering(RefGames);

			var sb = new StringBuilder();
			AppendIfNot0(sb, "Score: ", timesPlayedScore + playSelfScore + backToBackScore + timeOnSite + teamsOnSite + maxOnSite + difficultyError + colourError + refereeClustering, ".  ", "F3");
			AppendIfNot0(sb, "Times played: ", timesPlayedScore, ", ", "F1");
			AppendIfNot0(sb, "play self: ", playSelfScore);
			AppendIfNot0(sb, "back-to-back: ", backToBackScore);
			AppendIfNot0(sb, "time on site: ", timeOnSite, ", ", "F1");
			AppendIfNot0(sb, "teams on site: ", teamsOnSite, ", ", "F1");
			AppendIfNot0(sb, "max teams on site: ", maxOnSite);
			AppendIfNot0(sb, "difficulty: ", difficultyError, ", ", "F3");
			AppendIfNot0(sb, "colour: ", colourError, ", ", "F1");
			AppendIfNot0(sb, "referees: ", refereeClustering, ".  ", "F3");
			AppendIfNot0(sb, "", successes, " successes. ");
			AppendIfNot0(sb, "", attemptsThisSecond, " attempts per second. ");

			if (successes > 0)
				sb.AppendFormat("Last success: {0:T}; swapped {1}.", lastSuccessTime, lastSuccessfulSwap);

			sb.Append("\r\n");

			return sb.ToString();
		}

		private void AppendIfNot0(StringBuilder sb, string prefix, double x, string suffix = ", ", string format = "")
		{
			if (x == 0)
				return;

			sb.Append(prefix);

			if (Math.Abs(x - Math.Round(x)) < 0.001)
				sb.Append((int)x);
			else if (string.IsNullOrEmpty(format))
				sb.Append(x);
			else
				sb.AppendFormat("{0:" + format + "}", x);

			sb.Append(suffix);
		}

		private string Details()
		{
			var sb = new StringBuilder();

			bool found = false;
			foreach (ShuffleType st in Enum.GetValues(typeof(ShuffleType)))
				if (swaps[st] > 0)
				{
					if (!found)
						sb.Append("Swaps: ");

					sb.Append(st);
					sb.Append(": ");
					sb.Append(swaps[st]);
					sb.Append(", ");
					found = true;
				}

			if (found)
			{
				sb.Length -= 2;
				sb.Append(".\r\n");
			}

			plays.Calc(PlayGames, ExistingPlays, this.Colours, Rings);

			var backToBacks = GetBackToBacks(PlayGames, RefGames);
			if (backToBacks.Length > 0)
			{
				sb.Append("Back-to-back games: ");
				sb.Append(backToBacks);
			}

			// Header row above main grid.
			sb.Append("\r\nGames");
			sb.Append(' ', Math.Max(PlayGames.Count - 8, 0));
			sb.Append(difficulties.Any() ? "Difficulty   #  " : "      #  ");

			var coloursPlayed = Colours.Where(c => c != Colour.Referee);
			bool coloursAreDistinct = coloursPlayed.Count() == coloursPlayed.Distinct().Count();
			if (coloursAreDistinct)
			{
				foreach (var colour in coloursPlayed)
					sb.Append(colour.ToChar());

				if (Colours.Any(c => c == Colour.Referee))
					sb.Append(Colour.Referee.ToChar());

				sb.Append("  ");
			}

			sb.Append("Who-plays-who\r\n");

			// Main grid. One row per team.
			for (int teamIndex = 0; teamIndex <= PlayGames.MaxIndex; teamIndex++)
			{
				// One column per game.
				for (int g = 0; g < PlayGames.Count; g++)
				{
					var game = PlayGames[g];
					if (Sessions.Any(s => s.Last == g))
						sb.Append(' ');

					char ch = onSite[g][teamIndex] ? '-' : '.';

					int numberOfTimesTeamPlaysInThisGame = game.Count(t => t == teamIndex) + RefGames[g].Count(t => t == teamIndex);
					if (numberOfTimesTeamPlaysInThisGame > 1)
						ch = Histo(numberOfTimesTeamPlaysInThisGame);
					else
					{
						for (int t = 0; t < game.Count; t++)
							if (game[t] == teamIndex)
								ch = Colours == null ? Histo(t + 10) : Colours.Valid(t) ? Colours[t].ToChar() : '?';

						for (int t = 0; t < RefGames[g].Count; t++)
							if (RefGames[g][t] == teamIndex)
								ch = Colour.Referee.ToChar();
					}

					sb.Append(ch);
				}

				if (difficulties.Any())
					sb.AppendFormat(" {0,6:F2}", difficulties[teamIndex] + 1);

				sb.AppendFormat("{0,4}  ", teamIndex + 1);

				if (coloursAreDistinct)
				{
					for (int c = 0; c < coloursPlayed.Count() && c < colourCounts[teamIndex].Count; c++)
						sb.Append(Histo(colourCounts[teamIndex][c]));  // Show how many times this team plays each colour.

					if (Colours.Any(c => c == Colour.Referee))
						sb.Append((Histo(refCount[teamIndex])));

					sb.Append("  ");
				}

				// Who plays who
				for (int teamIndex2 = 0; teamIndex2 < plays.Count; teamIndex2++)
					if (teamIndex == teamIndex2)
						sb.Append('\\');
					else if (plays[teamIndex][teamIndex2] == 0)
						sb.Append('.');
					else
						sb.Append(Histo(plays[teamIndex][teamIndex2]));

				sb.Append("\r\n");
			}

			if (longDays.Count > 0)
			{
				longDays.Sort((x, y) => (y.LastGame - y.FirstGame - x.LastGame + x.FirstGame));
				sb.Append("Longest day: team index ");
				sb.Append(longDays[0].TeamIndex + 1);
				sb.Append(", from ");
				sb.Append(longDays[0].FirstGame + 1);
				sb.Append(" to ");
				sb.Append(longDays[0].LastGame + 1);
				sb.Append(".\\r\n");
			}

			//int len = first - last;
			//AppendFormat("Team {0:2N} game {1:3N}; {2:3N}: {4:2N} {5}{6}\t", team, last + 1, first + 1, len, new string('*', len / 4), new string(' ', 8 - (len / 4)));

			if (shortNights.Count > 0)
			{
				shortNights.Sort((x, y) => (x.LastGame - x.FirstGame - y.LastGame + y.FirstGame));
				sb.AppendFormat("Shortest night: team {0}, from {1} to {2}.\r\n", shortNights[0].TeamIndex + 1, shortNights[0].FirstGame, shortNights[0].LastGame);
			}

			// List of games as space-separated for teams in games; semicolon-separated between games.
			sb.Append("\nGames as a list:\r\n");
			for (int g = 0; g < PlayGames.Count; g++)
			{
				sb.Append(string.Join(" ", PlayGames[g]));
				sb.Append("; ");

				if (Sessions.Any(s => s.Last == g))
					sb.Append('\t');
			}
			sb.Append("\r\n");

			return sb.ToString();
		}

		/// <summary>Convert an int to a char. If 0 to 9, use '0' to '9'. If greater than 9, use lowercase then uppercase letters.</summary>
		private char Histo(int i)
		{
			return (i >= 0 && i < 61) ? "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"[i] : '?';
		}
	}

	/// <summary>A list of indexes into a List of LeagueTeam. This represents a future game, not yet played.</summary>
	class IndexGame : List<int>
	{
		public IndexGame Clone()
		{
			var clone = new IndexGame();

			foreach (var i in this)
				clone.Add(i);

			return clone;
		}

		/// <summary>True if this team is in this game.</summary>
		public bool HasTeam(int teamIndex)
		{
			return this.Any(t => t == teamIndex);
		}
	}

	/// <summary>A list of games, with each game being a list of indexes into a List of LeagueTeam. These are future games, not yet played.</summary>
	class IndexGames: List<IndexGame>
	{
		public IndexGames Clone()
		{
			var clone = new IndexGames();

			foreach (var game in this)
				clone.Add(game.Clone());

			return clone;
		}

		///<summary>Assumes both IndexGames's have the same dimensions.</summary> 
		public void CopyFrom(IndexGames source)
		{
			for (var g = 0; g < source.Count; g++)
				for (int t = 0; t < source[g].Count; t++)
					this[g][t] = source[g][t];

			maxIndex = source.MaxIndex;
			numTeams = source.NumTeams;
			teamsPerGame = source.TeamsPerGame;
			maxTeamsPerGame = source.MaxTeamsPerGame;
			plays = source.Plays;
		}

		///<summary>Create, add and return a Game.</summary> 
		public IndexGame AddNewGame()
		{
			var game = new IndexGame();
			Add(game);
			return game;
		}

		public void ClearCache()
		{
			maxIndex = -1;
			numTeams = -1;
			teamsPerGame = -1;
			maxTeamsPerGame = -1;
			plays = -1;
		}

		int maxIndex = -1;
		/// <summary>Maximum TeamIndex of any team. Computed once, then cached.</summary>
		public int MaxIndex { get
			{
				if (maxIndex == -1 && this.Any())
					maxIndex = this.Max(g => g.Any() ? g.Max() : 0);

				return maxIndex;
			}
		}

		int numTeams = -1;
		/// <summary>Number of distinct different TeamIndex's.</summary>
		public int NumTeams
		{
			get
			{
				if (numTeams == -1)
					numTeams = this.SelectMany(g => g).Distinct().Count();

				return numTeams;
			}
		}

		double teamsPerGame = -1;
		/// <summary>Average number of teams in each game.</summary>
		public double TeamsPerGame { get
			{
				if (teamsPerGame == -1)
					teamsPerGame = Count == 0 ? 3 : this.Average(g => g.Count);

				return teamsPerGame;
			}
		}

		int maxTeamsPerGame = -1;
		public int MaxTeamsPerGame { get
			{
				if (maxTeamsPerGame == -1)
					maxTeamsPerGame = Count == 0 ? 3 : this.Max(g => g.Count);
				return maxTeamsPerGame;
			}
		}

		int plays = -1;
		/// <summary>A "play" is a team in a game. So Plays = number of games multiplied by number of teams per game.</summary>
		public int Plays { get
			{
				if (plays == -1)
					plays = this.Sum(g => g.Count);

				return plays;
			}
		}

		/// <summary>Average number of games each team plays in.</summary>
		public double PlaysPerTeam => NumTeams == 0 ? 0 : 1.0 * Plays / NumTeams;

		/// <summary>Average number of times each team plays each other team.</summary>
		public double AveragePlays => NumTeams == 0 ? 0 : 1.0 * PlaysPerTeam * (TeamsPerGame - 1) / (NumTeams - 1);
	}

	/// <summary>List of games, with each game containing a list of teams, 
	/// with each team having a bool showing if they are marked as being on site during this game.</summary>
	class OnSite : List<List<bool>>
	{
		/// <summary>
		/// Create (number of games) × (number of teams) bool grid.
		/// </summary>
		/// <param name="source">source is used for dimensions. Actual on-site data is not populated here in the constructor.</param>
		public OnSite(IndexGames source)
		{
			Clear();
			for (int g = 0; g < source.Count; g++)
				Add(Enumerable.Repeat(false, source.MaxIndex + 1).ToList());
		}

		///<summary>Mark true for teams that are actually in a game. Clears out any old data as it goes.</summary> 
		public void CalcInGame(IndexGames source)
		{
			for (int g = 0; g < source.Count; g++)
				for (int teamIndex = 0; teamIndex <= source.MaxIndex; teamIndex++)
					this[g][teamIndex] = source[g].HasTeam(teamIndex);
		}

		///<summary>Mark true for teams that are playing or reffing a game, or are sitting around in the lobby after a recent game and waiting for their next game.</summary> 
		public void CalcOnSite(IndexGames source1, IndexGames source2, Sessions sessions)
		{
			CalcInGame(source1);

			if (source2 != null)
				for (int g = 0; g < source2.Count; g++)
					for (int teamIndex = 0; teamIndex <= source2.MaxIndex; teamIndex++)
						this[g][teamIndex] |= source2[g].HasTeam(teamIndex);  // Note we use |= "or equals" here to add on to existing data, not to clear-and-set.

			for (int teamIndex = 0; teamIndex <= source1.MaxIndex; teamIndex++)
			{
				if (sessions.Count <= 1)
				{
					int first = FirstGame(teamIndex, 0, source1.Count - 1);
					MarkOnSite(teamIndex, ref first, LastGame(teamIndex, 0, source1.Count - 1));
				}
				else
				{
					int startGame = 0;  // Beginning of the current day.
					int session = sessions.FindIndex(s => s.First <= startGame);  // Session that begins the current day.

					while (startGame < source1.Count)
					{
						int endOfDayGame = sessions[sessions.NextEndOfDayIndex(session)].Last;

						int first = FirstGame(teamIndex, startGame, endOfDayGame);
						if (first != -1)
							MarkOnSite(teamIndex, ref first, LastGame(teamIndex, startGame, endOfDayGame));

						session = sessions.NextSessionIndex(endOfDayGame);
						startGame = endOfDayGame + 1;
					}
				}
			}
		}

		public int FirstGame(int teamIndex, int startGame, int endGame)
		{
			return this.FindIndex(startGame, endGame - startGame + 1, x => x[teamIndex]);
		}

		public int LastGame(int teamIndex, int startGame, int endGame)
		{
			return this.FindLastIndex(endGame, endGame - startGame, x => x[teamIndex]);
		}

		///<summary>
		/// Check 'count' spots ahead. If the team is in that game, mark the 
		/// team as being on site for all the games from 'first' to 'first + count'.
		/// If a team is in game 1 and game 4, they're likely to stay on site during games 2 and 3,
		/// so we mark them as being on site even though they're not playing in games 2 and 3.
		/// </summary> 
		private bool MarkOnSite(int teamIndex, ref int first, int last, int count)
		{
			if (!(first + count <= last && this[first + count][teamIndex]))
				return false;

			for (int game = first; game <= Math.Min(first + count - 1, last); game++)
				this[game][teamIndex] = true;
			first += count;

			return true;
		}

		/// <summary>
		/// Starting from a game we already know this team is in, look ahead up to 8 games, 
		/// and if we find one the team is in, mark them as being on site right through that whole group.</summary> 
		/// </summary>
		public void MarkOnSite(int teamIndex, ref int first, int last)
		{
			while (first < last)
				if (!(MarkOnSite(teamIndex, ref first, last, 2) ||
					  MarkOnSite(teamIndex, ref first, last, 3) ||
					  MarkOnSite(teamIndex, ref first, last, 4) ||
					  MarkOnSite(teamIndex, ref first, last, 5) ||
					  MarkOnSite(teamIndex, ref first, last, 6) ||
					  MarkOnSite(teamIndex, ref first, last, 7) ||
					  MarkOnSite(teamIndex, ref first, last, 8)))
				{
					first += 8;
					while (first < last && !this[first][teamIndex])
						first++;
				}
		}
	}

	/// <summary>Plays is a n × n int array, with each plays[a,b] representing the number of times team a plays team b.</summary>
	class Plays : List<List<int>>
	{
		/// <summary>Return a size × size Plays object, all zeroes.</summary>
		public Plays(int size)
		{
			for (int i = 0; i < size; i++)
				Add(Enumerable.Repeat(0, size).ToList());
		}

		/// <summary>From a league, return who plays who how many times.</summary>
		public Plays(League league, List<LeagueTeam> teams) : this(teams.Count)
		{
			var teamIndexes = new Dictionary<int?, int>();  // Dictionary: Key = TeamId, Value = index in List<LeagueTeam> teams.
			for (int i = 0; i < teams.Count; i++)
				teamIndexes.Add(teams[i].TeamId, i);

			var leagueGames = league.Games();
			foreach (var leagueGame in leagueGames)
			{
				for (int t1 = 0; t1 < leagueGame.Teams.Count; t1++)
					for (int t2 = t1 + 1; t2 < leagueGame.Teams.Count; t2++)
						if (teamIndexes.TryGetValue(leagueGame.Teams[t1].TeamId, out int index1) &&
							teamIndexes.TryGetValue(leagueGame.Teams[t2].TeamId, out int index2))
						{
							this[index1][index2]++;
							this[index2][index1]++;
						}
			}
		}

		public void ClearPlays()
		{
			for (int t1 = 0; t1 < Size; t1++)
				for (int t2 = 0; t2 < Size; t2++)
					this[t1][t2] = 0;

			average = -1;
		}

		public void CopyFrom(Plays source)
		{
			for (int t1 = 0; t1 < Size && t1 < source.Size; t1++)
				for (int t2 = 0; t2 < Size && t2 < source.Size; t2++)
					this[t1][t2] = source[t1][t2];

			average = source.average;
		}

		public void Calc(IndexGames games, Plays existingPlays, List<Colour> colours, bool rings)
		{
			if (existingPlays == null || existingPlays.Size == 0)
				ClearPlays();
			else
				CopyFrom(existingPlays);

			for (int g = 0; g < games.Count; g++)
			{
				var game = games[g];

				for (int t1 = 0; t1 < game.Count; t1++)
					for (int t2 = t1 + 1; t2 < game.Count; t2++)
						if (rings)
						{
							if (colours[t1] == colours[t2] || game[t1] == game[t2])
							{
								this[game[t1]][game[t2]]++;
								this[game[t2]][game[t1]]++;
							}
						}
						else
						{
							this[game[t1]][game[t2]]++;
							this[game[t2]][game[t1]]++;
						}
			}
		}

		private double average = -1;
		/// <summary>Average number of times each team plays each other team.</summary>
		public double Average()
		{
			if (average == -1)
			{
				if (!this.Any())
					average = 0.0;
				else
					average = 1.0 * this.Sum(x => x.Sum()) / Size / (Size - 1);
			}
			return average;
		}

		public int Size => this.Count;
	}

	/// <summary>All the things needed to initialise an Improver.</summary>
	class Bundle
	{
		public IndexGames PlayGames;
		public IndexGames RefGames;
		public ScoreScalers ScoreScalers;
		public Plays ExistingPlays;
		public Sessions Sessions;
		public List<Colour> Colours;
		public ShuffleType ShuffleType;
		public bool Rings;
	}

	/// <summary>Holder for a potential fixture, showing its games and its detailed score.</summary>
	class GamesResult
	{
		public IndexGames PlayGames;  // One list for teams that are playing in a game
		public IndexGames RefGames;  // and another for teams that are refereeing that game.
		public double Score = double.MaxValue;
		public string ScoreString;
		public string Details;
	}

	/// <summary>Holds settings for hill-climbing to find "good" possible fixtures.
	/// Each value is used to multiply that part of the score calculating for how "good" this fixture is.
	/// Bigger multipliers mean more weight to negative multipliers give that thing negative weight.</summary>
	class ScoreScalers
	{
		public int PlaySelf = 1000;  // This is the penalty for a team playing against itself. Set this high enough that this never happens.
		public int TimesPlayed = 50;  // A scaling factor for the number of times teams play other teams.
		public bool PastTimesPlayed = true;  // If true, when calculating TimesPlayed score, include already-played games. Good for league night.
		public int DayLength = 30;  // Attempts to minimise the amount of time each team is on site within a given day, by giving them games close together.
		public int NightLength = 3;  // Attempts to maximise the amount of time each team is off site between days. (No reward for night longer than 16 games.)
		public int NoGamesInSession = -1;  // No games in a session is good, so we reward that a little bit.
		public int OneGameInSession = 2;  // Exactly one game in a session is bad.
		public int TeamsOnSite = 1;  // We try to minimise the maximum number of teams that are on site.
		public int SameDifficulty = 1;  // For partial round robins, this ensures that all teams play opponents of approximately the same skill.
		public int CascadeDifficulty = 0;  // For cascades, this ensures that teams play opponents of approximately their own rank.
		public int PerfectColour = -1;  // If a team plays on each colour the same number of times, we reward that a little bit.
		public int BackToBack = 250;  // For each time a team plays two games in a row.
		public int RefThenPlay = 125;  // Referee a game then play the very next game.
		public int PlayThenRef = 100;  // Play a game then referee the very next game.
		public int RefereeClustering = 1;  // Incentivise grouping together a team's refereeing spots into groups of about 4 in a row.
	}

	public enum BreakType { WithinDay, Night };

	public class Session
	{
		/// <summary>Index of first game in this session.</summary>
		public int First;
		/// <summary>Index of last game in this session.</summary>
		public int Last;
		/// <summary>Start time of first game in this session.</summary>
		public DateTime Start;
		/// <summary>Time between game starts within the session.</summary>
		public TimeSpan Between;
		/// <summary>After this session, what type of break occurs?</summary>
		public BreakType Break;

		public Session Clone()
		{
			return new Session() { First = First, Last = Last, Start = Start, Between = Between, Break = Break };
		}
	}

	public class Sessions : List<Session>
	{
		/// <summary>Find the first session after the specified game number.</summary>
		public int NextSessionIndex(int fromGame)
		{
			return FindLastIndex(s => s.First >= fromGame);
		}

		/// <summary>Find the first session after the specified session, which ends a day.</summary>
		public int NextEndOfDayIndex(int startindex)
		{
			return FindIndex(startindex, s => s.Break != BreakType.WithinDay);
		}

		public Sessions Clone()
		{
			var clone = new Sessions();

			foreach (var session in this)
				clone.Add(session.Clone());

			return clone;
		}
	}

	/// <summary>Represents the length of a team's day or night, for later display to the user. Long days are bad; short nights are bad.</summary>
	class DayOrNightLength
	{
		public int TeamIndex;
		public int FirstGame;
		public int LastGame;
	}

	[Flags]
	public enum ShuffleType { None = 0, BetweenGames = 1, Games = 2, Rings = 4, WithinGames = 8, Referees = 16, Multiple = 32 };

	class SwapRecord
	{
		public ShuffleType ShuffleType;
		public int Game1;
		public int Game2;
		public int Team1;
		public int Team2;

		public override string ToString()
		{
			switch (ShuffleType)
			{
				case ShuffleType.BetweenGames: return string.Format("game {0} team {1} with game {2} team {3}", Game1 + 1, Team1 + 1, Game2 + 1, Team2 + 1);
				case ShuffleType.Games: return string.Format("games {0} and {1}", Game1 + 1, Game2 + 1);
				case ShuffleType.Rings: return string.Format("game {0} ring {1} with game {2} ring {3}", Game1 + 1, Team1 + 1, Game2 + 1, Team2 + 1);
				case ShuffleType.WithinGames: return string.Format("game {0} team {1} with team {2}", Game1 + 1, Team1 + 1,	Team2 + 1);
				case ShuffleType.Referees: return string.Format("game {0} referee {1} with game {2} referee {3}", Game1 + 1, Team1 + 1, Game2 + 1, Team2 + 1);
				case ShuffleType.Multiple: return "multiple swaps";
				default: return "none";
			}
		}
	}
}
