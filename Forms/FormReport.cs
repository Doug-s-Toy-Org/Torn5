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
		public ReportTemplate ReportTemplate { get; set; } = new ReportTemplate();
		/// <summary>Leagues to report on.</summary>
		public List<League> Leagues { get; set; }
		/// <summary>Output: games from Leagues, filtered by the user's selections.</summary>
		public List<Game> Games { get; internal set; } = new List<Game>();
		/// <summary>Input: when the user launches this dialog, what games were selected in the main form?</summary>
		public List<DateTime> SelectedGameTimes { get; set; }
		/// <summary>If a game's Secret property is true, should we include it in our output Games list?</summary>
		public bool IncludeSecretGames { get; set; }

		private int secretClicked = 0;
		bool chartTypeChanged = false;
		Color disabledColor;
		bool initialising;
		List<Game> allGames = new List<Game>();

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
				allGames = Leagues.SelectMany(l => l.Games()).OrderBy(g => g.Time).ToList();

				var gameTimes = allGames.Select(g => g.Time).OrderBy(dt => dt).ToList();
				if (!gameTimes.Any())
					gameTimes.Add(DateTime.Now);

				datePickerFrom.Value = gameTimes.First().Date;
				datePickerTo.Value = gameTimes.Last().Date;
				var titles = allGames.Select(g => g.Title ?? "").Distinct();
				if (titles.Any())
				{
					descriptionGroup.Items.Clear();
					descriptionGroup.Items.AddRange(titles.ToArray());
				}
			}

			if (ReportTemplate != null)
			{
				listViewReportType.SelectedIndices.Clear();
				if (ReportTemplate.ReportType != ReportType.None)
					listViewReportType.SelectedIndices.Add(listViewReportType.Items.Count <= (int)ReportTemplate.ReportType - 1 ? listViewReportType.Items.Count - 1 : (int)ReportTemplate.ReportType - 1);

				title.Text = ReportTemplate.Title;

				foreach (Control c in splitContainer1.Panel2.Controls)
					if (c is CheckBox checkBox && c.Tag is string s)
						checkBox.Checked = ReportTemplate.Settings.Contains(s);

				radioButtonGames.Checked = ReportTemplate.Drops != null && (ReportTemplate.Drops.CountBest > 0 || ReportTemplate.Drops.CountWorst > 0);
				radioButtonPercent.Checked = ReportTemplate.Drops != null && (ReportTemplate.Drops.PercentBest > 0 || ReportTemplate.Drops.PercentWorst > 0);
				numericUpDownBest.Value = ReportTemplate.Drops == null ? 0 : (Decimal)Math.Max(ReportTemplate.Drops.CountBest, ReportTemplate.Drops.PercentBest);
				numericUpDownWorst.Value = ReportTemplate.Drops == null ? 0 : (Decimal)Math.Max(ReportTemplate.Drops.CountWorst, ReportTemplate.Drops.PercentWorst);

				dateFrom.Checked = ReportTemplate.From is DateTime from && from >= datePickerFrom.MinDate && from <= datePickerFrom.MaxDate;
				if (dateFrom.Checked)
				{
					datePickerFrom.Value = ((DateTime)ReportTemplate.From).Date;
					timePickerFrom.Value = (DateTime)ReportTemplate.From;
				}

				dateTo.Checked = ReportTemplate.To is DateTime to && to >= datePickerTo.MinDate && to <= datePickerTo.MaxDate;
				if (dateTo.Checked)
				{
					datePickerTo.Value = ((DateTime)ReportTemplate.To).Date;
					timePickerTo.Value = (DateTime)ReportTemplate.To;
				}

				descriptionGroup.Text = ReportTemplate.Setting("Group");

				int? i = ReportTemplate.SettingInt("TopN");
				showTopN.Checked = i != null;
				numericUpDownTopN.Value = i ?? 0;

				i = ReportTemplate.SettingInt("AtLeastN");
				numericUpDownAtLeastN.Value = i ?? 0;

				chartType.Text = ReportTemplate.Setting("ChartType") ?? "bar";
				orderBy.Text = ReportTemplate.Setting("OrderBy") ?? "TR×SR";
			}

			initialising = false;
			GameFilterChanged(sender, e);
		}

		void FormReportFormClosed(object sender, FormClosedEventArgs e)
		{
			if (this.DialogResult == DialogResult.OK)
			{
				if (listViewReportType.SelectedIndices.Count > 0)
					ReportTemplate.ReportType = (ReportType)(listViewReportType.SelectedIndices[0] + 1);

				ReportTemplate.Title = title.Text;

				ReportTemplate.Settings.Clear();
				foreach (Control c in splitContainer1.Panel2.Controls)
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
			AbleClear(isDecimal, isTeamOrSolo || r == ReportType.GameGrid || r == ReportType.GameGridCondensed);
			longitudinal.Checked = false;
			AbleClear(showZeroed, isTeamOrSolo || r == ReportType.DetailedGames);

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

			GameFilterChanged(sender, e);
		}

		void DatePickerToValueChanged(object sender, EventArgs e)
		{
			if (!initialising)
				dateTo.Checked = true;

			GameFilterChanged(sender, e);
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
				orderBy.Items.AddRange(new string[] { "scaled victory points then score", "scaled victory points then score ratio" });

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
			GameFilterChanged(sender, e);
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

		private void GameFilterChanged(object sender, EventArgs e)
		{
			if (!initialising)
			{
				ReportTemplate.From = dateFrom.Checked ? datePickerFrom.Value.Add(timePickerFrom.Value.TimeOfDay) : (DateTime?)null;
				ReportTemplate.To = dateTo.Checked ? datePickerTo.Value.Add(timePickerTo.Value.TimeOfDay) : (DateTime?)null;

				Games = allGames.Where(g =>
					g.Time > (ReportTemplate.From ?? DateTime.MinValue) &&
					g.Time < (ReportTemplate.To ?? DateTime.MaxValue) &&
					(!withDescription.Checked || (g.Title ?? "").Contains(descriptionGroup.Text)) &&
					(!selectedGames.Checked || SelectedGameTimes.Any(dt => dt == g.Time))
				).ToList();

				panelGraphic.Invalidate();
			}
		}

		private void PanelGraphicPaint(object sender, PaintEventArgs e)
		{
			if (DesignMode || Games == null || !allGames.Any())
				return;

			var gr = e.Graphics;
			gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
			var font = panelGraphic.Font;
			SizeF textSize = gr.MeasureString("24", font);

			Rectangle r = new Rectangle(0, (int)textSize.Height, panelGraphic.Width - 2, panelGraphic.Height - (int)textSize.Height - 1);
			if (r.Width <= 0)
				return;

			gr.FillRectangle(new SolidBrush(BackColor), r);  // Clear background of old paint.

			var minimumBetween = TimeSpan.FromHours(1);
			for (int i = 0; i < allGames.Count - 1; i++)
				if (minimumBetween > allGames[i + 1].Time - allGames[i].Time)
					minimumBetween = allGames[i + 1].Time - allGames[i].Time;

			var earliestTime = TimeSpan.FromHours(Math.Truncate(allGames.Min(g => g.Time.TimeOfDay).TotalHours));  // Earliest TimeOfDay of any game, rounded down.
			var latestTime = TimeSpan.FromHours(Math.Ceiling((allGames.Max(g => g.Time.TimeOfDay) + minimumBetween).TotalHours));  // Latest TimeOfDay of any game, rounded up.

			// Is system default for a 24-hour-style clock, or an AM/PM-style clock?
			bool is24 = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("H");

			// Put a label every _interval_ hours. Depends on space: more space, smaller interval, more labels.
			int interval = (int)((latestTime - earliestTime).TotalHours * 2.5 * textSize.Width / r.Width);

			if (interval < 1) interval = 1;
			if (interval > 24) interval = 24;

			interval = 24 / (24 / interval);  // Make it a number that divides nicely into 24.

			StringFormat sf = new StringFormat();
			Pen pen = new Pen(Color.LightGray);
			for (TimeSpan hour = TimeSpan.FromHours((int)Math.Ceiling(earliestTime.TotalHours / interval) * interval); hour <= latestTime; hour = hour.Add(TimeSpan.FromHours(interval)))
			{
				int x = r.Left + (int)Scale(hour, earliestTime, latestTime, r.Width);

				sf.Alignment = x < r.Left + 10 ? StringAlignment.Near : x < r.Right - 10 ? StringAlignment.Center : StringAlignment.Far;
				gr.DrawString(Hour(hour, is24), font, Brushes.Black, x, 0, sf);

				if (hour < latestTime)
					gr.DrawLine(pen, x, r.Top, x, r.Bottom);
			}

			var days = allGames.Select(g => g.Time.Date).Distinct().OrderBy(d => d).ToList();

			int dayHeight = days.Count > 4 ? r.Height / days.Count : r.Height / 4;
			int top = (days.Count > 4 ? 0 : (r.Height - dayHeight * days.Count) / 2) + r.Y;

			foreach (var day in days)
			{
				foreach (var game in allGames.Where(g => g.Time.Date == day).ToList())
				{
					var start = game.Time;
					var x = Scale(game.Time.TimeOfDay, earliestTime, latestTime, r.Width);
					var gameWidth = Scale(minimumBetween, new TimeSpan(0), latestTime - earliestTime, r.Width);
					if (gameWidth < 1.25F) gameWidth = 1.25F;

					Brush brush = new SolidBrush(Games.Contains(game) ? Color.Blue : Utility.MixColors(panelGraphic.BackColor, Color.Gray, 0.5));
					gr.FillRectangle(brush, x, top, gameWidth * 0.8F, dayHeight - 1);
				}
				top += dayHeight;
			}
		}

		/// <summary>value, scaleMin and scaleMax are all in the before-scaling ordinate system. outputWidth is the range of the after-scaling ordinate system.</summary>
		float Scale(TimeSpan value, TimeSpan scaleMin, TimeSpan scaleMax, float outputWidth)
		{
			return outputWidth * (value - scaleMin).Ticks / (scaleMax - scaleMin).Ticks;
		}

		string Hour(TimeSpan hour, bool is24)
		{
			if (is24 && hour.TotalHours == 24)
				return "24";

			// Format the hour like it was part of a time of day; i.e. if the system has a 12-hour clock: 12, 1, etc.
			string s = new DateTime(2000, 1, 1).Add(hour).ToString(is24 ? "H " : "h ");

			// But we've been forced to ask for "h " or "H " instead of just "h" or "H", because if you ask for a single-character format string it thinks it's a _standard_ format string not a _custom_ format string, and it throws. So trim the trailing " " we were forced to add.
			return s.Trim(' ');
		}

		private void SplitContainer1Panel2Resize(object sender, EventArgs e)
		{
			int margin = dateFrom.Left;

			groupBoxReduce.Left = splitContainer1.Panel2.Width - groupBoxReduce.Width - margin;
			groupBoxDateRange.Width = groupBoxReduce.Left - margin;

			panelGraphic.Width = groupBoxDateRange.Width - timePickerFrom.Right - margin - 1;
			panelGraphic.Invalidate();

			buttonCancel.Left = splitContainer1.Panel2.Width - buttonCancel.Width - margin * 3 / 2;
			buttonOK.Left = buttonCancel.Left - buttonCancel.Width - margin;

			button1.Top = splitContainer1.Panel2.Height - button1.Height;
		}
	}
}
