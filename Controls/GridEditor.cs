using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Torn;
using Torn.Grids;

namespace Torn5.Controls
{
	public partial class GridEditor : UserControl
	{
		private Fixture fixture;
		public Fixture Fixture
		{
			get => fixture;
			set
			{
				fixture = value;

				if (value != null)
				{
					coloursUsed.AddRange(fixture.Games.SelectMany(fg => fg.Teams.Select(kv => kv.Value)).Distinct());
					EnsureColour(Colour.Referee);
					for (int i = 0; i < coloursUsed.Count && i < buttons.Count; i++)
						buttons[i].Colour = coloursUsed[i];
				}
			}
		}
		public bool Rings { get; set; }
		public Sessions Sessions { get; set; } = new Sessions();

		[Browsable(true), Category("Action"), Description("Occurs when user clicks a cell thus making a change to the Fixture.")]
		public event EventHandler Changed;

		private int cellSize = 10;

		private class MouseButtonColour
		{
			public string Name;
			public MouseButtons MouseButton;
			public Colour Colour;
		}

		private readonly List<MouseButtonColour> buttons = new List<MouseButtonColour>() {
			new MouseButtonColour { Name = "L", MouseButton = MouseButtons.Left },
			new MouseButtonColour { Name = "M", MouseButton = MouseButtons.Middle },
			new MouseButtonColour { Name = "R", MouseButton = MouseButtons.Right },
			new MouseButtonColour { Name = "\u2190", MouseButton = MouseButtons.XButton1 },
			new MouseButtonColour { Name = "\u2192", MouseButton = MouseButtons.XButton2 },
		};

		private MouseButtons mouseButton;

		private enum SetOrClear { Neither, Setting, Clearing };
		private SetOrClear setOrClear = SetOrClear.Neither;

		private List<Colour> coloursUsed = new List<Colour>();
		public List<Colour> ColoursUsed => coloursUsed;

		private Point point = new Point(-1, -1);  // This is the point in the grid last clicked on. It's counted in grid squares, not in pixels: 9,9 is ninth column, ninth row.
		private int gridLeft;
		private bool resizing = false;
		private Graphics graphics;
		private Font font;

		public GridEditor()
		{
			InitializeComponent();
			graphics = CreateGraphics();
			graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
		}

		private void ThisPaint(object sender, PaintEventArgs e)
		{
			if (Fixture == null)
				return;

			coloursUsed = fixture.Games.SelectMany(fg => fg.Teams.Select(kv => kv.Value)).Distinct().OrderBy(c => c).ToList();
			cellSize = CellSize();
			gridLeft = cellSize * 5;
			int rows = Fixture.Teams.Count;

			graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.None;
			graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
			float scale = DeviceDpi / 96F;
			font = new Font(Font.Name, cellSize / scale * 0.75F);

			if (resizing)
			{
				PaintGrid(cellSize, rows);
				PaintPalette(cellSize, rows);
			}
			else
			{
				if (e.ClipRectangle.Top < (rows + 3) * cellSize)
				{
					PaintTeamNames(cellSize);
					PaintGrid(cellSize, rows);
					PaintCells(cellSize);
					PaintColumnTotals(cellSize, rows);
					PaintDifficulties(cellSize, rows);
					PaintTopCaptions(cellSize);
				}

				PaintPalette(cellSize, rows);
			}

			//DrawStringAtCell(cellSize.ToString(), rows + 3, Fixture.Games.Count, cellSize);
		}

		private void PaintTeamNames(int size)
		{
			for (int t = 0; t < Fixture.Teams.Count; t++)
				FitText(Fixture.Teams[t].Name, 0, (t + 1) * size, size * 5, size);
		}

		private void PaintGrid(int size, int rows)
		{
			graphics.FillRectangle(Brushes.White, gridLeft + 1, size + 1, Fixture.Games.Count * size - 2, rows * size - 1);  // Erase old grid.

			for (int row = 1; row < rows; row++)  // Draw horizontals.
				graphics.DrawLine(Pens.LightGray, gridLeft, (row + 1) * size, gridLeft + Math.Min(Fixture.Games.Count * size, DisplayRectangle.Right), (row + 1) * size);

			for (int col = 0; col < Fixture.Games.Count; col++)  // Draw verticals.
			{
				int session = Sessions.FindIndex(s => s.First == col);

				if (session == -1)
					graphics.DrawLine(Pens.LightGray, gridLeft + col * size, size, gridLeft + col * size, Math.Min((rows + 1) * size, DisplayRectangle.Bottom));
				else  // Lines at the start of a session are drawn darker, and their tops are higher.
					graphics.DrawLine(Pens.Black, gridLeft + col * size, session == 0 || Sessions[session - 1].Break == BreakType.Night ? 1 : size / 2,
						gridLeft + col * size, Math.Min((rows + 1) * size, DisplayRectangle.Bottom));
			}

			// Draw remaining three sides of black outline. Last, so it doesn't get overpainted.
			graphics.DrawLine(Pens.Black, gridLeft, size, gridLeft + Math.Min(Fixture.Games.Count * size, DisplayRectangle.Right), size);
			graphics.DrawLine(Pens.Black, gridLeft, (rows + 1) * size, gridLeft + Math.Min(Fixture.Games.Count * size, DisplayRectangle.Right), (rows + 1) * size);
			graphics.DrawLine(Pens.Black, gridLeft + Fixture.Games.Count * size, size, gridLeft + Fixture.Games.Count * size, Math.Min((rows + 1) * size, DisplayRectangle.Bottom));
		}

