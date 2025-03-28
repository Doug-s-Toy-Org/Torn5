﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Torn;
using Torn.Report;
using Torn5;
using Zoom;

namespace Torn.UI
{
	/// <summary>
	/// Allow users to create or import tournament fixtures.
	/// </summary>
	public partial class FormFixture : Form
	{
		Holder holder;
		public Holder Holder
		{
			get { return holder; }

			set
			{
				holder = value;

				if (frameFinals1 != null)
					frameFinals1.Games = holder.League.AllGames;
				if (framePyramid1 != null)
					framePyramid1.Holder = holder;
				if (framePyramidRound1 != null)
					framePyramidRound1.Holder = holder;

				SetExportFileName();
			}
		}

		string exportFolder;
		public string ExportFolder
		{
			get { return exportFolder; }

			set
			{
				exportFolder = value;

				SetExportFileName();
			}
		}

		private void SetExportFileName()
		{
			if (!string.IsNullOrEmpty(exportFolder) && holder != null)
				printReport1.FileName = System.IO.Path.Combine(ExportFolder, holder.Key, "fixture." + holder.ReportTemplates.OutputFormat.ToExtension());
		}

		private List<List<int>> previousGrid;
		private double previousBestScore;
		private double previousGamesPerTeam;
		private bool previousHasRef;
		private List<List<int>> previousExistingPlays;
		private List<CheckBox> teamSelectors = new List<CheckBox>();
		private List<CheckBox> fixtureTeamSelectors = new List<CheckBox>();
		private List<LeagueTeam> selectedTeams = new List<LeagueTeam>();
		private List<LeagueTeam> fixtureSelectedTeams = new List<LeagueTeam>();

		Colour leftButton, middleButton, rightButton, xButton1, xButton2;
		Point point;  // This is the point in the grid last clicked on. It's counted in grid squares, not in pixels: 9,9 is ninth column, ninth row.
		bool resizing;
		bool loading;

		public FormFixture()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();

			datePicker.Value = DateTime.Now.Date;
			timePicker.CustomFormat = CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern;
			DateTime dt = DateTime.Now;
			gameDateTime.Value = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind).AddMinutes(1);  // Don't include seconds / milliseconds.

