using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Torn.Report;
using Torn5.Controls;
using Zoom;

namespace Torn
{
	/// <summary>Creates a pyramid fixture with several rounds and repêchages. Outputs a report showing that fixture.</summary>
	class Pyramid
	{
		public List<PyramidFixture> Rounds { get; } = new List<PyramidFixture>();

		private List<int> planBHeights = new List<int>();

		/// <summary>Returns a report showing a pyramid fixture with several rounds and repêchages. Each round (or repêchage) is a column in the report.</summary>
		public ZoomReport Report(string title, int finalsGames, int finalsTeams)
		{
			var roundsAndReps = new List<PyramidHalfFixture>();
			for (int i = 0; i < Rounds.Count; i++)
			{
				roundsAndReps.Add(Rounds[i].FixtureRound);
				if (Rounds[i].HasRepechage)
					roundsAndReps.Add(Rounds[i].FixtureRepechage);
			}

			var maxPlayers = Math.Max(roundsAndReps.Max(r => (double)r.TeamsIn / r.Games), finalsTeams);

			var report = new ZoomReport(title + " Pyramid");
			report.Colors.BackgroundColor = Color.Empty;
			report.Colors.OddColor = Color.Empty;

			var gameColumns = new List<ZColumn>();
			report.SameWidths.Add(gameColumns);

			planBHeights.Clear();
			for (int i = 0; i < roundsAndReps.Count; i++)
				AddRound(report, gameColumns, roundsAndReps, i, maxPlayers);

			for (int i = 0; i < finalsGames; i++)
			{
				gameColumns.Add(report.AddColumn(new ZColumn(((char)((int)'A' + i)).ToString(), ZAlignment.Center, "Finals")));
				GameCell(report.Rows.Force(0), report.Columns.Count - 1, finalsTeams);
			}

			var sb = new StringBuilder();

			for (int i = 0; i < Rounds.Count; i++)
			{
				sb.Append(Rounds[i].Description());

				if (i < Rounds.Count - 1)
					sb.Append("\r\n\r\n\r\n");
			}
			report.Description = sb.ToString();

			return report;
		}

