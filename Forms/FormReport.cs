using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Torn.Report;

namespace Torn.UI
{
	/// <summary>
	/// Allow user to create or edit a report template.
	/// </summary>
	public partial class FormReport : Form
	{
		public ReportTemplate ReportTemplate { get; set; }
		public DateTime From { set { datePickerFrom.Value = value; } get { return datePickerFrom.Value; } }
		public DateTime To { set { datePickerTo.Value = value; } get { return datePickerTo.Value; } }
		public List<League> Leagues { get; set; }

		private int secretClicked = 0;
		bool chartTypeChanged = false;
		Color disabledColor;
		bool initialising;

		public FormReport()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();

			timePickerFrom.CustomFormat = CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern;
			timePickerTo.CustomFormat = CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern;
		}

		void FormReportShown(object sender, EventArgs e)
		{
			initialising = true;
			Text = "Report on " + (Leagues.Count == 1 ? Leagues.First().Title : Leagues.Count.ToString() + " leagues");

			List<string> reports = new List<string>
			{
				"Team Ladder",
				"Multi Ladder",
				"Teams vs teams",
				"Solo Ladder",
				"Game by game",
				"Game grid",
				"Game grid condensed",
				"Pyramid",
				"Pyramid condensed",
				"Ascension",
				"Ascension as a grid",
				"Colours",
				"Term Report",
				"Sanity Check",
				"Detailed Games",
				"Everything",
			};

			List<string> descriptions = new List<string>
			{
				"Teams, ranked.",
				"Uses game descriptions to group games into rounds.",
				"How many times teams play and defeat each other.",
				"Players, ranked.",
				"Good for 3 team games.",
				"Good for many team games.",
				"Takes less width than Game grid.",
				"Good where teams get eliminated after each round.",
				"Takes less width than Pyramid.",
				"For semifinals.",
				"Takes less width than Ascension.",
				"Performance of each colour.",
				"Terminations, warnings, etc.",
				"Try me!",
				"",
				"Good for data export.",
			};

			disabledColor = Utility.MixColors(listViewReportType.ForeColor, listViewReportType.BackColor, 0.25);
			listViewReportType.Items.Clear();
			for (int i = 0; i < reports.Count && i < descriptions.Count; i++)
			{
				var item = listViewReportType.Items.Add(reports[i]);
				item.SubItems.Add(descriptions[i]);
				if (Leagues.Count > 1 && i != 11 && i != 13)
					item.ForeColor = disabledColor;
			}

			listViewReportType.Focus();

			if (Leagues.Any())
			{
				var games = Leagues.SelectMany(l => l.Games());

				var gameTimes = games.Select(g => g.Time).OrderBy(dt => dt).ToList();
				if (!gameTimes.Any())
					gameTimes.Add(DateTime.Now);

				From = gameTimes.First().Date;
				To = gameTimes.Last().Date;

				var titles = games.Select(g => g.Title ?? "").Distinct();
				if (titles.Any())
				{
					descriptionGroup.Items.Clear();
					descriptionGroup.Items.AddRange(titles.ToArray());
				}
			}

			if (ReportTemplate != null)
			{
				listViewReportType.SelectedIndices.Clear();
				listViewReportType.SelectedIndices.Add(listViewReportType.Items.Count <= (int)ReportTemplate.ReportType - 1 ? listViewReportType.Items.Count - 1 : (int)ReportTemplate.ReportType - 1);

				title.Text = ReportTemplate.Title;

				foreach (Control c in this.Controls)
					if (c is CheckBox checkBox && c.Tag != null)
						checkBox.Checked = ReportTemplate.Settings.Contains((string)c.Tag);

				radioButtonGames.Checked = ReportTemplate.Drops != null && (ReportTemplate.Drops.CountBest > 0 || ReportTemplate.Drops.CountWorst > 0);
				radioButtonPercent.Checked = ReportTemplate.Drops != null && (ReportTemplate.Drops.PercentBest > 0 || ReportTemplate.Drops.PercentWorst > 0);
				numericUpDownBest.Value = ReportTemplate.Drops == null ? 0 : (Decimal)Math.Max(ReportTemplate.Drops.CountBest, ReportTemplate.Drops.PercentBest);
				numericUpDownWorst.Value = ReportTemplate.Drops == null ? 0 : (Decimal)Math.Max(ReportTemplate.Drops.CountWorst, ReportTemplate.Drops.PercentWorst);

				dateFrom.Checked = ReportTemplate.From != null && (DateTime)ReportTemplate.From >= datePickerFrom.MinDate && (DateTime)ReportTemplate.From <= datePickerFrom.MaxDate;
				if (dateFrom.Checked)
				{
					datePickerFrom.Value = ((DateTime)ReportTemplate.From).Date;
					timePickerFrom.Value = (DateTime)ReportTemplate.From;
				}

				dateTo.Checked = ReportTemplate.To != null && (DateTime)ReportTemplate.To >= datePickerTo.MinDate && (DateTime)ReportTemplate.To <= datePickerTo.MaxDate;
				if (dateTo.Checked)
				{
					datePickerTo.Value = ((DateTime)ReportTemplate.To).Date;
					timePickerTo.Value = (DateTime)ReportTemplate.To;
				}

				descriptionGroup.Text = ReportTemplate.Setting("Group");
				withDescription.Checked = !string.IsNullOrEmpty(descriptionGroup.Text);

				int? i = ReportTemplate.SettingInt("TopN");
				showTopN.Checked = i != null;
				numericUpDownTopN.Value = i ?? 0;

				i = ReportTemplate.SettingInt("AtLeastN");
				numericUpDownAtLeastN.Value = i ?? 0;

				chartType.Text = ReportTemplate.Setting("ChartType") ?? "bar";
				orderBy.Text = ReportTemplate.Setting("OrderBy") ?? "TR×SR";
			}
			initialising = false;
		}