			leftButton = Colour.Red;
			middleButton = Colour.Blue;
			rightButton = Colour.Green;
			xButton1 = Colour.Yellow;
			xButton2 = Colour.Purple;
			point = new Point(-1, -1);
			resizing = false;
		}

		void ButtonClearClick(object sender, EventArgs e)
		{
			Holder.Fixture.Games.Clear();
			displayReportGames.Report = null;
			displayReportGrid.Report = null;
		}

		void LogGrid<T>(List<List<T>> grid)
		{
			string str = "**********\n";
			foreach (List<T> list in grid)
			{
				foreach (T item in list)
				{
					str += item + ",\t";
				}
				str += "\n";
			}
			str += "**********";
			Console.WriteLine(str);
		}

		List<T> FlattenGrid<T>(List<List<T>> grid)
		{
			List<T> flat = new List<T>();
			foreach (List<T> list in grid)
			{
				flat.AddRange(list);
			}
			return flat;
		}

		List<List<T>> TransposeGrid<T>(List<List<T>> grid)
		{
			return grid.SelectMany(inner => inner.Select((item, index) => new { item, index }))
				.GroupBy(i => i.index, i => i.item)
				.Select(g => g.ToList())
				.ToList();
		}

		List<List<int>> GetGrid(FixtureTeams teams, double teamsPerGame, double gamesPerTeam, bool hasRef, List<List<int>> existingPlays, int maxMillis)
		{
			List<List<int>> bestGrid = SetupGrid(teams, teamsPerGame, gamesPerTeam);

			double bestScore = CalcScore(bestGrid, gamesPerTeam, hasRef, existingPlays);

			return ContinueMixing(bestGrid, gamesPerTeam, hasRef, existingPlays, maxMillis, bestScore);

		}

		List<List<int>> ContinueMixing(List<List<int>> grid, double gamesPerTeam, bool hasRef, List<List<int>> existingPlays, int maxMillis, double startingScore)
		{
			List<List<int>> bestGrid = grid;
			double bestScore = startingScore;
			bool badFixture = bestScore > 10000;

			Console.WriteLine("initScore: {0}", bestScore);
			LogGrid(bestGrid);

			Stopwatch sw = new Stopwatch();
			sw.Start();
			scoreLabel.Visible = false;
			scoreLabel.Text = "";
			int count = 0;

			for (int i = 0; i < 20000; i++)
			{
				count++;
				List<List<int>> mixedGrid = MixGrid(bestGrid);
				double score = CalcScore(mixedGrid, gamesPerTeam, hasRef, existingPlays);
				if (score <= bestScore)
				{
					bestScore = score;
					bestGrid = mixedGrid;
					if (bestScore > 10000)
					{
						badFixture = true;
					}
					else
					{
						badFixture = false;
					}
				} 
			}

			while (badFixture)
			{
				count++;
				if (sw.ElapsedMilliseconds > maxMillis)
					break;
				List<List<int>> mixedGrid = MixGrid(bestGrid);
				double score = CalcScore(mixedGrid, gamesPerTeam, hasRef, existingPlays);
				if (score <= bestScore)
				{
					bestScore = score;
					bestGrid = mixedGrid;
					if (bestScore > 10000)
					{
						badFixture = true;
					}
					else
					{
						badFixture = false;
					}
				}
			}
			CalcScore(bestGrid, gamesPerTeam, hasRef, existingPlays, true);
			Console.WriteLine("Time Elapsed (ms): {0}", sw.ElapsedMilliseconds);
			Console.WriteLine("Iterations: {0}", count);

			Console.WriteLine("bestScore: {0}", bestScore);
			LogGrid(bestGrid);
			Console.WriteLine("Existing Plays");
			LogGrid(NormalisePlays(existingPlays));
			Console.WriteLine("Plays");
			LogGrid(NormalisePlays(CalcPlays(bestGrid, hasRef, existingPlays)));
			scoreLabel.Text = bestScore >= 10000 ? "Extend max time and regenerate: " + Math.Round(bestScore).ToString() : "Score: " + Math.Round(bestScore).ToString() + " (Lower is better)";
			scoreLabel.BackColor = bestScore >= 10000 ? Color.FromName("red") : Color.Transparent;
			scoreLabel.Visible = true;

			previousGrid = bestGrid;
			previousBestScore = bestScore;
			previousGamesPerTeam = gamesPerTeam;
			previousHasRef = hasRef;
			previousExistingPlays = existingPlays;
			continueGenerating.Enabled = true;


			return bestGrid;
		}

		List<List<int>> MixGrid(List<List<int>> grid)
		{
			List<List<int>> newGrid = grid.ConvertAll(row => row.ConvertAll(cell => cell));
			double teamsPerGame = grid[0].Count;
			double totalTeams = FlattenGrid(grid).Uniq().Count;
			Random rnd = new Random();

			int game1 = rnd.Next(grid.Count);
			int game2 = rnd.Next(grid.Count);

			int player1 = rnd.Next((int)teamsPerGame);
			int player2 = rnd.Next((int)teamsPerGame);

			newGrid[game1][player1] = grid[game2][player2];
			newGrid[game2][player2] = grid[game1][player1];

			return newGrid;
		}

		List<int> SumRows(List<List<int>> grid )
		{
			List<int> totals = new List<int>();
			foreach(List<int> row in grid)
			{
				int total = 0;
				foreach(int value in row)
				{
					total += value;
				}
				totals.Add(total);
			}
			return totals;
		}

		int AverageRow(List<int> row)
		{
			int total = 0;
			foreach (int value in row)
			{
				total += value;
			}

			return total / row.Count;
				
		}

		List<List<int>> NormalisePlays(List<List<int>> grid)
		{
            List<int> playsTotals = SumRows(grid);
            double mostPlays = 0;
			List<List<int>> updatedGrid = new List<List<int>>();

            foreach (int totalPlays in playsTotals)
            {
                if (totalPlays > mostPlays)
                {
                    mostPlays = totalPlays;
                }
            }


			for (int i = 0; i < grid.Count; i++)
			{
				List<int> row = grid[i];
				List<int> newRow = new List<int>();
				double teamPlays = playsTotals[i];
				double multiplier = teamPlays == 0 || mostPlays == 0 ? 1 : mostPlays / teamPlays;

				for(int j =  0; j < row.Count; j++)
				{
                    double teamPlays2 = playsTotals[j];
                    double multiplier2 = teamPlays2 == 0 || mostPlays == 0 ? 1 : mostPlays / teamPlays2;
					double mult = Math.Max(multiplier, multiplier2);
					double result = mult * row[j];
                    newRow.Add((int)Math.Round(result));
				}
				updatedGrid.Add(newRow);
			}
			return updatedGrid;
        }

		double GetAveragePlays(List<List<int>> grid)
		{
			double total = 0;
            double count = 0;
			for(int i = 0; i < grid.Count; i++)
			{
				for(int j = 0; j < grid[i].Count; j++)
				{
					if(i != j)
					{
						total += grid[i][j];
						count++;
					}
				}
			}
			return total / count;
		}

		double CalcScore(List<List<int>> grid, double gamesPerTeam, bool hasRef, List<List<int>> existingPlays, bool log = false)
		{
			List<List<int>> normalisedExistingPlays = NormalisePlays(existingPlays);
			int totalTeams = FlattenGrid(grid).Uniq().Count;
			int BACK_TO_BACK_PENALTY = (int)backToBackPenalty.Value;
			int teamsPerGame = grid[0].Count;
			double previousAveragePlays = GetAveragePlays(normalisedExistingPlays);
			List<List<int>> plays = NormalisePlays(CalcPlays(grid, hasRef, existingPlays));
			double averagePlays = GetAveragePlays(plays);

			double score = 0;

			for (int player1 = 0; player1 < plays.Count; player1++)
			{
				score += plays[player1][player1] * 100000; // penalty for playing themselves
				for(int player2 = player1 + 1; player2 < plays[player1].Count; player2++)
				{
					score += Math.Pow(plays[player1][player2] - averagePlays, 4);
				}
			}

			List<List<int>> transposedGrid = TransposeGrid(grid);

			//penalty for same colour
			foreach (List<int> colour in transposedGrid)
			{
				int numberOfGames = colour.Count;
				int numberOfColours = transposedGrid.Count;
				int uniqueTeams = colour.Uniq().Count;
				double gamesOnEachColour = gamesPerTeam / numberOfColours;
				int penalties = colour.FindAll(team => colour.FindAll(t => t == team).Count > gamesOnEachColour).Count;

				double penalty = penalties * 100000;
				score += penalty;
			}

			foreach(List<int> row in grid)
			{
				int uniquePlayers = row.Uniq().Count;
				score += Math.Abs(uniquePlayers - teamsPerGame) * 100000; // penalty for playing themselves
			}

			for (int game = 0; game < grid.Count - 1; game++)
			{
				foreach (var team in grid[game])
				{
					var nextGame = grid[game + 1];
					if(nextGame.Contains(team))
					{
						var isRefTho = hasRef && (nextGame.IndexOf(team) == (nextGame.Count - 1) || grid[game].IndexOf(team) == (grid[game].Count - 1));
						if (log) { Console.WriteLine("team " + team + " is in games " + game + " and " + (game + 1) + " back to back" + (isRefTho ? " but one is a ref game" : "")); }
						score += BACK_TO_BACK_PENALTY - (isRefTho ? BACK_TO_BACK_PENALTY * (0.5) : 0);
					}
				}
			}

			return score;
		}

		List<List<int>> AddGrids(List<List<int>> grid1, List<List<int>> grid2)
		{
			
			if (grid1.Count > 0 && grid1.Count == grid2.Count && grid1[0].Count == grid2[0].Count)
			{
				List<List<int>> grid3 = new List<List<int>>();
				for (int i = 0; i < grid1.Count; i++)
				{
					List<int> row = new List<int>();
					for (int j = 0; j < grid1[i].Count; j++)
					{
						if(grid2.Count > i && grid2[i].Count > j)
						{
							row.Add(grid1[i][j] + grid2[i][j]);
						} else
						{
							row.Add(grid1[i][j]);
						}
					}
					grid3.Add(row);
				}
				return grid3;
			} else
			{
				return grid1;
			}
		}

		List<List<int>> CalcPlays(List<List<int>> grid, bool hasRef, List<List<int>> existingPlays)
		{
			List<List<int>> newGrid = grid.ConvertAll(row => row.ConvertAll(cell => cell));
			// ignore ref games
			if (hasRef)
			{
				List<List<int>> transposedGrid = TransposeGrid(newGrid);
				transposedGrid.RemoveAt(transposedGrid.Count - 1);
				newGrid = TransposeGrid(transposedGrid);
			}

			int totalTeams = FlattenGrid(newGrid).Uniq().Count;
			List<List<int>> plays = new List<List<int>>();
			for(int team1 = 0; team1 < totalTeams; team1++)
			{
				List<int> row = new List<int>();
				for (int team2 = 0; team2 < totalTeams; team2++)
				{
					if (team1 == team2)
					{
						List<List<int>> games = newGrid.FindAll(g => g.FindAll(t => t == team1).Count > 1);
						row.Add(games.Count);
					}
					else
					{
						List<List<int>> games = newGrid.FindAll(g => g.Contains(team1) && g.Contains(team2));
						row.Add(games.Count);
					}
				}
				plays.Add(row);
			}

			return AddGrids(plays, existingPlays);
		}

		List<T> Shuffle<T>(List<T> list)
		{
			Random random = new Random();
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = random.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
			return list;
		}

		List<List<int>> RandomiseGrid (List<List<int>> grid)
		{
			for(int i = 0; i < grid.Count; i++)
			{
				Random random = new Random();
				grid[i] = Shuffle(grid[i]);
			}
			List<List<int>> transposed = TransposeGrid(grid);
			for (int i = 0; i < grid.Count; i++)
			{
				Random random = new Random();
				grid[i] = Shuffle(grid[i]);
			}
			return TransposeGrid(grid);

		}


		List<List<int>> SetupGrid (FixtureTeams teams, double teamsPerGame, double gamesPerTeam)
		{
			int numberOfTeams = teams.Count();
			double numGames = (numberOfTeams / teamsPerGame) * gamesPerTeam;

			// set up grid
			List<int> arr = Enumerable.Repeat(-1, (int) teamsPerGame * (int)Math.Ceiling(numGames)).ToList();
			int index = 0;
			List<int> updatedArr = new List<int>();
			foreach (int el in arr)
			{
				updatedArr.Add((int)(index % numberOfTeams));
				index++;
			}
			List<List<int>> grid = updatedArr.ChunkBy((int)teamsPerGame);
			for (int i = 0; i < 5000; i++)
			{
				RandomiseGrid(grid);
			}


			return grid;

		}

		public List<List<int>> GetLeagueGrid(League league)
		{
			List<List<int>> grid = new List<List<int>>();
			foreach(Game game in league.AllGames)
			{
				List<int> row = new List<int>();
				foreach(GameTeam team in game.Teams)
				{
					row.Add(team.TeamId - 1 ?? -1);
				}
				grid.Add(row);
			}

			return grid;
		}

		public List<List<int>> PadPlaysToTeamNumber(int numberOfTeams, List<List<int>> plays)
		{
			Console.WriteLine(numberOfTeams);
			List<List<int>> newGrid = plays.ConvertAll(row => row.ConvertAll(cell => cell));
			int teamPlays = newGrid.Count;
			if (numberOfTeams > teamPlays)
			{
				for(int i = 0; i < teamPlays; i++)
				{
					for(int j = teamPlays; j < numberOfTeams; j++)
					{
						newGrid[i].Add(0);
					}
				}
				for(int i = teamPlays; i < numberOfTeams; i++)
				{
					List<int> arr = Enumerable.Repeat(0, numberOfTeams).ToList();
					newGrid.Add(arr);
				}

				return newGrid;
			} else
			{
				return plays;
			}
		}

		int TeamsPerGame()
		{
			return (red.Checked ? 1 : 0) + (blue.Checked ? 1 : 0) + (green.Checked ? 1 : 0) + (yellow.Checked ? 1 : 0) + (referee.Checked ? 1 : 0);
		}

		string TeamColours()
		{
			string colours = "";
			colours += red.Checked ? "1," : "";
			colours += blue.Checked ? "2," : "";
			colours += yellow.Checked ? "4," : "";
			colours += green.Checked ? "3," : "";
			colours += referee.Checked ? "17," : "";
			return colours;
		}

		void ButtonGenerateClick(object sender, EventArgs e)
		{
			buttonGenerate.Text = "Generating...";
			buttonGenerate.Enabled = false;
			try
			{
				if (checkRings.Enabled && numericRings.Value > 1)
					GenerateRingGrid();
				else
					GenerateTeamGrid();
			}
			finally
			{
				buttonGenerate.Text = "Generate";
				buttonGenerate.Enabled = true;
			}
		}

		private void GenerateRingGrid()
		{
			var grid = new RingGrid();
			grid.GenerateRingGrid(Holder.League, Holder.Fixture, fixtureSelectedTeams, (int)numericRings.Value, (int)gamesPerTeamInput.Value, gameDateTime.Value, (int)minBetween.Value, referee.Checked);

			RefreshReports();
		}

		private void GenerateTeamGrid()
		{
			continueGenerating.Enabled = false;
			Holder.Fixture.Teams.Clear();
			Holder.Fixture.Teams.Populate(fixtureSelectedTeams);
			Holder.Fixture.Games.Clear();
			double numberOfTeams = Holder.Fixture.Teams.Count;
			bool hasRef = referee.Checked;
			double teamsPerGame = TeamsPerGame();
			double gamesPerTeam = (double)gamesPerTeamInput.Value + (hasRef ? (double)gamesPerTeamInput.Value / (teamsPerGame - 1) : 0);
			int maxMillis = (int)maxTime.Value * 1000;

			List<List<int>> existingGrid = GetLeagueGrid(Holder.League);
			List<List<int>> existingPlays = CalcPlays(existingGrid, false, new List<List<int>>());
			LogGrid(existingPlays);

			List<List<int>> existingPlaysPadded = PadPlaysToTeamNumber((int)numberOfTeams, existingPlays);
			LogGrid(existingPlaysPadded);

			List<List<int>> grid = GetGrid(Holder.Fixture.Teams, teamsPerGame, gamesPerTeam, hasRef, existingPlaysPadded, maxMillis);

			Holder.Fixture.Games.Parse(grid, Holder.Fixture.Teams, gameDateTime.Value, TimeSpan.FromMinutes((double)minBetween.Value), TeamColours());

			RefreshReports();
		}

		private void RefreshReports()
		{
			textBoxGames.Text = Holder.Fixture.Games.ToString();
			textBoxGrid.Lines = Holder.Fixture.Games.ToGrid(Holder.Fixture.Teams);

			if (outputGrid.Checked && outputList.Checked)
			{
				reportTeamsList.Report = Reports.FixtureCombined(Holder.Fixture, Holder.League);
			}
			else if (outputList.Checked)
			{
				reportTeamsList.Report = Reports.FixtureList(Holder.Fixture, Holder.League);
			}
			else if (outputGrid.Checked)
			{
				reportTeamsList.Report = Reports.FixtureGrid(Holder.Fixture, Holder.League);
			}
		}

		private void ContinueGenerateClick(object sender, EventArgs e)
		{
			Holder.Fixture.Teams.Clear();
			Holder.Fixture.Teams.Populate(fixtureSelectedTeams);
			Holder.Fixture.Games.Clear();
			buttonGenerate.Text = "Generating...";
			buttonGenerate.Enabled = false;
			continueGenerating.Enabled = false;
			int maxMillis = (int)maxTime.Value * 1000;
			List<List<int>> grid = ContinueMixing(previousGrid, previousGamesPerTeam, previousHasRef, previousExistingPlays, maxMillis, previousBestScore);

			Holder.Fixture.Games.Parse(grid, Holder.Fixture.Teams, gameDateTime.Value, TimeSpan.FromMinutes((double)minBetween.Value), TeamColours());

			RefreshReports();

			buttonGenerate.Text = "Generate";
			buttonGenerate.Enabled = true;
		}

		void ButtonImportGamesClick(object sender, EventArgs e)
		{
			Holder.Fixture.Games.Clear();

			bool fromLeague = (ModifierKeys.HasFlag(Keys.Control) && ModifierKeys.HasFlag(Keys.Shift)) ||
				(ModifierKeys.HasFlag(Keys.Control) && ModifierKeys.HasFlag(Keys.Alt)) ||
				(ModifierKeys.HasFlag(Keys.Shift) && ModifierKeys.HasFlag(Keys.Alt));

			if (fromLeague)
				Holder.Fixture.Games.Parse(Holder.League, Holder.Fixture.Teams);
			else
			{
				if (radioButtonTab.Checked)
					Holder.Fixture.Games.Parse(textBoxGames.Text, Holder.Fixture.Teams, '\t');
				else if (textBoxSeparator.Text.Length > 0)
					Holder.Fixture.Games.Parse(textBoxGames.Text, Holder.Fixture.Teams, textBoxSeparator.Text[0]);
			}

			textBoxGames.Text = Holder.Fixture.Games.ToString();
			textBoxGrid.Lines = Holder.Fixture.Games.ToGrid(Holder.Fixture.Teams);
			displayReportGames.Report = Reports.FixtureList(Holder.Fixture, Holder.League);
			displayReportGrid.Report = Reports.FixtureGrid(Holder.Fixture, Holder.League);
		}

		void ButtonImportGridClick(object sender, EventArgs e)
		{
			textBoxGrid.Lines = Holder.Fixture.Games.Parse(textBoxGrid.Lines, Holder.Fixture.Teams, 
			                                        datePicker.Value.Date + timePicker.Value.TimeOfDay, 
			                                        TimeSpan.FromMinutes((double)numericMinutes.Value));
			textBoxGames.Text = Holder.Fixture.Games.ToString();
			displayReportGames.Report = Reports.FixtureList(Holder.Fixture, Holder.League);
			displayReportGrid.Report = Reports.FixtureGrid(Holder.Fixture, Holder.League);
		}

		private void ButtonExportClick(object sender, EventArgs e)
		{
			ExportPages.ExportFixture(ExportFolder, Holder);
			MessageBox.Show("Fixture exported to " + System.IO.Path.Combine(ExportFolder, Holder.Key), "Fixture Exported");
		}

		List<LeagueTeam> Ladder()
		{
			// Find the most appropriate report template showing the date range, drop games, etc.
			var rt = Holder.ReportTemplates.Find(r => r.ReportType == ReportType.TeamLadder) ?? 
				Holder.ReportTemplates.Find(r => r.ReportType == ReportType.MultiLadder) ??
				Holder.ReportTemplates.Find(r => r.ReportType == ReportType.GameByGame) ?? 
				Holder.ReportTemplates.Find(r => r.ReportType == ReportType.GameGrid || r.ReportType == ReportType.GameGridCondensed) ??
				Holder.ReportTemplates.Find(r => r.ReportType == ReportType.Pyramid || r.ReportType == ReportType.PyramidCondensed) ??
				Holder.ReportTemplates.FirstOrDefault() ??
				new ReportTemplate() { ReportType = ReportType.TeamLadder };

			List<Game> games = Holder.League.Games(true).Where(g => g.Time > (rt.From ?? DateTime.MinValue) && g.Time < (rt.To ?? DateTime.MaxValue)).ToList();
			return games != null && games.Any() ? Reports.Ladder(Holder.League, games, rt).Select(tle => tle.Team).ToList() : null;
		}

		void FormFixtureShown(object sender, EventArgs e)
		{
			// Hide tabs for now as they serve no purpose
			tabControl1.TabPages.Remove(tabGamesList);
			tabControl1.TabPages.Remove(tabGamesGrid);
			tabControl1.TabPages.Remove(tabGraphic);

			if (Holder.Fixture != null)
			{
				Holder.League.Load(Holder.League.FileName);
				List<LeagueTeam> leagueTeams = Holder.League.GetTeamLadder();
				if (Holder.Fixture.Teams.Count == 0)
				{
					Holder.Fixture.Teams.Populate(leagueTeams);
				}
				else
				{
					var comparer = new TeamComparer() { LeagueTeams = leagueTeams };
					Holder.Fixture.Teams.Sort(comparer);
				}

				if (Holder.Fixture.Games.Any())
				{
					displayReportGames.Report = Reports.FixtureList(Holder.Fixture, Holder.League);
					displayReportGrid.Report = Reports.FixtureGrid(Holder.Fixture, Holder.League);
				}
				else
				{
					displayReportGames.Report = null;
					displayReportGrid.Report = null;
				}

				if (Holder.Fixture.Games.Any())
				{
					textBoxGames.Text = Holder.Fixture.Games.ToString();
					textBoxGrid.Lines = Holder.Fixture.Games.ToGrid(Holder.Fixture.Teams);
				}

				loading = true;
				Console.WriteLine(leagueTeams.Count);
				SetTeamsBox(leagueTeams.Count);

				for (int i = 0; i < leagueTeams?.Count; i++)
				{
					teamSelectors[i].Text = leagueTeams[i].Name;
					teamSelectors[i].Checked = true;
					fixtureTeamSelectors[i].Text = leagueTeams[i].Name;
					fixtureTeamSelectors[i].Checked = true;
					teamSelectors[i].Visible = true;
					fixtureTeamSelectors[i].Visible = true;
				}

				for (int i = leagueTeams?.Count ?? 0; i < teamSelectors?.Count; i++)
				{
					teamSelectors[i].Checked = false;
					teamSelectors[i].Visible = false;
					fixtureTeamSelectors[i].Checked = false;
					fixtureTeamSelectors[i].Visible = false;
				}

				loading = false;
				TeamCheckedChanged(null, null);
			}
		}

		void SetTeamsBox(int i)
		{
			while (teamSelectors.Count < i)
			{
				var teamBox = new CheckBox
				{
					Left = 9,
					Top = 10 + teamSelectors.Count * 26,
					Width = teamsList.Width - 28,  // Leave room for a vertical scrollbar, should the owning panel decide it needs one.
					Parent = teamsList
				};

				teamBox.CheckedChanged += TeamCheckedChanged;
				teamSelectors.Add(teamBox);
			}
			while (fixtureTeamSelectors.Count < i)
			{
				var teamBox = new CheckBox
				{
					Left = 9,
					Top = 10 + fixtureTeamSelectors.Count * 26,
					Width = fixtureTeamsList.Width - 28,
					Parent = fixtureTeamsList
				};

				teamBox.CheckedChanged += FixtureTeamCheckedChanged;
				fixtureTeamSelectors.Add(teamBox);
			}
		}

		private void TeamCheckedChanged(object sender, EventArgs e)
		{
			if (loading)
			{
				return;
			}

			selectedTeams = Holder.League.GetTeamLadder().FindAll(t =>
			{
				return teamSelectors.Find(teamSelector =>
				{
					return teamSelector.Checked && teamSelector.Text == t.Name;

				}) != null;
			});

			frameFinals1.Teams = selectedTeams;
		}

		private void OutputCheckChanged(object sender, EventArgs e)
		{
			RefreshReports();
		}

		private void CheckRingsCheckedChanged(object sender, EventArgs e)
		{
			numericRings.Enabled = checkRings.Checked;

			red.Enabled = !checkRings.Checked;
			green.Enabled = !checkRings.Checked;
			blue.Enabled = !checkRings.Checked;
			yellow.Enabled = !checkRings.Checked;
		}

		private void FixtureTeamCheckedChanged(object sender, EventArgs e)
		{
			fixtureSelectedTeams = Holder.League.GetTeamLadder().FindAll(t =>
			{
				return fixtureTeamSelectors.Find(fixtureTeamSelector =>
				{
					return fixtureTeamSelector.Checked && fixtureTeamSelector.Text == t.Name;

				}) != null;
			});
		}

		private void PrintReportSaveHtmlTable(object sender, EventArgs e)
		{
			ExportPages.ExportFixtureToFile(printReport1.FileName, holder);
		}

		void TextBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.A &&sender != null)
				((TextBox)sender).SelectAll();
		}

		// TODO: turn panelGraphic into a custom control that takes a Holder.Fixture as a property, so it can manage its own painting, clicks, etc.
		void FillCell(int row, int col, int size, Color color)
		{
			var g = panelGraphic.CreateGraphics();
			g.FillRectangle(new SolidBrush(color), new Rectangle(col * size + 1, row * size + 1, size - 1, size - 1));
		}

		void PaintNumbers(int size, int rows)
		{
			var difficulties = new float[rows];
			var counts = new int[rows];
			var averages = new float[rows];
			var games = Holder.Fixture.Games;

			foreach (var fg in games)
				foreach (var ft in fg.Teams)
					if (0 < ft.Key.TeamId && ft.Key.TeamId < difficulties.Length)
					{
						difficulties[ft.Key.TeamId - 1] += (fg.Teams.Sum(x => x.Key.TeamId) - ft.Key.TeamId) / (fg.Teams.Count - 1F);
						counts[ft.Key.TeamId - 1]++;
					}
			
			for (int row = 0; row < rows; row++)
				if (counts[row] > 0)
					averages[row] = difficulties[row] / counts[row];
				else
					averages[row] = float.NaN;
			
			float max = averages.Count() == 0 ? 1 : averages.Max();

			var g = panelGraphic.CreateGraphics();
			var font = new Font("Arial", size - 2);
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
			Pen pen = new Pen(Color.Black);

			g.FillRectangle(new SolidBrush(Color.White), games.Count * size + 1, 0, games.Count * size, rows * size);  // Erase right-hand side text etc.

			// Draw difficulty text and chart.
			for (int row = 0; row < rows; row++)
				if (!float.IsNaN(averages[row]))
				{
					g.DrawString(counts[row].ToString() + "  " + averages[row].ToString("N2"), font, Brushes.Black, games.Count * size, row * size - 2);
					float x = games.Count * size + 50 + averages[row] / max * 100;
					g.DrawLine(pen, x, row * size, x, row * size + size - 1);
				}

			g.FillRectangle(new SolidBrush(Color.White), 0, rows * size + 1, games.Count * size, size);  // Erase bottom text.

			for (int col = 0; col < games.Count; col++)
				g.DrawString(games[col].Teams.Count.ToString(), font, Brushes.Black, col * size - 1, rows * size);
		}

		void PaintWhoPlaysWho(int size)
		{
			int left = Holder.Fixture.Games.Count * size + 150;

			var g = panelGraphic.CreateGraphics();
			var font = new Font("Arial", size - 2);
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
			Pen pen = new Pen(Color.Black);

			// Paint diagonal.
			for (int x = 0; x < Holder.Fixture.Teams.Count; x++)
				g.FillRectangle(new SolidBrush(Color.FromArgb(0xF8, 0xF8, 0xF8)), left + x * size, x * size, size, size);

				// Colour squares for selected team and game.
				if (Holder.Fixture.Games.Valid(point.X))
				{
				var game = Holder.Fixture.Games[point.X];
				var team = Holder.Fixture.Teams[point.Y];

				// Highlight cells for selected team.
				for (int x = 0; x < Holder.Fixture.Teams.Count; x++)
					if (x != point.Y && Holder.Fixture.Games.Count(fg => fg.Teams.ContainsKey(Holder.Fixture.Teams[x]) && fg.Teams.ContainsKey(team)) != 0)
					{
						g.FillRectangle(new SolidBrush(Color.FromArgb(0xE0, 0xE0, 0xE0)), left + x * size, point.Y * size, size, size);
						g.FillRectangle(new SolidBrush(Color.FromArgb(0xE0, 0xE0, 0xE0)), left + point.Y * size, x * size, size, size);
					}

				// Colour cells that represent teams that the selected team plays against in this game.
				foreach (var kv in game.Teams)
				{
					g.FillRectangle(new SolidBrush(kv.Value.ToColor()), left + (kv.Key.TeamId - 1) * size, (team.TeamId - 1) * size, size, size);
					g.FillRectangle(new SolidBrush(kv.Value.ToColor()), left + (team.TeamId - 1) * size, (kv.Key.TeamId - 1) * size, size, size);
				}
			}

			// Put numbers in squares.
			for (int x = 0; x < Holder.Fixture.Teams.Count; x++)
				for (int y = 0; y < Holder.Fixture.Teams.Count; y++)
					if (x != y)
					{
						int sum = Holder.Fixture.Games.Count(fg => fg.Teams.ContainsKey(Holder.Fixture.Teams[x]) && fg.Teams.ContainsKey(Holder.Fixture.Teams[y]));
						if (sum != 0)
							g.DrawString(sum.ToString(), font, Brushes.Black, left + x * size, y * size - 2);
					}
		}

		void PaintBorder(int size, int rows)
		{
			var g = panelGraphic.CreateGraphics();
			Pen pen = new Pen(Color.Gray);

			g.DrawLine(pen, 0, 0, Math.Min(Holder.Fixture.Games.Count * size, panelGraphic.DisplayRectangle.Right), 0);
			g.DrawLine(pen, 0, rows * size, Math.Min(Holder.Fixture.Games.Count * size, panelGraphic.DisplayRectangle.Right), rows * size);

			g.DrawLine(pen, 0, 0, 0, Math.Min(rows * size, panelGraphic.DisplayRectangle.Bottom));
			g.DrawLine(pen, Holder.Fixture.Games.Count * size, 0, Holder.Fixture.Games.Count * size, Math.Min(rows * size, panelGraphic.DisplayRectangle.Bottom));
		}

		void PaintGrid(int size, int rows)
		{
			var g = panelGraphic.CreateGraphics();
			Pen pen = new Pen(Color.Gray);

			for (int row = 0; row <= rows; row++)
				g.DrawLine(pen, 0, row * size, Math.Min(Holder.Fixture.Games.Count * size, panelGraphic.DisplayRectangle.Right), row * size);

			for (int col = 0; col <= Holder.Fixture.Games.Count; col++)
				g.DrawLine(pen, col * size, 0, col * size, Math.Min(rows * size, panelGraphic.DisplayRectangle.Bottom));

//			// Paint a light gray bar on the row and column of the clicked team. Not in use because we'd have to rebuild grid every PanelGraphicMouseClick.
//			for (int x = 0; x < Holder.Fixture.Games.Count; x++)
//				for (int y = 0; y < Holder.Fixture.Teams.Count; y++)
//					if (point != null && (point.Y == x || point.Y == y))
//						FillCell(x, y, size, Color.LightGray);
		}

		// Paint coloured cells onto grid to show teams in games.
		void PaintCells(int size)
		{
			for (int col = 0; col < Holder.Fixture.Games.Count; col++)
			{
				var fg = Holder.Fixture.Games[col];
				foreach (var x in fg.Teams)
				{
					var row = Holder.Fixture.Teams.IndexOf(x.Key);
					if (row != -1)
					{
						if (x.Value == Colour.None)
							FillCell(row, col, size, Color.Black);
						else
							FillCell(row, col, size, x.Value.ToSaturatedColor());
					}
				}
			}
		}

		void PanelGraphicPaint(object sender, PaintEventArgs e)
		{
			int size = (int)numericSize.Value;
			int rows = Holder.Fixture.Teams.Count;

			if (resizing)
			{
				PaintBorder(size, rows);
				return;
			}

			PaintGrid(size, rows);
			PaintCells(size);
			PaintNumbers(size, rows);
			PaintWhoPlaysWho(size);

			// Paint palette.
			for (Colour i = Colour.None; i < Colour.White; i++)
				FillCell(rows + 2, (int)i, size, i.ToSaturatedColor());
		}

		void NumericSizeValueChanged(object sender, EventArgs e)
		{
			panelGraphic.Invalidate();
		}

		void PanelGraphicMouseClick(object sender, MouseEventArgs e)
		{

			int size = (int)numericSize.Value;
			point = panelGraphic.PointToClient(Cursor.Position);
			point = new Point(point.X / size, point.Y / size);
			int rows = Holder.Fixture.Teams.Count;

			if (point.X < Holder.Fixture.Games.Count && point.Y < rows)
			{
				if (Holder.Fixture.Games[point.X].Teams.ContainsKey(Holder.Fixture.Teams[point.Y]))
				{
					Holder.Fixture.Games[point.X].Teams.Remove(Holder.Fixture.Teams[point.Y]);
					FillCell(point.Y, point.X, size, Color.White);
				}
				else
				{
					Colour c = Colour.None;
					switch (e.Button) {
						case MouseButtons.Left: c = leftButton;	    break;
						case MouseButtons.Right: c = rightButton;	break;
						case MouseButtons.Middle: c = middleButton;	break;
						case MouseButtons.XButton1: c = xButton1;	break;
						case MouseButtons.XButton2: c = xButton2;	break;
					}
					Holder.Fixture.Games[point.X].Teams.Add(Holder.Fixture.Teams[point.Y], c);
					FillCell(point.Y, point.X, size, c.ToSaturatedColor());
				}

				PaintNumbers(size, rows);
				PaintWhoPlaysWho(size);
			}

			else
			{
				if (point.Y == rows + 2 && point.X > 0 && point.X < 9)
				{
					var c = (Colour)(point.X);
					switch (e.Button) {
						case MouseButtons.Left: leftButton = c;	    break;
						case MouseButtons.Right: rightButton = c;	break;
						case MouseButtons.Middle: middleButton = c;	break;
						case MouseButtons.XButton1: xButton1 = c;	break;
						case MouseButtons.XButton2: xButton2 = c;	break;
					}
				}

				point = new Point(-1, -1);
			}
		}

		void FormFixtureResizeBegin(object sender, EventArgs e)
		{
			resizing = true;
		}

		void FormFixtureResizeEnd(object sender, EventArgs e)
		{
			resizing = false;
			panelGraphic.Invalidate();
		}
	}
}

class TeamComparer : IComparer<LeagueTeam>
{
	/// <summary>Sorted list of league teams.</summary>
	public List<LeagueTeam> LeagueTeams { get; set; }

	public int Compare(LeagueTeam x, LeagueTeam y)
	{
		if (LeagueTeams == null)
			return 0;

		int ix = LeagueTeams.FindIndex(lt => lt == x);
		int iy = LeagueTeams.FindIndex(lt => lt == y);
		if (ix == -1) ix = 999999;
		if (iy == -1) iy = 999999;
		return ix - iy;
	}
}

public static class ListExtensions
{
	public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
	{
		return source
			.Select((x, i) => new { Index = i, Value = x })
			.GroupBy(x => x.Index / chunkSize)
			.Select(x => x.Select(v => v.Value).ToList())
			.ToList();
	}
	public static List<T> Uniq<T>(this List<T> source)
	{
		List<T> result = new List<T>();

		foreach(T el in source)
		{
			bool exists = result.Contains(el);
			if (!exists)
				result.Add(el);
		}
		return result;

	}
}