		/// <summary>Add a round (or repechage) to our report. Tis involves adding one column for the boxes for the games for this round, and one column to hold the arrows showing how teams leave this round and enter the next.</summary>
		private void AddRound(ZoomReport report, List<ZColumn> gameColumns, List<PyramidHalfFixture> roundsAndReps, int r, double maxPlayers)
		{
			PyramidHalfFixture thisRound = roundsAndReps[r];

			if (thisRound.Games == 0)
			{
				planBHeights.Add(-1);
				return;
			}

			gameColumns.Add(report.AddColumn(new ZColumn("game".CountPluralise(thisRound.Games), ZAlignment.Center, thisRound.Title)));

			var arrowColumn = report.AddColumn(new ZColumn("", ZAlignment.Center, ""));
			var arrow = new Arrow();  // This arrow shows teams leaving this round, and skipping ahead, going to next round or repechage, or being eliminated.
			arrowColumn.Arrows.Add(arrow);

			PyramidHalfFixture previousRound = r == 0 ? null : roundsAndReps[r - 1];
			if (!thisRound.IsRound)  // This is a repechage
				arrow.From.Add(new ZArrowEnd(0, Scale(previousRound.Advance, maxPlayers)) { Expand = true });  // so add a From for teams traveling directly from previous round.

			// Draw a cell for each game in this round, and a "From" arrow from each game into our multi-headed arrow.
			int thisOffset = thisRound.IsRound ? 0 : 1;  // If this is a repechage, add a row of offset to make room for the previous To arrowhead for teams skipping past this rep.
			for (int i = 0; i < thisRound.Games; i++)
			{
				GameCell(report.Rows.Force(i + thisOffset), report.Columns.Count - 2, (thisRound.TeamsIn * thisRound.GamesPerTeam + thisRound.Games - i - 1) / thisRound.Games);
				arrow.From.Add(new ZArrowEnd(i + thisOffset, Scale((double)thisRound.TeamsIn / thisRound.Games, maxPlayers)));
			}

			if (r >= 1 && thisRound.IsRound && previousRound.PlanB > 0)
				arrow.From.Add(new ZArrowEnd(planBHeights[r - 1], Scale(previousRound.PlanB, maxPlayers)) { Expand = true });  // Add an arrowhead from previous Plan B if required.

			// Add "To" heads to the arrow.
			PyramidHalfFixture nextRound = r == roundsAndReps.Count - 1 ? null : roundsAndReps[r + 1];
			int toOffset = nextRound == null || nextRound.IsRound ? 0 : 1;
			int planBOffset = 0;
			int planBHeight = -1;
			if (nextRound == null)  // Next is finals
				arrow.To.Add(new ZArrowEnd(0, Scale(thisRound.Advance + (previousRound?.Advance ?? 0), maxPlayers)));  // so add a single simple To head pointing to finals.
			else
			{
				if (!nextRound.IsRound)  // Next is a repechage
					arrow.To.Add(new ZArrowEnd(0, Scale(thisRound.Advance, maxPlayers)) { Expand = true });  // so add a To for teams traveling directly to next round.

				// Add one head for each game in the next round.
				double teamsOut = nextRound.IsRound ? (previousRound?.Advance ?? 0) + thisRound.Advance : thisRound.TeamsOut;
				for (int i = 0; i < nextRound.Games; i++)
					arrow.To.Add(new ZArrowEnd(i + toOffset, Scale(teamsOut / nextRound.Games, maxPlayers)));

				if (thisRound.PlanB > 0)
				{
					planBHeight = nextRound.Games + toOffset;
					arrow.To.Add(new ZArrowEnd(planBHeight, Scale(thisRound.PlanB, maxPlayers)) { Expand = true });  // Add a Plan B arrowhead if required.
					planBOffset = 1;
				}
			}

			if (nextRound == null || thisRound.Eliminate > 0)  // This round has eliminations
			{
				var elimRow = thisRound.Games + thisOffset;
				if (nextRound != null)
					elimRow = Math.Max(elimRow, nextRound.Games + toOffset + planBOffset);

				arrow.To.Add(new ZArrowEnd(elimRow, Scale(thisRound.Eliminate, maxPlayers)));  // so add an arrow for eliminated teams
				var row = report.Rows.Force(elimRow);
				var cell = row.Force(report.Columns.Count);  // and a cell representing that elimination.
				cell.Text = "\u274c";
			}

			planBHeights.Add(planBHeight);
		}

		/// <summary>Return a width for an arrow, scaled to the max number of players per game, but at least 1 pixel and at most 5 pixels wide.</summary>
		double Scale(double players, double maxPlayers)
		{
			return Math.Max(Math.Min(players, maxPlayers) / maxPlayers * 5, 1);
		}

		void GameCell(ZRow row, int col, int value)
		{
			var cell = row.Force(col);
			cell.TextColor = Color.LightGray;
			cell.Color = Color.White;
			cell.Border = Color.Black;
			cell.Text = value.ToString();
		}

		/// <summary>
		/// Set all rounds and repêchages to a similar advance percentage; i.e. all games are approximately the same difficulty.
		/// Number of teams and games in each round and repêchage are adjusted accordingly.
		/// </summary>
		public double Idealise(int desiredTeamsPerGame, int teams)
		{
			double advanceRatePerRound = Math.Pow((double)desiredTeamsPerGame / teams, 1.0 / Rounds.Count);
			double advanceRatePerPartRound = 1 - Math.Sqrt(1 - advanceRatePerRound);

			for (int i = 0; i < Rounds.Count; i++)
			{
				Rounds[i].DesiredTeamsPerGame = desiredTeamsPerGame;
				Rounds[i].Idealise(advanceRatePerPartRound);

				if (i < Rounds.Count - 1)
					Rounds[i + 1].TeamsIn = Rounds[i].TeamsOut;
			}

			return advanceRatePerPartRound;
		}
	}

	/// <summary>Create a draw for the next round or repechage in a pyramid-shaped tournament..</summary>
	class PyramidDraw
	{
		/// <summary>
		/// If true, compare teams on rank, then on victory points then score.
		/// Otherwise, compare on victory points then score then rank. 
		/// </summary>
		public bool CompareRank { get; set; }

		/// <summary>If true, take best teams from each game. If false, take worst teams from each game.</summary>
		public bool TakeTop { get; set; }

