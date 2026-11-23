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

		private int cellSize = 10;  // In pixels.

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
		private bool dragging = false;  // Is the user currently dragging a whole game left or right?
		private int dragFromColumn = -1;

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
				PaintGrid();
				PaintPalette();
			}
			else
			{
				if (e.ClipRectangle.Top < (rows + 3) * cellSize)
				{
					PaintTeamNames();
					PaintGrid();
					PaintCells();
					PaintColumnTotals();
					PaintDifficulties();
					PaintTopCaptions();
				}

				PaintPalette();
			}

			//DrawStringAtCell(cellSize.ToString(), rows + 3, Fixture.Games.Count, cellSize);
		}

		private void PaintTeamNames()
		{
			for (int t = 0; t < Fixture.Teams.Count; t++)
				FitText(Fixture.Teams[t].Name, 0, (t + 1) * cellSize, cellSize * 5, cellSize);
		}

		private void PaintGrid()
		{
			int rows = Fixture.Teams.Count;
			graphics.FillRectangle(Brushes.White, gridLeft + 1, cellSize + 1, Fixture.Games.Count * cellSize - 2, rows * cellSize - 1);  // Erase old grid.

			for (int row = 1; row < rows; row++)  // Draw horizontals.
				graphics.DrawLine(Pens.LightGray, gridLeft, (row + 1) * cellSize, gridLeft + Math.Min(Fixture.Games.Count * cellSize, DisplayRectangle.Right), (row + 1) * cellSize);

			for (int col = 0; col < Fixture.Games.Count; col++)  // Draw verticals.
			{
				int session = Sessions.FindIndex(s => s.First == col);

				if (session == -1)
					graphics.DrawLine(Pens.LightGray, gridLeft + col * cellSize, cellSize, gridLeft + col * cellSize, Math.Min((rows + 1) * cellSize, DisplayRectangle.Bottom));
				else  // Lines at the start of a session are drawn darker, and their tops are higher.
					graphics.DrawLine(Pens.Black, gridLeft + col * cellSize, session == 0 || Sessions[session - 1].Break == BreakType.Night ? 1 : cellSize / 2,
						gridLeft + col * cellSize, Math.Min((rows + 1) * cellSize, DisplayRectangle.Bottom));
			}

			// Draw remaining three sides of black outline. Last, so it doesn't get overpainted.
			graphics.DrawLine(Pens.Black, gridLeft, cellSize, gridLeft + Math.Min(Fixture.Games.Count * cellSize, DisplayRectangle.Right), cellSize);
			graphics.DrawLine(Pens.Black, gridLeft, (rows + 1) * cellSize, gridLeft + Math.Min(Fixture.Games.Count * cellSize, DisplayRectangle.Right), (rows + 1) * cellSize);
			graphics.DrawLine(Pens.Black, gridLeft + Fixture.Games.Count * cellSize, cellSize, gridLeft + Fixture.Games.Count * cellSize, Math.Min((rows + 1) * cellSize, DisplayRectangle.Bottom));
		}

		private void PaintTopCaptions()
		{
			foreach (var session in Sessions)
				FitText(session.Start.ToString("t") + " " + session.Start.FriendlyDate(), gridLeft + session.First * cellSize, 0, (session.Last - session.First + 1) * cellSize, cellSize);

			FitText("Difficulty", gridLeft + Fixture.Games.Count * cellSize + 1, 0, cellSize * 5, cellSize);
		}

		private void PaintCells()
		{
			for (int col = 0; col < Fixture.Games.Count; col++)
				foreach (var kv in Fixture.Games[col].Teams)
					FillCell(Fixture.Teams.IndexOf(kv.Key), col, kv.Value);
		}

		/// <summary>Draw totals below each game for number of teams playing and refereeing.</summary>
		private void PaintColumnTotals()
		{
			int games = Fixture.Games.Count;
			int rows = Fixture.Teams.Count;

			graphics.FillRectangle(Brushes.White, gridLeft, (rows + 1) * cellSize + 1, games * cellSize, cellSize * 2);  // Erase old totals.

			for (int col = 0; col < games; col++)
				PaintOneColumnTotal(rows, col, false);

			for (int col = 0; col < games; col++)
				PaintOneColumnTotal(rows + 1, col, true);
		}

		/// <summary>Draw totals below one game for number of teams playing and refereeing.</summary>
		private void PaintColumnTotal(int col)
		{
			int rows = Fixture.Teams.Count;
			graphics.FillRectangle(Brushes.White, gridLeft + col * cellSize - 1, (rows + 1) * cellSize + 1, cellSize + 2, cellSize * 2);  // Erase old totals.

			PaintOneColumnTotal(rows, col, false);
			PaintOneColumnTotal(rows + 1, col, true);
		}

		/// <summary>Draw total below one game for number of teams playing or refereeing.</summary>
		/// <param name="referees">If true, show number of referees in this game. If false, show number of teams playing.</param>
		private void PaintOneColumnTotal(int row, int col, bool referees)
		{
			bool lambda(KeyValuePair<LeagueTeam, Colour> x) => (x.Value == Colour.Referee) == referees;
			var average = Fixture.Games.Where(fg => fg != Fixture.Games[col]).Average(fg => fg.Teams.Count(lambda));  // Average over all games except this one.
			int count = Fixture.Games[col].Teams.Count(lambda);
			DrawStringAtCell(count.ToString(), row, col, count < Math.Floor(average) || count > Math.Ceiling(average) ? Color.FromArgb(0xFF, 0xA0, 0xA0) : default);
		}

		private void PaintDifficulties()
		{
			int rows = Fixture.Teams.Count;
			var difficulties = new float[rows];
			var counts = new int[rows];
			var averages = new float[rows];

			var teamIndexes = new Dictionary<LeagueTeam, int>();  // Value: Index into Fixture.Teams.
			for (int t = 0; t < Fixture.Teams.Count; t++)
				teamIndexes.Add(Fixture.Teams[t], t);

			foreach (var fg in Fixture.Games)
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

			int difficultiesLeft = gridLeft + Fixture.Games.Count * cellSize + 1;

			graphics.FillRectangle(Brushes.White, difficultiesLeft, cellSize, cellSize * 5, rows * cellSize);  // Erase right-hand side text etc.

			// Draw difficulty text and chart.
			for (int row = 0; row < rows; row++)
			{
				graphics.FillRectangle(brush, difficultiesLeft, (row + 1) * cellSize + 1, averages[row] / max * difficultiesWidth, cellSize - 1);
				graphics.DrawString(averages[row].ToString("0.##"), font, Brushes.Black, difficultiesLeft, (row + 1) * cellSize - 2);
			}
		}

		/// <summary>Paint coloured cells onto grid to show teams in games.</summary>
		private void PaintPalette()
		{
			int rows = Fixture.Teams.Count;
			graphics.DrawString("Palette:", font, Brushes.Black, 1, (rows + 4) * cellSize - 1);

			if (coloursUsed.Any())
				foreach (Colour c in coloursUsed)
					FillCell(rows + 3, (int)c, c);
			else
				for (Colour i = Colour.None; i <= Colour.Referee; i++)
					FillCell(rows + 3, (int)i, i);

			foreach (var colour in coloursUsed)
			{
				var button = buttons.Find(b => b.Colour == colour);
				if (button != default)
					DrawStringAtCell(button.Name, rows + 3, (int)button.Colour);
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

		private void DrawStringAtCell(string text, int row, int col, Color background = default)
		{
			if (background != default)
				using (var brush = new SolidBrush(background))
					FillCell(row, col, brush);

			var cell = new Rectangle(gridLeft + (col - 1) * cellSize, (row + 1) * cellSize + 1, cellSize * 3, cellSize);
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
		private void FillCell(int row, int col, Brush brush)
		{
			graphics.FillRectangle(brush, gridLeft + col * cellSize + 1, (row + 1) * cellSize + 1, cellSize - 1, cellSize - 1);
		}

		/// <summary>Fill a single grid cell with a representation of the team colour.</summary>
		private void FillCell(int row, int col, Colour colour)
		{
			using (var brush = ColourToBrush(colour))
				FillCell(row, col, brush);
		}

		/// <summary>Create a disposable brush with a representation of the team colour.</summary>
		private Brush ColourToBrush(Colour colour)
		{
			switch (colour)
			{
				case Colour.None:
					return new SolidBrush(Color.Gray);  // Don't return Brushes.Gray here, because our caller uses it inside a using, which means it would get disposed, and disposing of a static Brush is bad.
				case Colour.White:
					return new LinearGradientBrush(new Rectangle(0, 0, cellSize, cellSize), Color.White, Color.LightGray, LinearGradientMode.ForwardDiagonal);
				case Colour.Black:
					return new LinearGradientBrush(new Rectangle(0, 0, cellSize, cellSize), Color.Black, Color.Gray, LinearGradientMode.BackwardDiagonal);
				case Colour.Fire:
					return new LinearGradientBrush(new Rectangle(0, 0, cellSize, cellSize), Color.Red, Color.Yellow, LinearGradientMode.Vertical);
				case Colour.Ice:
					return new HatchBrush(HatchStyle.LargeConfetti, Color.FromArgb(0x90, 0x90, 0xFF), Color.White);
				case Colour.Earth:
					return new LinearGradientBrush(new Rectangle(0, 0, cellSize, cellSize), Color.FromArgb(0xB0, 0xB0, 0x80), Color.FromArgb(0x30, 0x30, 0x00), LinearGradientMode.Vertical);
				case Colour.Crystal:
					return new HatchBrush(HatchStyle.SolidDiamond, Color.FromArgb(0x80, 0x00, 0x78), Color.FromArgb(0xFF, 0xC0, 0xFF));
				case Colour.Rainbow:
					var brush = new LinearGradientBrush(new Rectangle(0, 0, cellSize, cellSize), Color.Red, Color.Magenta, LinearGradientMode.Horizontal)
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

			dragging = Fixture.Games.Valid(point.X) && (point.Y == Fixture.Teams.Count || point.Y == Fixture.Teams.Count + 1);
			if (dragging)
			{
				dragFromColumn = point.X;
				PaintDragMark(point.X);
			}
		}

		private void GridEditorMouseMove(object sender, MouseEventArgs e)
		{
			var newPoint = GridPosition();
			if (newPoint != point && InGrid(newPoint))
			{
				point = newPoint;
				Toggle();
			}

			if (dragging && point.X != newPoint.X)
			{
				PaintColumnTotal(point.X);
				point = newPoint;
				PaintDragMark(point.X);
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

			if (dragging)
			{
				if (dragFromColumn == upPoint.X)
					PaintColumnTotal(point.X);
				else if (Fixture.Games.Valid(point.X) && Fixture.Games.Valid(dragFromColumn))
				{
					var gameToMove = Fixture.Games[dragFromColumn];
					Fixture.Games.Remove(gameToMove);
					Fixture.Games.Insert(upPoint.X, gameToMove);
					Invalidate();
				}
			}
			dragging = false;
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
					FillCell(point.Y, point.X, c);
					break;
				case SetOrClear.Clearing:
					if (fixtureGameTeams.Remove(team))
						FillCell(point.Y, point.X, Brushes.White);
					break;
			}

			PaintColumnTotal(point.X);
			PaintDifficulties();
			Changed?.Invoke(this, new EventArgs());
		}

		private void PaintDragMark(int col)
		{
			graphics = CreateGraphics();
			graphics.DrawLine(Pens.Black, gridLeft + col * cellSize, (Fixture.Teams.Count + 1) * cellSize + 3, gridLeft + col * cellSize, (Fixture.Teams.Count + 3) * cellSize - 2);

		}
	}
}
