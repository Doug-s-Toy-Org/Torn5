using System;
using System.Drawing;
using System.Windows.Forms;
using Zoom;

namespace Torn5.Controls
{
	public partial class DisplayReport : UserControl
	{
		readonly Timer RedrawTimer = new Timer();

		ZoomReport report;
		public ZoomReport Report
		{
			get { return report; }
			set
			{
				report = value;
				if (Report != null)
					Text = report.Title;

				RedrawTimerTick(null, null);
			}
		}

		public DisplayReport()
		{
			InitializeComponent();
			DoubleBuffered = true;
			BackgroundImageLayout = ImageLayout.Zoom;
			RedrawTimer.Interval = 1000;
			RedrawTimer.Tick += RedrawTimerTick;
		}

		Size size = new Size(1, 1);
		private void DisplayReportResize(object sender, EventArgs e)
		{
			if (this.Size.Width > size.Width || this.Size.Height > size.Height)
				RedrawTimer.Enabled = true;  // If the control has resized larger, we want to redraw at higher res. But we don't weant to redraw _lots_ of times, so only do it once per second.

			size = this.Size;
		}

		private void RedrawTimerTick(object sender, EventArgs e)
		{
			RedrawTimer.Enabled = false;

			if (!DesignMode)
				Redraw();
		}

		private bool IsDark()
		{
			try
			{
				return (int)Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) == 0;
			}
			catch
			{
				return false;
			}
		}
        
		public void Redraw()
		{
			BackColor = IsDark() ? Color.Black : Color.White;
			BackgroundImage = Report?.ToBitmap(Width, Height, true);
		}
	}
}
