using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Torn;
using Torn.Grids;
using Torn.Report;
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
					frameFinals1.Games = holder.League.Games();
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

		private readonly List<CheckBox> teamSelectors = new List<CheckBox>();
		private readonly List<CheckBox> fixtureTeamSelectors = new List<CheckBox>();
		private List<LeagueTeam> selectedTeams = new List<LeagueTeam>();
		private List<LeagueTeam> fixtureSelectedTeams = new List<LeagueTeam>();
		private GridFinder gridFinder = new GridFinder();

		Colour leftButton, middleButton, rightButton, xButton1, xButton2;
		Point point;  // This is the point in the grid last clicked on. It's counted in grid squares, not in pixels: 9,9 is ninth column, ninth row.
		bool resizing;
		bool loading;
		decimal gamesCount = 1;

		public FormFixture()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();

			datePicker.Value = DateTime.Now.Date;
			timePicker.CustomFormat = CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern;

			leftButton = Colour.Red;
			middleButton = Colour.Blue;
			rightButton = Colour.Green;
			xButton1 = Colour.Yellow;
			xButton2 = Colour.Purple;
			point = new Point(-1, -1);
			resizing = false;
		}

		void ButtonClearGamesGridClick(object sender, EventArgs e)
		{
			Holder.Fixture.Games.Clear();
			displayReportGames.Report = null;
			displayReportGrid.Report = null;
		}

		private void ButtonClearClick(object sender, EventArgs e)
		{
			gridFinder = new GridFinder();
			Holder.Fixture.Games.Clear();
			reportTeamsList.Report = null;
		}

		void ButtonGenerateClick(object sender, EventArgs e)
		{
			SetBusy(true);
			try
			{
				var teams = fixtureSelectedTeams;

				if (fixtureSelectedTeams.Count != numericTeams.Value)
				{
					int maxId = Holder.League.Teams().Any() ? Holder.League.Teams().Max(t => t.TeamId) : 0;
					teams = fixtureSelectedTeams.ToList();

					if (teams.Count > numericTeams.Value)
						teams.RemoveRange((int)numericTeams.Value, teams.Count - (int)numericTeams.Value);

					while (teams.Count < numericTeams.Value)
					{
						maxId++;
						teams.Add(new LeagueTeam() { TeamId = maxId, Name = "Team " + maxId.ToString() });
					}
				}

				PopulateColours();
				PopulateScoreScalers();
				gridFinder.ShuffleType = GetShuffleType();
				fixtureSessions.Populate(gridFinder.Sessions);
				gridFinder.Rings = checkRings.Checked;

				if (checkRings.Checked)
					GenerateRingGrid(teams);
				else
					GenerateTeamGrid(teams);
			}
			finally
			{
				SetBusy(false);
			}
		}

		private void GenerateRingGrid(List<LeagueTeam> teams)
		{
			if (reportTeamsList.Report == null)  // Blank slate.
			{
				// Generate a starting-point fixture.
				var grid = new RingGrid();
				textBoxScore.Text = grid.GenerateRingGrid(holder.League, holder.Fixture, teams, (int)numericTeamsPerGame.Value, (int)numericGamesPerTeam.Value, gridFinder.Sessions.First().Start, (int)gridFinder.Sessions.First().Between.TotalMinutes, numericReferees.Value > 0);

				if (holder.Fixture.Games.Any())  // If that generate succeeded,
				{
					RefreshReports();
					gridFinder.SetupFromFixture(holder.Fixture);  // set us up to improve it.
				}
			}

			if (holder.Fixture.Games.Any())
				Improve();
		}

		private void GenerateTeamGrid(List<LeagueTeam> teams)
		{
			gridFinder.Setup(Holder.League, Holder.Fixture, teams, (int)numericGamesPerTeam.Value, (int)numericReferees.Value);

			Improve();
		}

		private bool Improve()
		{
			RefreshReports();
			textBoxScore.Text = gridFinder.Details;

			while (run && gridFinder.ShuffleType != ShuffleType.None)
			{
				gridFinder.ImproveParallel(Holder.Fixture);
				if (gridFinder.Changed)
				{
					gridFinder.Changed = false;
					RefreshReports();
				}
				textBoxScore.Text = gridFinder.Details;
				Application.DoEvents();
			}

			return gridFinder.Changed;
		}

		private void PopulateColours()
		{
			int ringMultiplier = checkRings.Checked ? 3 : 1;

			gridFinder.Colours.Clear();
			foreach (var checkBox in tabFixtureSettings.Controls.OfType<CheckBox>())
				if (checkBox.Tag is Colour colour && checkBox.Checked)
					for (int i = 0; i < ringMultiplier; i++)
						gridFinder.Colours.Add(colour);

			// Add more entries to the Colours list if necessary (because the user has ticked some boxes and then manually turned up the Teams Per Game value).
			if (checkRings.Checked)
			{
				// Make a list of colours not yet used, with hard-to-distinguish colours last.
				var bestColours = ColourExtensions.BestFirst().Where(c => !gridFinder.Colours.Contains(c)).ToList();

				// Add colours until we've got enough.
				while (gridFinder.Colours.Count < numericTeamsPerGame.Value * ringMultiplier)
				{
					var colourToAdd = bestColours.FirstOrDefault();
					for (int i = 0; i < ringMultiplier; i++)
						gridFinder.Colours.Add(colourToAdd);
					bestColours.Remove(colourToAdd);
				}
			}
			else
			{
				// If there was only one colour ticked, add more of that colour. Otherwise, just add Colour.None.
				Colour colourToAdd = gridFinder.Colours.Count == 1 ? gridFinder.Colours.First() : Colour.None;
				while (gridFinder.Colours.Count < numericTeamsPerGame.Value)
						gridFinder.Colours.Add(colourToAdd);
			}

			// Add entries for referees, if any.
			if (checkRings.Checked && numericReferees.Value > 0)
				for (int i = 0; i < numericTeamsPerGame.Value * ringMultiplier; i++)
					gridFinder.Colours.Add(Colour.Referee);
			else
				for (int i = 0; i < numericReferees.Value; i++)
					gridFinder.Colours.Add(Colour.Referee);
		}

		private void PopulateScoreScalers()
		{
			var ss = gridFinder.ScoreScalers;

			ss.PlaySelf = (int)numericPlaySelf.Value;
			ss.TimesPlayed = (int)numericTimesPlayed.Value;
			ss.PastTimesPlayed = checkPastTimesPlayed.Checked;
			ss.DayLength = (int)numericDayLength.Value;
			ss.NightLength = (int)numericNightLength.Value;
			ss.NoGamesInSession = (int)numericNoGamesInSession.Value;
			ss.OneGameInSession = (int)numericOneGameInSession.Value;
			ss.TeamsOnSite = (int)numericTeamsOnSite.Value;
			ss.SameDifficulty = (int)numericSameDifficulty.Value;
			ss.CascadeDifficulty = (int)numericCascadeDifficulty.Value;
			ss.PerfectColour = (int)numericPerfectColour.Value;
			ss.BackToBack = (int)numericBackToBack.Value;
			ss.RefThenPlay = (int)numericRefThenPlay.Value;
			ss.PlayThenRef = (int)numericPlayThenRef.Value;
			ss.RefereeClustering = (int)numericClusterRefereeing.Value;
		}

		private ShuffleType GetShuffleType()
		{
			ShuffleType st = ShuffleType.None;

			if (checkRings.Checked)
			{
				if (checkShuffleGames.Checked) st |= ShuffleType.Rings;
			}
			else
			{
				if (checkShuffleTeams.Checked) st |= ShuffleType.BetweenGames;
				if (checkShuffleGames.Checked) st |= ShuffleType.Games;
				if (checkShuffleColours.Checked) st |= ShuffleType.WithinGames;
			}

			if (checkShuffleReferees.Checked) st |= ShuffleType.Referees;

			return st;
		}

		private void RefreshReports()
		{
			textBoxGames.Text = Holder.Fixture.Games.ToString();
			textBoxGrid.Lines = Holder.Fixture.Games.ToGrid(Holder.Fixture.Teams);

			if (checkGameGrid.Checked && checkGameList.Checked)
				reportTeamsList.Report = Reports.FixtureCombined(Holder.Fixture, Holder.League);
			else if (checkGameList.Checked)
				reportTeamsList.Report = Reports.FixtureList(Holder.Fixture, Holder.League);
			else if (checkGameGrid.Checked)
				reportTeamsList.Report = Reports.FixtureGrid(Holder.Fixture, Holder.League);
		}

		bool run;
		private void ButtonStopClick(object sender, EventArgs e)
		{
			buttonStop.Text = "&Stopping...";
			run = false;
		}

		private void FormFixtureFormClosing(object sender, FormClosingEventArgs e)
		{
			run = false;
		}

		private void SetBusy(bool busy)
		{
			buttonGenerate.Text = busy ? "Generating..." : "&Generate";

			if (!busy)
				buttonStop.Text = "&Stop";

			valuesChanging = true;
			buttonGenerate.Enabled = !busy;
			buttonStop.Enabled = busy;
			buttonClear.Enabled = !busy && Holder.Fixture.Games.Any();  // Dear Visual Studio: why does clearing buttonClear.Enabled cause a call to RadioPresetClick()?
			valuesChanging = false;

			foreach (Control control in groupBoxPresets.Controls)
				control.Enabled = !busy;

			run = busy;
			UseWaitCursor = busy;
			Application.DoEvents();
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

			List<Game> games = Holder.League.Games().Where(g => g.Time > (rt.From ?? DateTime.MinValue) && g.Time < (rt.To ?? DateTime.MaxValue)).ToList();
			return games.Any() ? Reports.Ladder(Holder.League, games, rt).Select(tle => tle.Team).ToList() : null;
		}

		void FormFixtureShown(object sender, EventArgs e)
		{
			// Hide tabs for now as they serve no purpose
			tabControl1.TabPages.Remove(tabGamesList);
			tabControl1.TabPages.Remove(tabGamesGrid);
			tabControl1.TabPages.Remove(tabGraphic);

			int screenHeight = Screen.GetWorkingArea(this).Height;
			Height = (int)Math.Min(Math.Max(Height, screenHeight * 0.8), screenHeight * 0.99);
			framePyramidRound1.SetSplit();
			splitContainerTeams.SplitterDistance = groupBoxPresets.Right + 4;

			if (Holder.Fixture != null)
			{
				Holder.League.Load();
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
				Console.WriteLine("Teams count: " + leagueTeams.Count);
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

				numericTeams.Value = Math.Max(leagueTeams.Count, 2);

				var coloursUsed = Holder.League.Games().SelectMany(g => g.Teams.Select(t => t.Colour)).Distinct().ToList();
				if (coloursUsed.Any())
				{
					foreach (var control in tabFixtureSettings.Controls)
						if (control is CheckBox cb && cb.Tag is Colour colour)
							cb.Checked = coloursUsed.Contains(colour);

					numericTeamsPerGame.Value = Math.Max(coloursUsed.Count, 2);
				}

				loading = false;
				TeamCheckedChanged(null, null);
				FixtureTeamCheckedChanged(null, null);
			}
		}

		/// <summary>Create checkboxes as needed to represent teams, on both the Teams and Finals tabs.</summary>
		void SetTeamsBox(int i)
		{
			teamsList.SuspendLayout();
			fixtureTeamsList.SuspendLayout();
			try
			{
				while (teamSelectors.Count < i)
					teamSelectors.Add(MakeTeamBox(teamsList));

				while (fixtureTeamSelectors.Count < i)
					fixtureTeamSelectors.Add(MakeTeamBox(fixtureTeamsList));
			}
			finally
			{
				fixtureTeamsList.ResumeLayout();
				teamsList.ResumeLayout();
			}
		}

		private CheckBox MakeTeamBox(Panel parent)
		{
			var teamBox = new CheckBox
			{
				Dock = DockStyle.Top,
				Height = buttonGenerate.Height,
				Parent = parent
			};

			teamBox.CheckedChanged += TeamCheckedChanged;
			return teamBox;
		}

		private void TeamCheckedChanged(object sender, EventArgs e)
		{
			if (loading)
				return;

			selectedTeams = Holder.League.GetTeamLadder().FindAll(t =>
			{
				return teamSelectors.Find(teamSelector =>
				{
					return teamSelector.Checked && teamSelector.Text == t.Name;

				}) != null;
			});

			frameFinals1.Teams = selectedTeams;
		}

		private void FixtureTeamCheckedChanged(object sender, EventArgs e)
		{
			if (loading)
				return;

			fixtureSelectedTeams = Holder.League.GetTeamLadder().FindAll(t =>
			{
				return fixtureTeamSelectors.Find(fixtureTeamSelector =>
				{
					return fixtureTeamSelector.Checked && fixtureTeamSelector.Text == t.Name;

				}) != null;
			});

			numericTeams.Value = Math.Max(fixtureSelectedTeams.Count, 2);
		}

		bool valuesChanging = false;

		/// <summary>Set settings and tuning values to appropriate values for various scenarios.</summary>
		private void RadioPresetClick(object sender, EventArgs e)
		{
			if (valuesChanging)
				return;

			bool defaults = sender == radioDefaults;
			bool league = sender == radioLeague;
			bool sides = sender == radioSides;
			bool cascade = sender == radioCascade;
			bool roundRobin = sender == radioRoundRobin;
			bool referees = sender == radioAddReferees;
			bool lotr = sender == radioLordOfTheRing;

			bool triples = sides && numericTeams.Value < 50;
			bool doubles = sides && 50 <= numericTeams.Value && numericTeams.Value < 90;
			bool solos = sides && 90 <= numericTeams.Value;

			if (defaults)
			{
				numericPlaySelf.Value = 1000;
				numericTimesPlayed.Value = 50;
				numericBackToBack.Value = 250;
				numericRefThenPlay.Value = 125;
				numericPlayThenRef.Value = 100;
				numericClusterRefereeing.Value = 1;
			}

			if (!referees)
			{
				checkPastTimesPlayed.Checked = league;
				numericDayLength.Value = lotr ? 90 : defaults || cascade || roundRobin ? 30 : 0;
				numericNightLength.Value = defaults || cascade || roundRobin || lotr ? 3 : 0;
				numericNoGamesInSession.Value = defaults || cascade || roundRobin || lotr ? -1 : 0;
				numericOneGameInSession.Value = defaults || cascade || roundRobin || lotr ? 2 : 0;
				numericTeamsOnSite.Value = league ? 0 : 1;
				numericSameDifficulty.Value = defaults || sides || roundRobin ? 1 : 0;
				numericCascadeDifficulty.Value = cascade ? 10 : 0;
				numericPerfectColour.Value = sides || lotr ? 0 : -1;
				numericClusterRefereeing.Value = lotr ? 0 : 1;

				if (lotr)
					numericRefThenPlay.Value = -1;

				if (sides)
				{
					foreach (var control in tabFixtureSettings.Controls)
						if (control is CheckBox cb && cb.Tag is Colour colour)
							cb.Checked = triples ? (int)colour <= 6 : doubles;  // 6 teams per game for triples. All colours true for doubles; all false for solos.

					if (solos)
						numericTeamsPerGame.Value = 20;
				}
				else if (!lotr)
					numericTeamsPerGame.Value = 3;

				numericGamesPerTeam.Value = roundRobin ? (int)(numericTeams.Value / 2 - 1) : cascade || lotr ? 6 : league ? 3 : 2;
			}

			numericReferees.Value = referees ? 2 : league || lotr ? 1 : 0;  // I want to set cascade || roundRobin to have 2 referees per game, but I can't get there in one hop: we have to generate the fixture without referees, then have the user hit Stop, add referees, and hit Generate again.

			checkShuffleTeams.Checked = league | roundRobin | sides;
			checkShuffleGames.Checked = cascade;
			checkShuffleColours.Checked = cascade;
			checkShuffleReferees.Checked = !lotr && numericReferees.Value > 0;

			checkRings.Checked = lotr;  // Do this last or last-ish, as it will fire CheckRingsCheckedChanged().
		}

		private void FixturesValueChanged(object sender, EventArgs e)
		{
			valuesChanging = true;

			if (numericPlaySelf.Value != 1000 || numericTimesPlayed.Value != 50 || numericBackToBack.Value != 250 || checkRings.Checked)
				radioDefaults.Checked = false;

			if (!checkPastTimesPlayed.Checked || numericTeamsOnSite.Value != 0 || numericGamesPerTeam.Value != 3 || numericReferees.Value != 1 || !checkShuffleTeams.Checked || checkRings.Checked)
				radioLeague.Checked = false;

			if (numericSameDifficulty.Value != 1 || numericPerfectColour.Value != 0 || checkShuffleTeams.Checked || checkRings.Checked)
				radioSides.Checked = false;

			if (numericDayLength.Value != 30 || numericNightLength.Value != 3 || numericNoGamesInSession.Value != -1 || numericOneGameInSession.Value != 2 || numericTeamsOnSite.Value != 0 || checkRings.Checked)
			{
				radioCascade.Checked = false;
				radioRoundRobin.Checked = false;
			}

			if (numericCascadeDifficulty.Value != 10 || numericGamesPerTeam.Value != 6 || !checkShuffleGames.Checked || !checkShuffleColours.Checked)
				radioCascade.Checked = false;

			if (numericSameDifficulty.Value != 1 || !checkShuffleTeams.Checked)
				radioRoundRobin.Checked = false;

			if (numericReferees.Value == 0)
				radioAddReferees.Checked = false;

			valuesChanging = false;
		}

		private void TeamCountValueChanged(object sender, EventArgs e)
		{
			gamesCount = numericTeams.Value * numericGamesPerTeam.Value / numericTeamsPerGame.Value;
			if (checkRings.Checked)
				gamesCount /= 3;

			labelGameCount.Text = gamesCount == Math.Round(gamesCount) ? gamesCount.ToString() : gamesCount.ToString("N2");
			fixtureSessions.Games = (int)gamesCount;
		}

		private void ColourCheckedChanged(object sender, EventArgs e)
		{
			if (loading || valuesChanging)
				return;

			int count = 0;
			foreach (var control in tabFixtureSettings.Controls)
				if (control is CheckBox cb && cb.Tag is Colour colour && cb.Checked)
					count++;

			if (count >= 2)
				numericTeamsPerGame.Value = count;
		}

		private void NumericRefereesValueChanged(object sender, EventArgs e)
		{
			checkShuffleReferees.Enabled = numericReferees.Value > 0;
		}

		private void CheckRingsCheckedChanged(object sender, EventArgs e)
		{
			bool c = checkRings.Checked;

			labelTeamsPerGame.Text = c ? "Rings" : "Teams per game";

			checkShuffleTeams.Enabled = !c;
			checkShuffleColours.Enabled = !c;

			if (c)
			{
				numericGamesPerTeam.Value = 6;
				checkShuffleTeams.Checked = false;
				checkShuffleColours.Checked = false;
				numericRefThenPlay.Value = -10;

				if (numericTeams.Value < 18)
					numericTeams.Value = 18;
			}

			TeamCountValueChanged(sender, e);
		}

		private void CheckShuffleChanged(object sender, EventArgs e)
		{
			bool c = ((CheckBox)sender).Checked;

			if (sender == checkShuffleTeams && c)
			{
				checkShuffleGames.Checked = false;
				checkShuffleColours.Checked = false;
			}
			else if ((sender == checkShuffleGames || sender == checkShuffleColours) && c)
				checkShuffleTeams.Checked = false;
		}

		private void PrintReportSaveHtmlTable(object sender, EventArgs e)
		{
			ExportPages.ExportFixtureToFile(printReport1.FileName, holder);
		}

		private void OutputCheckChanged(object sender, EventArgs e)
		{
			RefreshReports();
		}

		private void PanelRightResize(object sender, EventArgs e)
		{
			checkGameList.Top = panelRight.Height - checkGameList.Height;
			checkGameGrid.Top = checkGameList.Top - checkGameGrid.Height;
		}

		/// <summary>When user presses Ctrl-A in this control, Select All.</summary>
		void TextBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.A &&sender != null)
				((TextBox)sender).SelectAll();
		}

		#region Graphic
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

		/// <summary>Paint coloured cells onto grid to show teams in games.</summary>
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
		#endregion
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
