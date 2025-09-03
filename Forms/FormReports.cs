﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Torn.Report;
using Zoom;

namespace Torn.UI
{
	/// <summary>
	/// List of report templates for user to add/edit/delete.
	/// </summary>
	public partial class FormReports : Form
	{
		readonly FormReport formReport = null;
		public Holder Holder { get; set; }

		public FormReports()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			formReport = new FormReport();
		}

		public FormReports(Holder holder): this()
		{
			Holder = holder;
			var games = Holder.League.Games();
			if (games.Any())
			{
				formReport.From = games.First().Time.Date;
				formReport.To = games.Last().Time.Date;
				formReport.League = Holder.League;
			}
			RefreshListView();

			switch (Holder.ReportTemplates.OutputFormat) {
				case OutputFormat.Svg:       radioSvg.Checked = true;    break;
				case OutputFormat.HtmlTable: radioTables.Checked = true; break;
				case OutputFormat.Tsv:       radioTsv.Checked = true;    break;
				case OutputFormat.Csv:       radioCsv.Checked = true;    break;
			}
		}

		/// <summary>Rebuild the list view from the ReportTemplates collection.</summary>
		void RefreshListView()
		{
			// Remember what was selected.
			List<ReportTemplate> reportTemplates = new List<ReportTemplate>();
			foreach (ListViewItem item in listViewReports.SelectedItems)
				reportTemplates.Add((ReportTemplate)item.Tag);

			listViewReports.Items.Clear();

			// Rebuild from ReportTemplates.
			foreach(ReportTemplate reportTemplate in Holder.ReportTemplates)
			{
				ListViewItem item = new ListViewItem(reportTemplate.ReportType.ToString());
				item.SubItems.Add(reportTemplate.Title);
				item.SubItems.Add(reportTemplate.ReportType == ReportType.PageBreak ? "----" : reportTemplate.ToString());
				item.Tag = reportTemplate;
				listViewReports.Items.Add(item);
			}

			// Restore selection.
			foreach (ListViewItem item in listViewReports.Items)
				if (reportTemplates.Contains((ReportTemplate)item.Tag))
					item.Selected = true;

			listViewReports.Focus();
		}

		void ButtonAddClick(object sender, EventArgs e)
		{
			formReport.ReportTemplate = null;

			if (formReport.ShowDialog() == DialogResult.OK)
			{
				formReport.ReportTemplate.Validate();
				Holder.ReportTemplates.Add(formReport.ReportTemplate);
				RefreshListView();
			}
		}

		private void ButtonPageBreakClick(object sender, EventArgs e)
		{
			Holder.ReportTemplates.Add(new ReportTemplate(ReportType.PageBreak, null));
			RefreshListView();
		}

		void FormReportsShown(object sender, EventArgs e)
		{
			Text = "Reports for " + Holder.League.Title;
		}
		
		void ButtonEditClick(object sender, EventArgs e)
		{
			if (listViewReports.SelectedItems.Count > 0)
			{
				formReport.ReportTemplate = (ReportTemplate)listViewReports.SelectedItems[0].Tag;
				if (formReport.ShowDialog() == DialogResult.OK)
					RefreshListView();
			}
		}
		
		void ButtonDeleteClick(object sender, EventArgs e)
		{
			if (listViewReports.SelectedItems.Count > 0)
			{
				var item = listViewReports.SelectedItems[0];
				Holder.ReportTemplates.Remove((ReportTemplate)item.Tag);
				listViewReports.Items.Remove(item);
				RefreshListView();
			}
		}
		
		void ButtonUpClick(object sender, EventArgs e)
		{
			if (listViewReports.SelectedItems.Count > 0)
			{
				int i = listViewReports.SelectedIndices[0];
				if (i > 0)
					Swap(i, i - 1);
			}
		}
		
		void ButtonDownClick(object sender, EventArgs e)
		{
			if (listViewReports.SelectedItems.Count > 0)
			{
				int i = listViewReports.SelectedIndices[0];
				if (i < listViewReports.Items.Count - 1)
					Swap(i, i + 1);
			}
		}
		
		void Swap(int a, int b)
		{
			ReportTemplate temp = Holder.ReportTemplates[a];
			Holder.ReportTemplates[a] = Holder.ReportTemplates[b];
			Holder.ReportTemplates[b] = temp;

			RefreshListView();
		}
		
		void RadioCheckedChanged(object sender, EventArgs e)
		{
			Holder.ReportTemplates.OutputFormat = (OutputFormat)((Control)sender).Tag;
		}

		private void ButtonDefaultsClick(object sender, EventArgs e)
		{
			Holder.ReportTemplates.AddDefaults(Holder.League);
			RefreshListView();
		}
		private void ListViewReportsKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyData & Keys.KeyCode)
			{
				case Keys.Add:
				case Keys.Oemplus:
					ButtonAddClick(null, null);
					e.Handled = true;
					break;
				case Keys.F2:
					ButtonEditClick(null, null);
					e.Handled = true;
					break;
				case Keys.Delete:
					ButtonDeleteClick(null, null);
					e.Handled = true;
					break;
				case Keys.Up:
					if (e.Alt)
					{
						ButtonUpClick(null, null);
						e.Handled = true;
					}
					break;
				case Keys.Down:
					if (e.Alt)
					{
						ButtonDownClick(null, null);
						e.Handled = true;
					}
					break;
			}
		}

		float previousScale = 1;
		protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
		{
			base.ScaleControl(factor, specified);

			float scale = factor.Width;
			if (scale != previousScale)
				Utility.ScaleListViewColumns(listViewReports, scale / previousScale);
			previousScale = scale;
		}
	}
}