		public League League { get; set; }

		/// <summary>Build a list of at most (teamsFromRound) teams from the previous round followed by at most (teamsFromRepechage) teams from the previous repêchage.</summary>
		TeamsPlays BuildList(List<PyramidGame> pyramidGames, int teamsFromRound, int teamsFromRepechage, int teamsFromPlanB)
		{
			var roundList = BuildOneList(pyramidGames, Priority.Round, teamsFromRound);
			roundList.AddRange(BuildOneList(pyramidGames, Priority.Repechage, teamsFromRepechage));
			roundList.AddRange(BuildOneList(pyramidGames, Priority.PlanB, teamsFromPlanB));

			return roundList;
		}

		/// <summary>Iterate through the games we've been passed. If a specific number of teams are marked to be taken, take them. Then GetRest() ensures we have length teams.</summary>
		TeamsPlays BuildOneList(IEnumerable<PyramidGame> pyramidGames, Priority priority, int length)
		{
			var list = new TeamsPlays();

			// Get from each relevant game the number of teams that are marked to be taken from that game.
			foreach (var pg in pyramidGames.Where(p => p.Priority == priority))
				if (pg.TeamsToTake is int take)
				{
					if (priority == Priority.PlanB)
						AddNPlanB(list, list, pyramidGames, pg.Game, take);
					else
						list.AddN(League, pg.Game, TakeTop, pg.Priority, 0, take);
				}

			GetRest(list, pyramidGames, priority, length);

			return list;
		}

		/// <summary>From a single game, add teams that _haven't_ appeared in Round or Repêchage.
		/// Note that I'm not respecting TakeTop for Plan B: we use Plan B when building a repêchage, which is typically take bottom
		/// from the previous round, plus take top teams that didn't make it to that round from the _previous_ repêchage.</summary>
		void AddNPlanB(TeamsPlays list, TeamsPlays otherList, IEnumerable<PyramidGame> pyramidGames, Game game, int? TeamsToTake)
		{
			var nonPlanBGames = pyramidGames.Where(pg => pg.Priority != Priority.PlanB);

			for (int i = 0, taken = 0; i < game.Teams.Count && taken < TeamsToTake; i++)
			{
				var gameTeam = game.Teams[i];

				if (!nonPlanBGames.Any(pg => pg.Game.Teams.Any(t => t.TeamId == gameTeam.TeamId)))  // If this team hasn't appeared in Round or Repêchage,
				{
					if(!list.Any(tps => tps.TeamId == gameTeam.TeamId && tps.Any(tp => tp.Game == game)) && 
						!otherList.Any(tps => tps.TeamId == gameTeam.TeamId && tps.Any(tp => tp.Game == game)))  // and we don't already have it listed,
					{
						list.Add(League, game, gameTeam, Priority.PlanB);  // add it.
						if (League.LeagueTeam(gameTeam).Active)
							taken++;
					}
				}
			}
		}

		/// <summary>If necessary, get the remaining teams from the specified games. Sort so that the already-selected teams are first,
		/// and every team is in the right order. Truncate the list so that it has the right number of active teams in it.</summary>
		void GetRest(TeamsPlays list, IEnumerable<PyramidGame> pyramidGames, Priority priority, int length)
		{
			var comparer = new TeamPlaysComparer() { CompareOnRank = CompareRank };
			list.Sort(comparer);

			// If we already have too many items in the list, remove some.
			int activeCount = list.Count(t => League.LeagueTeam(t.TeamId).Active);
			if (activeCount > length)
			{
				if (TakeTop)
					list.RemoveRange(length, activeCount - length);
				else
					list.RemoveRange(0, length);
			}

			// If we don't have enough items, add some from any games which don't specify how many teams to take from that game.
			else if (activeCount < length)
			{
				// Build the tail list: all the teams that didn't get brought in before.
				var tail = new TeamsPlays();
				foreach (var pg in pyramidGames.Where(p => p.Priority == priority))
					if (priority == Priority.PlanB)
						AddNPlanB(tail, list, pyramidGames, pg.Game, Int32.MaxValue);
					else
						tail.AddN(League, pg.Game, TakeTop, pg.Priority, pg.TeamsToTake ?? 0, pg.Game.Teams.Count);

				tail.Sort(comparer);

				// Build the take list: enough active teams to get us up to length, plus any withdrawn teams we come across while we do that.
				var take = new TeamsPlays();

				int toAdd = length - activeCount;
				int i = 0;
				while (take.Count(t => League.LeagueTeam(t.TeamId).Active) < toAdd && i < tail.Count)
				{
					take.Add(tail[TakeTop || priority == Priority.PlanB ? i : tail.Count - i - 1]);
					i++;
				}
				take.Sort(comparer);

				// Then add in the take.
				list.AddRange(take);
			}
		}

