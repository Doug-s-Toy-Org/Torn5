﻿using System;
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
		public League League { get; set; }

		bool chartTypeChanged = false;
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
			listBoxReportType.Focus();

			if (League != null)
			{
				var titles = League.AllGames.Select(g => g.Title ?? "").Distinct();
				if (titles.Any())
				{
					descriptionGroup.Items.Clear();
					descriptionGroup.Items.AddRange(titles.ToArray());
				}
			}

			if (ReportTemplate != null)
			{
				listBoxReportType.SelectedIndex = (int)ReportTemplate.ReportType - 1;

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

				string s = ReportTemplate.Settings.Find(x => x.StartsWith("ChartType=", StringComparison.OrdinalIgnoreCase));
				chartType.Text = string.IsNullOrEmpty(s) ? "bar" : s.Substring(s.IndexOf('=') + 1);

				s = ReportTemplate.Settings.Find(x => x.StartsWith("TopN=", StringComparison.OrdinalIgnoreCase));
				bool b = !string.IsNullOrEmpty(s);
				showTopN.Checked = b;
				numericUpDownTopN.Enabled = b;
				numericUpDownTopN.Value = b ? int.Parse(s.Substring(s.IndexOf('=') + 1), CultureInfo.InvariantCulture) : 0;

				s = ReportTemplate.Settings.Find(x => x.StartsWith("AtLeastN=", StringComparison.OrdinalIgnoreCase));
				b = !string.IsNullOrEmpty(s);
				atLeastN.Checked = b;
				numericUpDownAtLeastN.Enabled = b;
				numericUpDownAtLeastN.Value = b ? int.Parse(s.Substring(s.IndexOf('=') + 1), CultureInfo.InvariantCulture) : 0;

				s = ReportTemplate.Settings.Find(x => x.StartsWith("ChartType=", StringComparison.OrdinalIgnoreCase));
				chartType.Text = string.IsNullOrEmpty(s) ? "bar" : s.Substring(s.IndexOf('=') + 1);

				s = ReportTemplate.Settings.Find(x => x.StartsWith("OrderBy=", StringComparison.OrdinalIgnoreCase));
				orderBy.Text = string.IsNullOrEmpty(s) ? "score" : s.Substring(s.IndexOf('=') + 1);
			}

//			ListBoxReportTypeSelectedIndexChanged(null, null);
		}

		void FormReportFormClosed(object sender, FormClosedEventArgs e)
		{
			if (this.DialogResult == DialogResult.OK)
			{
				if (ReportTemplate == null)
					ReportTemplate = new ReportTemplate();

				ReportTemplate.ReportType = (ReportType)(listBoxReportType.SelectedIndex + 1);

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
			switch (orderBy.SelectedIndex) {
				case 0: return "score";
				case 1: return "score ratio";
				case 2: return "scaled score";
				case 3: return "scaled score ratio";
				default: return "";
			}
		}

		void ListBoxReportTypeSelectedIndexChanged(object sender, EventArgs e)
		{
			int i = listBoxReportType.SelectedIndex;
			ReportType r = (ReportType)(i + 1);

			scaleGames.Enabled = r == ReportType.TeamLadder;
			dropGames.Enabled = r == ReportType.TeamLadder || r == ReportType.SoloLadder || r == ReportType.GameGrid;
			dateFrom.Enabled = true;
			dateTo.Enabled = true;
			showColours.Enabled = r == ReportType.TeamLadder;
			showPoints.Enabled = r == ReportType.TeamsVsTeams;
			showComments.Enabled = r == ReportType.SoloLadder;
			chartType.Enabled = true;
			showTopN.Enabled = r == ReportType.TeamLadder || r == ReportType.SoloLadder;
			numericUpDownTopN.Enabled = r == ReportType.TeamLadder || r == ReportType.SoloLadder;
			labelTopWhat.Enabled = r == ReportType.TeamLadder || r == ReportType.SoloLadder;
			atLeastN.Enabled = r == ReportType.TeamLadder || r == ReportType.SoloLadder;
			numericUpDownAtLeastN.Enabled = r == ReportType.TeamLadder || r == ReportType.SoloLadder;
			labelAtLeastGames.Enabled = r == ReportType.TeamLadder || r == ReportType.SoloLadder;
			orderBy.Enabled = r == ReportType.TeamLadder || r == ReportType.SoloLadder;
			labelOrderBy.Enabled = r == ReportType.TeamLadder || r == ReportType.SoloLadder;
			description.Enabled = true;
			longitudinal.Enabled = r == ReportType.TeamLadder || r == ReportType.SoloLadder || r == ReportType.Packs;
			if (r == ReportType.Packs && ReportTemplate?.ReportType == ReportType.Packs)
				longitudinal.Checked = true;

			labelTopWhat.Text = r == ReportType.SoloLadder ? "players" : "teams";

			if (!chartTypeChanged)
				chartType.SelectedIndex =
					i == 0 || i == 1 || i == 2 ? 3 :  // TeamLadder, TeamsVsTeams, SoloLadder: bar with rug
					i == 10 ? 8 :  // Packs report: kernel density estimate with rug
					i == 11 ? 6 :  // Tech report: histogram
					1;  // everything else: bar
		}

		void DatePickerFromValueChanged(object sender, EventArgs e)
		{
			dateFrom.Checked = true;
		}
		
		void DatePickerToValueChanged(object sender, EventArgs e)
		{
			dateTo.Checked = true;
		}

		void DropGamesCheckedChanged(object sender, EventArgs e)
		{
			groupBoxDrops.Enabled = dropGames.Checked;
		}
		
		void ShowTopNCheckedChanged(object sender, EventArgs e)
		{
			numericUpDownTopN.Enabled = showTopN.Checked;
		}
		
		void AtLeastNCheckedChanged(object sender, EventArgs e)
		{
			numericUpDownAtLeastN.Enabled = atLeastN.Checked;
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

		private void WithDescriptionCheckedChanged(object sender, EventArgs e)
		{
			descriptionGroup.Enabled = withDescription.Checked;
		}
	}
}