		void FormReportFormClosed(object sender, FormClosedEventArgs e)
		{
			if (this.DialogResult == DialogResult.OK)
			{
				if (ReportTemplate == null)
					ReportTemplate = new ReportTemplate();

				if (listViewReportType.SelectedIndices.Count > 0)
					ReportTemplate.ReportType = (ReportType)(listViewReportType.SelectedIndices[0] + 1);

				ReportTemplate.Title = title.Text;

				ReportTemplate.Settings.Clear();
				foreach (Control c in this.Controls)
					if (c.Enabled && c is CheckBox checkBox && checkBox.Checked && !string.IsNullOrEmpty((string)c.Tag))
						ReportTemplate.Settings.Add((string)c.Tag);

				if (dropGames.Checked)
				{
					if (ReportTemplate.Drops == null)
						ReportTemplate.Drops = new Drops();

					if (radioButtonGames.Checked)
					{
						ReportTemplate.Drops.CountBest = (int)numericUpDownBest.Value;
						ReportTemplate.Drops.CountWorst = (int)numericUpDownWorst.Value;
					}
					else if (radioButtonPercent.Checked)
					{
						ReportTemplate.Drops.PercentBest = (double)numericUpDownBest.Value;
						ReportTemplate.Drops.PercentWorst = (double)numericUpDownWorst.Value;
					}
				}

				if (chartType.Text != "none")
					ReportTemplate.Settings.Add("ChartType=" + chartType.Text);

				if (showTopN.Checked)
					ReportTemplate.Settings.Add("ShowTopN=" + numericUpDownTopN.Value.ToString(CultureInfo.InvariantCulture));

				if (atLeastN.Checked)
					ReportTemplate.Settings.Add("AtLeastN=" + numericUpDownAtLeastN.Value.ToString(CultureInfo.InvariantCulture));

				if (orderBy.Enabled)
					ReportTemplate.Settings.Add("OrderBy=" + OrderByText());

				ReportTemplate.From = dateFrom.Checked ? datePickerFrom.Value.Add(timePickerFrom.Value.TimeOfDay) : (DateTime?)null;
				ReportTemplate.To = dateTo.Checked ? datePickerTo.Value.Add(timePickerTo.Value.TimeOfDay) : (DateTime?)null;

				if (withDescription.Checked)
					ReportTemplate.Settings.Add("Group=" + descriptionGroup.Text);
			}
		}

		string OrderByText()
		{
			switch (orderBy.SelectedIndex)
			{
				case 0: return "TRxSR";
				case 1: return "tag ratio";
				case 2: return "score ratio";
				case 3: return "score";
				default: return "";
			}
		}