		TeamsPlays TeamsNotTaken(TeamsPlays taken, IEnumerable<PyramidGame> pyramidGames)
		{
			var notTaken = new TeamsPlays();

			foreach (var pg in pyramidGames)
				foreach (var gt in pg.Game.Teams)
					if (!taken.Any(tg => tg.TeamId == gt.TeamId))
						notTaken.Add(League, pg.Game, gt, Priority.Unmarked);

			notTaken.Sort(new TeamPlaysComparer() { CompareOnRank = CompareRank });

			return notTaken;
		}

		private static readonly Random random = new Random();

		/// <summary>Generate two reports. The past report highlights the teams which will advance. The draw report shows which teams are in which game for the next round or repechage.</summary>
		public (ZoomReport past, ZoomReport draw) Reports(List<PyramidGame> pyramidGames, int games, int teamsFromRound, int teamsFromRepechage, int teamsFromPlanB, string title, bool colour)
		{
			var teamsPlays = BuildList(pyramidGames, teamsFromRound, teamsFromRepechage, teamsFromPlanB);  // This is the data source from which both reports will be built.

			// Build the "past" report showing which teams made the cut: one row per team, one game per column.
			var pastReport = new Zoom.ZoomReport("From", "Rank,Team", "center,left");

			var times = pyramidGames.Select(pg => pg.Game.Time);
			bool oneDay = times.Any() && times.Min().Date == times.Max().Date;

			// Add columns for each game used from pyramidGames.
			foreach (var pg in pyramidGames.Where(pg => pg.Priority != Priority.Unmarked))
				pastReport.Columns.Add(new Zoom.ZColumn(pg.Game.Time.ToShortTimeString(), Zoom.ZAlignment.Left, pg.Game.Title + " " + (oneDay ? "" : Utility.FriendlyDate(pg.Game.Time))) { Tag = pg });

			pastReport.Columns.Add(new ZColumn("Average", ZAlignment.Right));

			// Put all the rows in the past report. If we're taking the top teams, list them first; otherwise, list them last.
			if (TakeTop)
				AddPastRows(pastReport, teamsPlays);
			AddPastRows(pastReport, TeamsNotTaken(teamsPlays, pyramidGames));
			if (!TakeTop)
				AddPastRows(pastReport, teamsPlays);

			for (int r = 0; r < pastReport.Rows.Count; r++)
				pastReport.Rows[r][0].Number = r + 1;  // Rank.

			// Build the Draw report: one future game per column.
			var drawReport = new Zoom.ZoomReport(title);

			for (int col = 1; col <= games; col++)
				drawReport.Columns.Add(new Zoom.ZColumn("Game " + col.ToString(), Zoom.ZAlignment.Left));

			var activeTeams = teamsPlays.Where(t => League.LeagueTeam(t.TeamId).Active).ToList();

			// Pull off activeTeams into drawReport, either from the low end or the high end.
			int i = 0;
			while (i < activeTeams.Count)
			{
				var row = drawReport.AddRow(new Zoom.ZRow());

				if (i + games <= activeTeams.Count)  // This is a complete row.
					for (int j = 0; j < games; j++)
					{
						LeagueTeam team;
						int rowNum = drawReport.Rows.Count;
						if (rowNum % 2 == 1)
							team = League.LeagueTeam(activeTeams[i].TeamId);  // Zig.  Run this row forward: team1 in game 1, etc.
						else
							team = League.LeagueTeam(activeTeams[rowNum * games - j - 1].TeamId);  // Zag.  Run this row backwards: first team in last game, etc.

						if (team != null)
							row.Add(new Zoom.ZCell(team.Name));
						else
							row.Add(new Zoom.ZCell("---"));

						i++;
					}
				else  // This is the final, partial row.
					for (int j = 0; j < games; j++)
					{
						if (i < activeTeams.Count)
						{
							LeagueTeam team = League.LeagueTeam(activeTeams[i].TeamId);
							if (team != null)
								row.Add(new Zoom.ZCell(team.Name));
							else
								row.Add(new Zoom.ZCell("--"));
						}
						else
							row.Add(new Zoom.ZCell("-"));

						i++;
					}
			}

			if (colour)
			{
				List<Colour> colours = new List<Colour>() { Colour.Red, Colour.Blue, Colour.Green, Colour.Yellow, Colour.Cyan, Colour.Pink, Colour.Purple, Colour.Orange };  // This array has the hardest to distinguish colours last, so they get used the least.
				var coloursUsed = pyramidGames.SelectMany(pg => pg.Game.Teams.Select(t => t.Colour)).Distinct();

				colours.RemoveAll(c => !coloursUsed.Any(u => u == c));

				for (int col = 0; col < drawReport.Columns.Count; col++)  // For each column,
					for (int rowGroup = 0; rowGroup < drawReport.Rows.Count; rowGroup += colours.Count)  // For each group of rows,
					{
						int remaining = Math.Min(drawReport.Rows.Count - rowGroup, colours.Count);
						var shuffledColours = colours.Take(remaining).OrderBy(x => random.Next()).ToList();  // create shuffled colours,
						for (int row = 0; row < remaining; row++)
							drawReport.Rows[rowGroup + row][col].Color = shuffledColours[row].ToColor();  // and assign them to cells.
					}
			}

			return (pastReport, drawReport);
		}