		private void PaintTopCaptions(int size)
		{
			foreach (var session in Sessions)
				FitText(session.Start.ToString("t") + " " + session.Start.FriendlyDate(), gridLeft + session.First * size, 0, (session.Last - session.First + 1) * size, size);

			FitText("Difficulty", gridLeft + Fixture.Games.Count * size + 1, 0, cellSize * 5, cellSize);
		}

		private void PaintCells(int size)
		{
			for (int col = 0; col < Fixture.Games.Count; col++)
				foreach (var x in Fixture.Games[col].Teams)
					FillCell(Fixture.Teams.IndexOf(x.Key), col, size, x.Value);
		}

		/// <summary>Draw totals below each game for number of teams playing and refereeing.</summary>
		private void PaintColumnTotals(int size, int rows)
		{
			var games = Fixture.Games;

			graphics.FillRectangle(Brushes.White, gridLeft, (rows + 1) * size + 1, games.Count * size, size * 2);  // Erase old totals.

			for (int col = 0; col < games.Count; col++)
				PaintOneColumnTotal(rows, col, size, false);

			for (int col = 0; col < games.Count; col++)
				PaintOneColumnTotal(rows + 1, col, size, true);
		}

		/// <summary>Draw totals below one game for number of teams playing and refereeing.</summary>
		private void PaintColumnTotal(int col, int size, int rows)
		{
			graphics.FillRectangle(Brushes.White, gridLeft + col * size - 1, (rows + 1) * size + 1, size + 2, size * 2);  // Erase old totals.

			PaintOneColumnTotal(rows, col, size, false);
			PaintOneColumnTotal(rows + 1, col, size, true);
		}

		/// <summary>Draw total below one game for number of teams playing or refereeing.</summary>
		/// <param name="referees">If true, show number of referees in this game. If false, show number of teams playing.</param>
		private void PaintOneColumnTotal(int row, int col, int size, bool referees)
		{
			bool lambda(KeyValuePair<LeagueTeam, Colour> x) => (x.Value == Colour.Referee) == referees;
			var average = Fixture.Games.Where(fg => fg != Fixture.Games[col]).Average(fg => fg.Teams.Count(lambda));  // Average over all games except this one.
			int count = Fixture.Games[col].Teams.Count(lambda);
			DrawStringAtCell(count.ToString(), row, col, size, count < Math.Floor(average) || count > Math.Ceiling(average) ? Color.FromArgb(0xFF, 0xA0, 0xA0) : default);
		}

		private void PaintDifficulties(int size, int rows)
		{
			var games = Fixture.Games;
			var difficulties = new float[rows];
			var counts = new int[rows];
			var averages = new float[rows];

			var teamIndexes = new Dictionary<LeagueTeam, int>();  // Value: Index into Fixture.Teams.
			for (int t = 0; t < Fixture.Teams.Count; t++)
				teamIndexes.Add(Fixture.Teams[t], t);

			foreach (var fg in games)
			{
				var indexesPlayingThisGame = fg.Teams.Where(x => teamIndexes.ContainsKey(x.Key) && x.Value != Colour.Referee).Select(x => teamIndexes[x.Key]);
				foreach (var index in indexesPlayingThisGame)
				{
					difficulties[index] += (indexesPlayingThisGame.Sum() - index) / (indexesPlayingThisGame.Count() - 1F);
					counts[index]++;
				}
			}

			for (int row = 0; row < rows; row++)
				averages[row] = difficulties[row] / counts[row] + 1;

			float max = averages.Count() == 0 ? 1 : averages.Max();
			int difficultiesWidth = cellSize * 5;

			Brush brush = new SolidBrush(Color.FromArgb(0xFF, 0xC0, 0xA0));  // Orange.

			int difficultiesLeft = gridLeft + games.Count * size + 1;

			graphics.FillRectangle(Brushes.White, difficultiesLeft, size, size * 5, rows * size);  // Erase right-hand side text etc.

			// Draw difficulty text and chart.
			for (int row = 0; row < rows; row++)
			{
				graphics.FillRectangle(brush, difficultiesLeft, (row + 1) * size + 1, averages[row] / max * difficultiesWidth, size - 1);
				graphics.DrawString(averages[row].ToString("0.##"), font, Brushes.Black, difficultiesLeft, (row + 1) * size - 2);
			}
		}

