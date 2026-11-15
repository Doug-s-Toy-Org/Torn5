using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Torn;
using Torn.Grids;
using Torn.Report;
using Torn5.Forms;
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

		public event EventHandler<ReportEventArgs> SendToScoreboard
		{
			add
			{
				printReport1.SendToScoreboard += value;
				frameFinals1.SendToScoreboard += value;
				framePyramid1.SendToScoreboard += value;
				framePyramidRound1.SendToScoreboard += value;
			}
			remove
			{
				printReport1.SendToScoreboard -= value;
				frameFinals1.SendToScoreboard -= value;
				framePyramid1.SendToScoreboard -= value;
				framePyramidRound1.SendToScoreboard -= value;
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

		private bool loading;
		private decimal gamesCount = 1;

		public FormFixture()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
		}

		private void ButtonClearClick(object sender, EventArgs e)
		{
			gridFinder = new GridFinder();
			Holder.Fixture.Games.Clear();
			reportTeamsList.Report = null;
			textBoxScore.Text = "";
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
				fixtureSessions.PopulateIntoSessions(gridFinder.Sessions);
				gridFinder.Rings = checkRings.Checked ? (int)numericRings.Value : 1;

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
			bool didSomething = false;
			if (reportTeamsList.Report == null)  // Blank slate.
			{
				// Generate a starting-point fixture.
				textBoxScore.Text = new RingGrid().GenerateRingGrid(holder.Fixture, teams, (int)numericRings.Value, (int)numericTeamsPerRing.Value, (int)numericGamesPerTeam.Value, gridFinder.Sessions.First().Start, (int)gridFinder.Sessions.Between.TotalMinutes, checkRefsRings.Checked);
				didSomething = true;
				if (holder.Fixture.Games.Any())  // If that generate succeeded,
					SetFixtureTitle(holder.Fixture, numericGamesPerTeam.Value == 4 && teams.Count == 9, numericGamesPerTeam.Value == 4 && teams.Count == 36);
			}
			else if (checkRefsRings.Checked && holder.Fixture.Games.Sum(g => g.Teams.Count(t => t.Value == Colour.Referee)) < holder.Fixture.Games.Sum(g => g.Teams.Count(t => t.Value != Colour.Referee)))
			{
				new RingGrid().AddReferees(holder.Fixture, teams, 0);
				didSomething = true;
			}

			if (didSomething)
			{
				gridFinder.SetupFromFixture(holder.Fixture);  // set us up to improve it.
				RefreshReports();
				textBoxScore.Text += gridFinder.Details;
			}

			if (holder.Fixture.Games.Any())
				Improve(CountModifierKeys() >= 2);
		}

		private void GenerateTeamGrid(List<LeagueTeam> teams)
		{
			gridFinder.Setup(Holder.League, Holder.Fixture, teams, (int)numericGamesPerTeam.Value, (int)numericReferees.Value);
			RefreshReports();
			textBoxScore.Text = gridFinder.Details;

			Improve(CountModifierKeys() >= 2);
		}

		private bool Improve(bool singleThread)
		{

			while (run && gridFinder.ShuffleType != ShuffleType.None)
			{
				if (singleThread)
					gridFinder.Improve(Holder.Fixture);  // During generation, hold down at least two of Shift, Ctrl, Alt to Improve single-threaded for easier debugging.
				else
					gridFinder.ImproveParallel(Holder.Fixture);

				if (gridFinder.Changed)
				{
					gridFinder.Changed = false;

					SetFixtureTitle(holder.Fixture, gridFinder.RoundRobinness > 0.9, gridFinder.ScoreScalers.CascadeDifficulty != 0 || gridFinder.ScoreScalers.SameDifficulty != 0);
					RefreshReports();
				}
				textBoxScore.Text = gridFinder.Details;
				Application.DoEvents();
			}

			return gridFinder.Changed;
		}

		int CountModifierKeys()
		{
			return (ModifierKeys.HasFlag(Keys.Control) ? 1 : 0) +
				(ModifierKeys.HasFlag(Keys.Shift) ? 1 : 0) +
				(ModifierKeys.HasFlag(Keys.Alt) ? 1 : 0);
		}

		/// <summary>
		/// Set the title of this fixture, which will be used as the title of the report showing the fixture,
		/// to show whether this fixture is reasonably close to being a perfect round robin, or is seeded 
		/// (either for cascade difficulty or for equal difficulty), or is unseeded.
		/// </summary>
		private void SetFixtureTitle(Fixture fixture, bool roundRobin, bool seeded)
		{
			string fixtureType = roundRobin ? "Round Robin" : seeded ? "Seeded" : "Unseeded";

			bool oneDay = fixture.Games.Any() && fixture.Games.First().Time.Date == fixture.Games.Last().Time.Date;
			string dateText = oneDay ? " " + fixture.Games.First().Time.ToShortDateString() : "";

			fixture.Title = fixtureType + " fixtures for " + holder.League.Title + dateText;
		}

		private void PopulateColours()
		{
			List<Colour> checkedColours = tabFixtureSettings.Controls.OfType<CheckBox>().Where(c => c.Tag is Colour colour && c.Checked).Select(c => (Colour)c.Tag).ToList();

			if (checkRings.Checked)
				gridFinder.SetupColours(checkedColours, (int)numericRings.Value, (int)numericTeamsPerRing.Value, checkRefsRings.Checked);
			else
				gridFinder.SetupColours(checkedColours, (int)numericTeamsPerGame.Value, (int)numericReferees.Value);
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

			if (checkShuffleGames.Checked) st |= checkRings.Checked ? ShuffleType.Rings : ShuffleType.Games;
			if (checkShuffleTeams.Checked) st |= ShuffleType.BetweenGames;
			if (checkShuffleColours.Checked) st |= ShuffleType.WithinGames;
			if (checkShuffleReferees.Checked && checkShuffleReferees.Enabled) st |= ShuffleType.Referees;

			return st;
		}

		private void RefreshReports()
		{
			if (!holder.Fixture.Games.Any())
				return;

			if (checkGameGrid.Checked && checkGameList.Checked)
				reportTeamsList.Report = Reports.FixtureCombined(Holder.Fixture, Holder.League);
			else if (checkGameList.Checked)
				reportTeamsList.Report = Reports.FixtureList(Holder.Fixture, Holder.League);
			else if (checkGameGrid.Checked)
				reportTeamsList.Report = Reports.FixtureGrid(Holder.Fixture);
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

		List<LeagueTeam> Ladder()
		{
			// Find the most appropriate report template showing the date range, drop games, etc.
			var rt = Holder.ReportTemplates.Find(r => r.ReportType == ReportType.MultiLadder) ??
				Holder.ReportTemplates.Find(r => r.ReportType == ReportType.TeamLadder) ??
				Holder.ReportTemplates.Find(r => r.ReportType == ReportType.GameByGame) ??
				Holder.ReportTemplates.Find(r => r.ReportType == ReportType.GameGrid || r.ReportType == ReportType.GameGridCondensed) ??
				Holder.ReportTemplates.Find(r => r.ReportType == ReportType.Pyramid || r.ReportType == ReportType.PyramidCondensed) ??
				new ReportTemplate() { ReportType = ReportType.TeamLadder };

			List<Game> games = Holder.League.Games().Where(g => g.Time > (rt.From ?? DateTime.MinValue) && g.Time < (rt.To ?? DateTime.MaxValue)).ToList();
			return games.Any() ? Reports.Ladder(Holder.League, games, rt).Select(tle => tle.Team).ToList() : null;
		}

		void FormFixtureShown(object sender, EventArgs e)
		{
			int screenHeight = Screen.GetWorkingArea(this).Height;
			Height = (int)Math.Min(Math.Max(Height, screenHeight * 0.8), screenHeight * 0.99);
			framePyramidRound1.SetSplit();
			splitContainerTeams.SplitterDistance = groupBoxPresets.Right + 4;

			if (Holder.Fixture != null)
			{
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
					foreach (var control in tabFixtureSettings.Controls)
						if (control is CheckBox cb && cb.Tag is Colour colour)
							cb.Checked = coloursUsed.Contains(colour);

				if (coloursUsed.Count > 1)
					numericTeamsPerGame.Value = Math.Max(coloursUsed.Count, 2);
				else if (holder.League.AnyGames())
					numericTeamsPerGame.Value = (int)Math.Round(holder.League.Games().Average(g => g.PlayerCount()));

				loading = false;
				TeamCheckedChanged(null, null);
				FixtureTeamCheckedChanged(null, null);

				fixtureSessions.PopulateFromFixtureGames(Holder.Fixture.Games);
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
			}

			if (lotr)
			{
				numericBackToBack.Value = 500;
				numericPlayThenRef.Value = 1;
			}

			if (!referees)
			{
				checkPastTimesPlayed.Checked = league;
				numericDayLength.Value = defaults || cascade || roundRobin || lotr ? 30 : 0;
				numericNightLength.Value = defaults || cascade || roundRobin || lotr ? 3 : 0;
				numericNoGamesInSession.Value = defaults || cascade || roundRobin || lotr ? -1 : 0;
				numericOneGameInSession.Value = defaults || cascade || roundRobin || lotr ? 2 : 0;
				numericTeamsOnSite.Value = league ? 0 : 1;
				numericSameDifficulty.Value = defaults || sides || roundRobin ? 1 : 0;
				numericCascadeDifficulty.Value = cascade ? 10 : 0;
				numericPerfectColour.Value = sides || lotr ? 0 : -1;
				numericClusterRefereeing.Value = lotr ? 0 : 1;

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

				numericGamesPerTeam.Value = roundRobin ? (int)(numericTeams.Value / 2) : cascade || lotr ? 6 : league ? 3 : 2;
			}

			numericReferees.Value = referees ? 2 : league ? 1 : 0;  // I want to set cascade || roundRobin to have 2 referees per game, but I can't get there in one hop: we have to generate the fixture without referees, then have the user hit Stop, add referees, and hit Generate again.
			if (referees && checkRings.Checked)
				checkRefsRings.Checked = true;

			checkShuffleTeams.Checked = league || roundRobin || sides || lotr;
			checkShuffleGames.Checked = cascade;
			checkShuffleColours.Checked = cascade;
			checkShuffleReferees.Checked = !lotr && !checkRings.Checked && numericReferees.Value > 0;

			if (!referees)
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
			gamesCount = numericTeams.Value * numericGamesPerTeam.Value / (checkRings.Checked ? numericRings.Value * numericTeamsPerRing.Value : numericTeamsPerGame.Value);

			labelGameCount.Text = gamesCount.ToString("0.##");
			fixtureSessions.Games = (int)Math.Ceiling(gamesCount);
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

		private void RefereesValueChanged(object sender, EventArgs e)
		{
			checkShuffleReferees.Enabled = checkRings.Checked ? checkRefsRings.Checked : numericReferees.Value > 0;
		}

		private void CheckRingsCheckedChanged(object sender, EventArgs e)
		{
			bool c = checkRings.Checked;

			labelTeamsPerGame.Visible = !c;
			numericTeamsPerGame.Visible = !c;
			labelRefs.Visible = !c;
			numericReferees.Visible = !c;

			labelTeamsPerRing.Visible = c;
			numericTeamsPerRing.Visible = c;
			labelRings.Visible = c;
			numericRings.Visible = c;
			labelRefsRings.Visible = c;
			checkRefsRings.Visible = c;

			checkShuffleGames.Text = (c ? "Rings" : "Games") + " (will not affect who plays who)";

			if (c)
			{
				numericRefThenPlay.Value = -10;

				if (numericTeams.Value < 18)
					numericTeams.Value = 18;
			}

			TeamCountValueChanged(sender, e);
			RefereesValueChanged(sender, e);
		}

		private void CheckShuffleChanged(object sender, EventArgs e)
		{
			if (!((CheckBox)sender).Checked)
				return;

			if (sender == checkShuffleTeams)
			{
				checkShuffleGames.Checked = false;
				checkShuffleColours.Checked = false;
			}
			else if (sender == checkShuffleGames || sender == checkShuffleColours)
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

		private void ButtonEditClick(object sender, EventArgs e)
		{
			if (Holder?.Fixture == null)
				return;

			var area = Screen.GetWorkingArea(this);

			var formEditFixture = new FormEditFixture
			{
				Icon = (Icon)Icon.Clone(),
				Left = area.Left + area.Width / 10,
				Top = area.Top + area.Height / 10,
				Width = area.Width * 4 / 5,
				Height = area.Height * 4 / 5,
				Fixture = Holder.Fixture.Clone(),
				Rings = checkRings.Checked
			};

			fixtureSessions.PopulateIntoSessions(formEditFixture.Sessions);
			formEditFixture.Sessions.Amend(Holder.Fixture.Games.Count);

			if (formEditFixture.ShowDialog() == DialogResult.OK)
			{
				Holder.Fixture.CopyFrom(formEditFixture.Fixture);
				RefreshReports();
				textBoxScore.Text = formEditFixture.Details;
			}
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