		/// <summary>Add rows to the Past report: one row per team in teamsPlays. Add cells for that team's score. Colour them for the Priority of the game they came from.</summary>
		void AddPastRows(ZoomReport pastReport, TeamsPlays teamsPlays)
		{
			bool hasPoints = teamsPlays.Any(tg => tg.AveragePoints() > 0);

			foreach (var teamPlays in teamsPlays)
			{
				var row = pastReport.AddRow(new Zoom.ZRow());
				row.Add(new Zoom.ZCell(0));  // Blank-for-now Rank.

				var team = League.LeagueTeam(teamPlays.TeamId);
				var priorities = teamPlays.Select(tg => tg.Priority).Distinct();

				var color = !team.Active ? Priority.Withdrawn.ToColor() :
							priorities.Count() == 1 ? priorities.First().ToColor() :
							Color.Red;

				row.Add(new Zoom.ZCell(team.Name, color));  // Team

				foreach (var pg in pastReport.Columns.Where(c => c.Tag is PyramidGame).Select(c => c.Tag as PyramidGame))
				{
					var teamPlay = teamPlays.Find(tg => tg.Game == pg.Game);
					if (teamPlay == null)
						row.Add(new Zoom.ZCell());
					else
						row.Add(FillOnePastCell(teamPlay, hasPoints, teamPlay.Priority.ToColor()));
				}

				row.Add(FillOnePastCell(teamPlays.AverageRank(), teamPlays.AverageScore(), teamPlays.AveragePoints(), hasPoints, default, "n1"));
			}
		}

		ZCell FillOnePastCell(TeamPlay teamPlay, bool hasPoints, Color color)
		{
			return FillOnePastCell(teamPlay.Game.Rank(teamPlay.GameTeam), teamPlay.GameTeam.Score, teamPlay.GameTeam.Points, hasPoints, teamPlay.Priority.ToColor());
		}

		ZCell FillOnePastCell(double rank, double score, double points, bool hasPoints, Color color, string numberFormat = "")
		{
			var cell = new ZCell(score) { Color = color };
			if (!string.IsNullOrEmpty(numberFormat))
				cell.NumberFormat = numberFormat;

			if (CompareRank || hasPoints)
			{
				string format = CompareRank && hasPoints ? "{0}: {1}; {2}" :
					CompareRank ? "{0}: {1}" :
					hasPoints ? "{1}; {2}" : "";

				cell.Text = string.Format(format, rank.Ordinate(), score, points);
			}

			return cell;
		}
	}