		/// <summary>Paint coloured cells onto grid to show teams in games.</summary>
		private void PaintPalette(int size, int rows)
		{
			graphics.DrawString("Palette:", font, Brushes.Black, 1, (rows + 4) * size - 1);

			if (coloursUsed.Any())
				foreach (Colour c in coloursUsed)
					FillCell(rows + 3, (int)c, size, c);
			else
				for (Colour i = Colour.None; i <= Colour.Referee; i++)
					FillCell(rows + 3, (int)i, size, i);

			foreach (var colour in coloursUsed)
			{
				var button = buttons.Find(b => b.Colour == colour);
				if (button != default)
					DrawStringAtCell(button.Name, rows + 3, (int)button.Colour, size);
			}
		}

		public int CellSize()
		{
			if (Fixture?.Games == null)
				return 10;

			int sizeByWidth = Fixture.Games.Count == 0 ? int.MaxValue : Width / (Fixture.Games.Count + 10);  // + 5 for team names; + 5 for difficulty scores.
			int sizeByHeight = Height / (Fixture.Teams.Count + 6);  // + 1 for top margin / header; + 2 for footer totals; + 3 for palette and spacing.

			return Math.Max(Math.Min(sizeByWidth, sizeByHeight), 1);
		}

		public int UsefulWidth(int width)
		{
			if (Fixture.Games.Count == 0)
				return width;

			int cellSize = (int)Math.Round(width / (Fixture.Games.Count + 10.0));
			return (Fixture.Games.Count + 10) * cellSize + 1;
		}

		private void DrawStringAtCell(string text, int row, int col, int size, Color background = default)
		{
			if (background != default)
				using (var brush = new SolidBrush(background))
					FillCell(row, col, size, brush);

			var cell = new Rectangle(gridLeft + (col - 1) * size, (row + 1) * size + 1, size * 3, size);
			var stringFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
			graphics.DrawString(text, font, Brushes.Black, cell, stringFormat);
		}

		void EnsureColour(Colour colour)
		{
			if (!coloursUsed.Contains(colour))
				coloursUsed.Add(colour);
		}

		private void FitText(string text, float x, float y, float width, float height)
		{
			var textSize = graphics.MeasureString(text, font);
			var tempFont = textSize.Width <= width ? font : new Font(font.Name, font.Size * width / textSize.Width);
			graphics.DrawString(text, tempFont, Brushes.Black, x, y + (height - tempFont.Height) / 2);
		}

		/// <summary>Fill a single grid cell with the specified brush.</summary>
		private void FillCell(int row, int col, int size, Brush brush)
		{
			graphics.FillRectangle(brush, gridLeft + col * size + 1, (row + 1) * size + 1, size - 1, size - 1);
		}

		/// <summary>Fill a single grid cell with a representation of the team colour.</summary>
		private void FillCell(int row, int col, int size, Colour colour)
		{
			using (var brush = ColourToBrush(colour, size))
				FillCell(row, col, size, brush);
		}

