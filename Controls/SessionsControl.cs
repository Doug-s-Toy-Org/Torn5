using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Torn.Grids;

namespace Torn5.Controls
{
	public partial class SessionsControl : UserControl
	{
		public int Games
		{
			get => Controls.OfType<SessionControl>().Sum(s => s.Games);
			set
			{
				var sessions = Controls.OfType<SessionControl>().OrderBy(s => s.Start).ToList();
				if (!sessions.Any())
					return;

				int sum = sessions.Sum(s => s.Games);
				int newLastGames = sessions.Last().Games + value - sum;
				if (sum != value && 0 < newLastGames && newLastGames <= 9999)
					sessions.Last().Games = newLastGames;
			}
		}

		public SessionsControl()
		{
			InitializeComponent();
			Add();
		}

		private void ButtonAddClick(object sender, EventArgs e)
		{
			Add();
		}

		private void Add()
		{
			var sessions = Controls.OfType<SessionControl>().OrderBy(s => s.Start).ToList();

			DateTime start;
			TimeSpan between;
			if (sessions.Any())
			{
				start = sessions.Last().End.AddMinutes(60);
				between = sessions.Last().Between;
			}
			else
			{
				var now = DateTime.Now;
				start = now.Minute <= 45 ? new DateTime(now.Year, now.Month, now.Day, now.Hour, (int)Math.Ceiling(now.Minute / 15.0) * 15, 0, now.Kind) :  // Start at the next quarter hour.
					new DateTime(now.Year, now.Month, now.Day, now.Hour + 1, 0, 0, now.Kind);
				between = TimeSpan.FromMinutes(15);
			}

			new SessionControl { Changed = Changed, Dock = DockStyle.Top, Parent = this, Start = start, Between = between };
			buttonRemove.Enabled = Controls.OfType<SessionControl>().Count() > 1;
			Changed();
		}

		private void ButtonRemoveClick(object sender, EventArgs e)
		{
			var lastSession = Controls.OfType<SessionControl>().OrderBy(s => s.Start).Last();
			Controls.Remove(lastSession);

			buttonRemove.Enabled = Controls.OfType<SessionControl>().Count() > 1;
			panelTop.Invalidate();
		}

		private void Changed()
		{
			var sessions = Controls.OfType<SessionControl>().OrderBy(s => -s.Start.Ticks).ToList();

			Controls.SetChildIndex(panelTop, sessions.Count);

			for (int i = sessions.Count - 1; i >= 0; i--)
				Controls.SetChildIndex(sessions[i], i);

			panelTop.Invalidate();
		}