	/// <summary>Used to mark from which games teams were taken for future rounds. Teams from games marked Round are ranked higher into 
	/// the next round than teams from games marked Repêchage, which are in turn higher than Plan B.</summary>
	public enum Priority { Withdrawn = 0, Unmarked, PlanB, Repechage, Round };

	public static class PriorityExtensions
	{
		public static Color ToColor(this Priority priority)
		{
			Color[] Colors = { Color.FromArgb(0xFF, 0xA0, 0xA0), Color.Empty, Color.FromArgb(0xFF, 0xD0, 0xA0), Color.FromArgb(0xFF, 0xFF, 0x90), Color.FromArgb(0xA0, 0xFF, 0xA0) };  // Red, None, Orange, Yellow, Green.

			return Colors.Valid((int)priority) ? Colors[(int)priority] : Color.Empty;
		}
	}

	/// <summary>A game from the pyramid round form's listViewGames.</summary>
	public class PyramidGame
	{
		public Game Game { get; set; }
		public int? TeamsToTake { get; set; }
		public Priority Priority { get; set; }
	}

	/// <summary>A single team in a single PyramidGame.</summary>
	internal class TeamPlay
	{
		public Game Game { get; set; }
		public GameTeam GameTeam { get; set; }
		public Priority Priority { get; set; }
	}

	/// <summary>A single team, with potentially several TeamPlay entries, each representing that team playing in one Game.</summary>
	internal class TeamPlays : List<TeamPlay>
	{
		public int TeamId { get; set; }

		public double AverageScore() => this.Any() ? this.Average(tp => tp.GameTeam.Score) : double.MinValue;
		public double AveragePoints() => this.Any() ? this.Average(tp => tp.GameTeam.Points) : double.MinValue;
		public double AverageRank() => this.Any() ? this.Average(tp => tp.Game.Teams.FindIndex(gt => gt.TeamId == TeamId)) + 1 : double.MaxValue;
	}

	/// <summary>One entry per LeagueTeam that we've seen in the selected games.</summary>
	internal class TeamsPlays : List<TeamPlays>
	{
		/// <summary>Add one team play to to that team's list of plays. If necessary, add the team so we have a place to add the play.</summary>
		public void Add(League league, Game game, GameTeam gt, Priority priority)
		{
			if (gt.TeamId == null)
				return;

			var teamPlays = Find(tg => tg.TeamId == gt.TeamId);
			if (teamPlays == null && gt.TeamId != null)  // If this team doesn't have an entry yet,
				Add(teamPlays = new TeamPlays() { TeamId = (int)gt.TeamId });  // make one and add it.

			var team = league.LeagueTeam(gt);
			if (!team.Active)
				priority = Priority.Withdrawn;

			teamPlays.Add(new TeamPlay() { Game = game, GameTeam = gt, Priority = priority });  // add this play for this team.
		}

		/// <summary>Add _n_ teams from this game to our list, skipping over the first _skip_ teams.</summary>
		public void AddN(League league, Game game, bool topFirst, Priority priority, int skip, int n)
		{
			if (topFirst) // Take the top N.
				for (int i = skip; i < skip + n && i < game.Teams.Count; i++)
					Add(league, game, game.Teams[i], priority);
			else  // Take the bottom N.
				for (int i = game.Teams.Count - skip - 1; i >= game.Teams.Count - skip - n && i >= 0; i--)
					Add(league, game, game.Teams[i], priority);
		}
	}

	class TeamPlaysComparer : IComparer<TeamPlays>
	{
		/// <summary>Sorted list of league teams.</summary>
		public List<LeagueTeam> LeagueTeams { get; set; }
		public bool CompareOnRank { get; set; }

		/// If CompareOnRank then compare teams on rank, then on victory points then score.
		/// Otherwise, compare on victory points then score then rank.
		public int Compare(TeamPlays x, TeamPlays y)
		{
			double result = CompareOnRank ? x.AverageRank() - y.AverageRank() : 0;

			if (result == 0)
				result = y.AveragePoints() - x.AveragePoints();

			if (result == 0)
				result = y.AverageScore() - x.AverageScore();

			if (result == 0)
				result = x.AverageRank() - y.AverageRank();

			return Math.Sign(result);
		}
	}
}
