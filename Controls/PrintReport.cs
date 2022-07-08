﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Zoom;

namespace Torn5.Controls
{
	/// <summary>
	/// UI to print and/or save a report template.
	/// </summary>
	public partial class PrintReport : UserControl
	{
		public ZoomReport Report { get; set; }
		public Image Image { get; set; }

		[Browsable(true)]
		public DisplayReport DisplayReport { get; set; }

		readonly SaveFileDialog saveFileDialog = new SaveFileDialog();
		readonly PrintDialog printDialog = new PrintDialog();
		readonly PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog();

		public PrintReport()
		{
			InitializeComponent();
		}

		string fileName;
		private void ButtonSaveClick(object sender, EventArgs e)
		{
			var outputFormat = radioSvg.Checked ? OutputFormat.Svg :
				radioTables.Checked ? OutputFormat.HtmlTable :
				radioTsv.Checked ? OutputFormat.Tsv :
				radioCsv.Checked ? OutputFormat.Csv :
				OutputFormat.Png;

			string file = (Report ?? DisplayReport.Report).Title.Replace('/', '-').Replace(' ', '_') + "." + outputFormat.ToExtension();  // Replace / with - so dates still look OK, and  space with _ to make URLs easier if this file is uploaded to the web.
			saveFileDialog.FileName = Path.GetInvalidFileNameChars().Aggregate(file, (current, c) => current.Replace(c, '_'));  // Replace all other invalid chars with _.

			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				if (radioPng.Checked)
				{
					if (numericScale.Visible)
						Image = (Report ?? DisplayReport.Report).ToBitmap((float)numericScale.Value);

					(Image ?? DisplayReport.BackgroundImage).Save(saveFileDialog.FileName);
				}
				else
				{
					var reports = new ZoomReports()
					{
						(Report ?? DisplayReport.Report)
					};

					using (StreamWriter sw = File.CreateText(saveFileDialog.FileName))
						sw.Write(reports.ToOutput(outputFormat));
				}
				fileName = saveFileDialog.FileName;
				buttonShow.Enabled = true;
			}
		}

		private void ButtonShowClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start(fileName);
		}

		private void ButtonPrintClick(object sender, EventArgs e)
		{
			var pd = (Report ?? DisplayReport.Report).ToPrint();
			if (printDialog.ShowDialog() == DialogResult.OK)
				pd.Print();
		}

		private void ButtonPrintPreviewClick(object sender, EventArgs e)
		{
			printPreviewDialog.Document = (Report ?? DisplayReport.Report).ToPrint();
			printPreviewDialog.ShowDialog();
		}

		private void radioPng_Click(object sender, EventArgs e)
		{
			numericScale.Visible = (ModifierKeys.HasFlag(Keys.Control) && ModifierKeys.HasFlag(Keys.Shift)) ||
				(ModifierKeys.HasFlag(Keys.Control) && ModifierKeys.HasFlag(Keys.Alt)) ||
				(ModifierKeys.HasFlag(Keys.Shift) && ModifierKeys.HasFlag(Keys.Alt));
		}
	}
}