		/// <summary>Draw rectangles to represent each game. Each row is a day; each column is a particular time of day.</summary>
		private void PanelTopPaint(object sender, PaintEventArgs e)
		{
			if (DesignMode)
				return;

			var sessions = Controls.OfType<SessionControl>().OrderBy(s => s.Start).ToList();
			if (!sessions.Any())
				return;

			var g = e.Graphics;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
			int margin = buttonAdd.Left;
			var font = buttonAdd.Font;
			SizeF textSize = g.MeasureString("24", font);
			Rectangle r = new Rectangle(buttonAdd.Right + margin, (int)textSize.Height + margin, panelTop.Width - buttonAdd.Right - margin - 1, panelTop.Height - (int)textSize.Height - margin * 2);
			g.FillRectangle(new SolidBrush(BackColor), r);  // Clear background of old paint.

			TimeSpan midnight = new TimeSpan(0, 0, 0);
			bool sessionCrossesMidnight = sessions.Any(s => s.Start.Date < s.End.Date && s.End.TimeOfDay != midnight);
			bool sessionEndsAtMidnight = sessions.Any(s => s.End.TimeOfDay == midnight);

			var earliestTime = sessionCrossesMidnight ? midnight : sessions.Min(s => s.Start.TimeOfDay);  // Earliest TimeOfDay of any session.
			var latestTime = sessionCrossesMidnight || sessionEndsAtMidnight ? new TimeSpan(24, 0, 0) : sessions.Max(s => s.End.TimeOfDay);  // Latest TimeOfDay of any session.

			var days = sessions.Select(s => s.Start.Date).Union(sessions.Select(s => s.End.Date)).Distinct().OrderBy(d => d).ToList();

			// Is system default for a 24-hour-style clock, or an AM/PM-style clock?
			bool is24 = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("H");

			// Put a label every _interval_ hours. Depends on space: more space, smaller interval, more labels.
			int interval = (int)((latestTime - earliestTime).TotalHours * 2.5 * textSize.Width / r.Width);

			if (interval < 1) interval = 1;
			if (interval > 24) interval = 24;

			interval = 24 / (24 / interval);  // Make it a number that divides nicely into 24.

			StringFormat sf = new StringFormat();
			Pen pen = new Pen(Color.LightGray);
			for (int i = 0; i <= 24 / interval; i++)
			{
				int x = r.Left + (int)Scale(new TimeSpan(i * interval, 0, 0), earliestTime, latestTime, r.Width);
				if (x < r.Left || x > r.Right)
					continue;

				sf.Alignment = x < r.Left + 10 ? StringAlignment.Near : x < r.Right - 10 ? StringAlignment.Center : StringAlignment.Far;
				string hour = Hour(i * interval, is24);

				g.DrawString(hour, font, Brushes.Black, x, margin, sf);
				g.DrawLine(pen, x, r.Top, x, r.Bottom);
			}

			// Height of each row 
			int dayHeight = days.Count > 4 ? r.Height / days.Count : r.Height / 4;
			int top = days.Count > 4 ? r.Top : r.Top + (r.Height - dayHeight * days.Count) / 2;

			foreach (var day in days)
			{
				foreach (var session in sessions.Where(s => s.Start.Date <= day && day <= s.End.Date).ToList())
				{
					var start = session.Start.Date == day ? session.Start.TimeOfDay : midnight;
					var end = session.End == day.AddDays(1) || session.End.Date > day ? new TimeSpan(24, 0, 0) : session.End.TimeOfDay;
					var x = r.Left + Scale(start, earliestTime, latestTime, r.Width);
					var gameWidth = Scale(session.Between, midnight, latestTime - earliestTime, r.Width);
					if (gameWidth < 1) gameWidth = 1;

					Brush brush = new SolidBrush(brewerDark2Colors[sessions.IndexOf(session) % brewerDark2Colors.Count]);
					for (var i = 0; i < session.Games && start < end; i++)
					{
						g.FillRectangle(brush, x + i * gameWidth, top, Math.Max(gameWidth - 1, 1), dayHeight - 1);
						start += session.Between;
					}
				}
				top += dayHeight;
			}
		}

		/// <summary>value, scaleMin and scaleMax are all in the before-scaling ordinate system. outputWidth is the range of the after-scaling ordinate system.</summary>
		float Scale(TimeSpan value, TimeSpan scaleMin, TimeSpan scaleMax, float outputWidth)
		{
			return outputWidth * (value - scaleMin).Ticks / (scaleMax - scaleMin).Ticks;
		}

		// Based on https://emilhvitfeldt.github.io/r-color-palettes/discrete/RColorBrewer/Dark2/index.html
		static readonly List<Color> brewerDark2Colors = new List<Color>
		{
			Color.FromArgb(0x1B, 0x9E, 0x77),
			Color.FromArgb(0x6D, 0x2F, 0x01),
			Color.FromArgb(0x75, 0x70, 0xB3),
			Color.FromArgb(0xE7, 0x29, 0x8A),
			Color.FromArgb(0x44, 0x6E, 0x14),
			Color.FromArgb(0x99, 0x72, 0x02),
			Color.FromArgb(0x53, 0x3B, 0x0E),
			Color.FromArgb(0x66, 0x66, 0x66)
		};

		string Hour(int hour, bool is24)
		{
			if (is24 && hour == 24)
				return "24";

			// Format the hour like it was part of a time of day; i.e. if the system has a 12-hour clock: 12, 1, etc.
			string s = (new DateTime(2000, 1, 1).AddHours(hour)) .ToString(is24 ? "H " : "h ");

			// But we've been forced to ask for "h " or "H " instead of just "h" or "H", because if you ask for a single-character format string it thinks it's a _standard_ format string not a _custom_ format string, and it throws. So trim the trailing " " we were forced to add.
			return s.Trim(' ');
		}

		public void Populate(Sessions sessions)
		{
			sessions.Clear();
			var controls = Controls.OfType<SessionControl>().OrderBy(s => s.Start).ToList();
			int game = 0;
			for (var i = 0; i < controls.Count; i++)
			{
				var s = controls[i];
				bool nextSessionSameDay = i < controls.Count - 1 && s.End.Date == controls[i + 1].Start.Date;
				sessions.Add(s.Session(game, nextSessionSameDay ? BreakType.WithinDay : BreakType.Night));
				game += s.Games;
			}
		}
	}
}