		/// <summary>Create a disposable brush with a representation of the team colour.</summary>
		private Brush ColourToBrush(Colour colour, int size)
		{
			switch (colour)
			{
				case Colour.None:
					return new SolidBrush(Color.Gray);  // Don't return Brushes.Gray here, because our caller uses it inside a using, which means it would get disposed, and disposing of a static Brush is bad.
				case Colour.White:
					return new LinearGradientBrush(new Rectangle(0, 0, size, size), Color.White, Color.LightGray, LinearGradientMode.ForwardDiagonal);
				case Colour.Black:
					return new LinearGradientBrush(new Rectangle(0, 0, size, size), Color.Black, Color.Gray, LinearGradientMode.BackwardDiagonal);
				case Colour.Fire:
					return new LinearGradientBrush(new Rectangle(0, 0, size, size), Color.Red, Color.Yellow, LinearGradientMode.Vertical);
				case Colour.Ice:
					return new HatchBrush(HatchStyle.LargeConfetti, Color.FromArgb(0x90, 0x90, 0xFF), Color.White);
				case Colour.Earth:
					return new LinearGradientBrush(new Rectangle(0, 0, size, size), Color.FromArgb(0xB0, 0xB0, 0x80), Color.FromArgb(0x30, 0x30, 0x00), LinearGradientMode.Vertical);
				case Colour.Crystal:
					return new HatchBrush(HatchStyle.SolidDiamond, Color.FromArgb(0x80, 0x00, 0x78), Color.FromArgb(0xFF, 0xC0, 0xFF));
				case Colour.Rainbow:
					var brush = new LinearGradientBrush(new Rectangle(0, 0, size, size), Color.Red, Color.Magenta, LinearGradientMode.Horizontal)
					{
						InterpolationColors = new ColorBlend
						{
							Colors = new Color[] { Color.FromArgb(0xFF, 0x40, 0x40), Color.Yellow, Color.FromArgb(0x60, 0xFF, 0x60), Color.FromArgb(0x60, 0x60, 0xFF), Color.FromArgb(0xFF, 0x40, 0xFF) },
							Positions = new float[] { 0F, 0.25F, 0.5F, 0.75F, 1F }
						}
					};
					return brush;
				case Colour.Cops:
					return new HatchBrush(HatchStyle.LargeCheckerBoard, Color.Blue, Color.White);
				case Colour.Referee:
					return new HatchBrush(HatchStyle.DarkVertical, Color.Black, Color.White);
				default:
					return new SolidBrush(colour.ToSaturatedColor());
			}
		}

		public void ResizeBegin()
		{
			resizing = true;
		}

		public void ResizeEnd()
		{
			resizing = false;
			Invalidate();
		}

		private void GridEditorMouseDown(object sender, MouseEventArgs e)
		{
			mouseButton = e.Button;  // If multiple mouse buttons pressed, just remember the most recent.
			point = GridPosition();
			setOrClear = InGrid(point) ? (Fixture.Games[point.X].Teams.ContainsKey(Fixture.Teams[point.Y]) ? SetOrClear.Clearing : SetOrClear.Setting) : SetOrClear.Neither;
			Toggle();
		}

		private void GridEditorMouseMove(object sender, MouseEventArgs e)
		{
			var newPoint = GridPosition();
			if (newPoint != point && InGrid(newPoint))
			{
				point = newPoint;
				Toggle();
			}
		}

		private void GridEditorMouseUp(object sender, MouseEventArgs e)
		{
			var upPoint = GridPosition();

			if (point == upPoint && point.Y == Fixture.Teams.Count + 3 && Enum.IsDefined(typeof(Colour), point.X) &&  // User has clicked palette.
				(coloursUsed.Contains((Colour)point.X) || 
					ModifierKeys.HasFlag(Keys.Control) || ModifierKeys.HasFlag(Keys.Shift) || ModifierKeys.HasFlag(Keys.Alt)))
			{
				var c = (Colour)point.X;
				EnsureColour(c);

				var button = buttons.Find(b => b.MouseButton == e.Button);
				if (button != default)
					button.Colour = c;

				Invalidate(new Rectangle(gridLeft, (Fixture.Teams.Count + 4) * cellSize + 1, cellSize * 19, cellSize));
			}

			mouseButton = MouseButtons.None;
			setOrClear = SetOrClear.Neither;
		}

		private void GridEditorMouseLeave(object sender, EventArgs e)
		{
			mouseButton = MouseButtons.None;
			setOrClear = SetOrClear.Neither;
		}

		private Point GridPosition()
		{
			var newPoint = PointToClient(Cursor.Position);
			return new Point((newPoint.X - gridLeft) / cellSize, newPoint.Y / cellSize - 1);
		}

		private bool InGrid(Point point)
		{
			return Fixture.Games.Valid(point.X) && Fixture.Teams.Valid(point.Y);  // User has clicked within grid.
		}

		private void Toggle()
		{
			if (setOrClear == SetOrClear.Neither)
				return;

			graphics = CreateGraphics();
			var fixtureGameTeams = Fixture.Games[point.X].Teams;
			var team = Fixture.Teams[point.Y];

			switch (setOrClear)
			{
				case SetOrClear.Setting:
					Colour c = buttons.Find(b => (b.MouseButton & mouseButton) != 0).Colour;
					fixtureGameTeams[team] = c;
					FillCell(point.Y, point.X, cellSize, c);
					break;
				case SetOrClear.Clearing:
					if (fixtureGameTeams.Remove(team))
						FillCell(point.Y, point.X, cellSize, Brushes.White);
					break;
			}

			PaintColumnTotal(point.X, cellSize, Fixture.Teams.Count);
			PaintDifficulties(cellSize, Fixture.Teams.Count);
			Changed?.Invoke(this, new EventArgs());
		}
	}
}