		void ListViewReportTypeSelectedIndexChanged(object sender, EventArgs e)
		{
			int i = listViewReportType.SelectedIndices.Count > 0 ? listViewReportType.SelectedIndices[0] : 0;
			ReportType r = (ReportType)(i + 1);
			bool isTeamOrSolo = r == ReportType.TeamLadder || r == ReportType.SoloLadder;

			AbleClear(scaleGames, r == ReportType.TeamLadder);
			AbleClear(dropGames, isTeamOrSolo || r == ReportType.GameGrid);
			dateFrom.Enabled = true;
			dateTo.Enabled = true;
			AbleClear(showColours, r == ReportType.TeamLadder);
			AbleClear(showPoints, r == ReportType.TeamsVsTeams);
			AbleClear(showComments, r == ReportType.SoloLadder);
			AbleClear(showGrades, r == ReportType.SoloLadder);
			AbleClear(ignorePoints, r == ReportType.GameGrid);
			chartType.Enabled = true;
			AbleClear(showTopN, isTeamOrSolo || r == ReportType.MultiLadder);
			numericUpDownTopN.Enabled = showTopN.Enabled;
			labelTopWhat.Enabled = showTopN.Enabled;
			atLeastN.Enabled = isTeamOrSolo;
			numericUpDownAtLeastN.Enabled = isTeamOrSolo;
			labelAtLeastGames.Enabled = isTeamOrSolo;
			orderBy.Enabled = r == ReportType.SoloLadder;
			labelOrderBy.Enabled = r == ReportType.SoloLadder;
			AbleClear(withDescription, r != ReportType.MultiLadder);
			description.Enabled = true;
			AbleClear(longitudinal, isTeamOrSolo || r == ReportType.Packs);
			AbleClear(showHits, r == ReportType.DetailedGames || r == ReportType.GameByGame || r == ReportType.GameGrid);
			AbleClear(isDecimal, r == ReportType.GameGrid || r == ReportType.TeamLadder || r == ReportType.SoloLadder || r == ReportType.GameGridCondensed);
			longitudinal.Checked = false;
			AbleClear(showZeroed, r == ReportType.TeamLadder || r == ReportType.SoloLadder || r == ReportType.DetailedGames);

			labelTopWhat.Text = r == ReportType.SoloLadder ? "players" : "teams";
			atLeastN.Text = r == ReportType.SoloLadder ? "show only players with at least" : "show only teams with at least";

			if (!chartTypeChanged)
				chartType.SelectedIndex =
					isTeamOrSolo || r == ReportType.TeamsVsTeams ? 3 :  // bar with rug
					r == ReportType.Packs ? 8 :  // kernel density estimate with rug
					1;  // everything else: bar
		}

		/// <summary>Enable/disable a checkbox. If we are disabling it, also clear the value in that checkbox.</summary>
		void AbleClear(CheckBox cb, bool enable)
		{
			cb.Enabled = enable;
			cb.Checked &= enable;
		}

		void DatePickerFromValueChanged(object sender, EventArgs e)
		{
			if (!initialising)
				dateFrom.Checked = true;
		}

		void DatePickerToValueChanged(object sender, EventArgs e)
		{
			if (!initialising)
				dateTo.Checked = true;
		}

		void DropGamesCheckedChanged(object sender, EventArgs e)
		{
			groupBoxDrops.Enabled = dropGames.Checked;
		}

		private void NumericUpDownTopNValueChanged(object sender, EventArgs e)
		{
			showTopN.Checked = numericUpDownTopN.Value > 0;
		}

		private void NumericUpDownAtLeastNValueChanged(object sender, EventArgs e)
		{
			atLeastN.Checked = numericUpDownAtLeastN.Value > 0;
		}

		void ScaleGamesCheckedChanged(object sender, EventArgs e)
		{
			if (scaleGames.Checked && orderBy.Items.Count == 2)
				orderBy.Items.AddRange(new string[] { "scaled victory points then score", "scaled victory points then score ratio" } );

			else if (!scaleGames.Checked && orderBy.Items.Count == 4)
			{
				orderBy.Items.RemoveAt(3);
				orderBy.Items.RemoveAt(2);
			}
		}

		private void ListBoxReportType_DoubleClick(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void ChartTypeSelectedIndexChanged(object sender, EventArgs e)
		{
			if (chartType == ActiveControl) // if this change is being done by the user
				chartTypeChanged = true;
		}

		private void DescriptionGroupTextChanged(object sender, EventArgs e)
		{
			withDescription.Checked = descriptionGroup.Text.Length > 0;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			secretClicked++;
			if (secretClicked == 5)
			{
				listViewReportType.Items.Add("Packs").SubItems.Add("Student's t test");
				listViewReportType.Items.Add("Pack Hits");
			}
		}

		private void ListViewReportTypeResize(object sender, EventArgs e)
		{
			colDescription.Width = listViewReportType.Width - colReportType.Width - SystemInformation.VerticalScrollBarWidth - 4;
		}

		float previousScale = 1;
		protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
		{
			base.ScaleControl(factor, specified);

			float scale = factor.Width;
			if (scale != previousScale)
			{
				Utility.ScaleListViewColumns(listViewReportType, scale / previousScale);
				splitContainer1.SplitterDistance = splitContainer1.Height - buttonOK.Bottom - 24;
			}
			previousScale = scale;
		}
	}
}
