using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Torn5.Controls
{
	public partial class PyramidFixture : UserControl
	{
		int round;
		public int Round { get => round;
			set
			{
				round = value;
				checkBoxRepechage.Text = " Repêchage " + round.ToString();
				fixtureRound.RoundNumber = value;
				fixtureRepechage.RoundNumber = value;
			}
		}

		public int TeamsIn { get => fixtureRound.TeamsIn; set { fixtureRound.TeamsIn = value; ValueChangedInternal(); } }
		public int TeamsOut { get => fixtureRound.Advance + (checkBoxRepechage.Checked ? fixtureRepechage.Advance : 0); }
		public int RepechageTeams { get => checkBoxRepechage.Checked ? fixtureRepechage.TeamsIn : 0; }
		public int RoundGames { get => fixtureRound.Games; set => fixtureRound.Games = value; }
		public int RepechageGames { get => checkBoxRepechage.Checked ? fixtureRepechage.Games : 0; set => fixtureRepechage.Games = value; }
		public int RoundAdvance { get => fixtureRound.Advance; set { fixtureRound.Advance = value; ValueChangedInternal(); } }
		public int RepechageAdvance { get => HasRepechage ? fixtureRepechage.Advance : 0; set => fixtureRepechage.Advance = value; }
		public int PlanB { get => HasRepechage ? fixtureRepechage.PlanB : 0; }
		int prevousPlanB;
		public int PreviousPlanB { get => prevousPlanB;
			set
			{
				prevousPlanB = value; ValueChangedInternal();
			}
		}
		public bool HasRepechage { get => checkBoxRepechage.Checked; set => checkBoxRepechage.Checked = value; }
		public int RoundGamesPerTeam { get => fixtureRound.GamesPerTeam; set => fixtureRound.GamesPerTeam = value; }
		public int DesiredTeamsPerGame { get => fixtureRound.DesiredTeamsPerGame; set { fixtureRound.DesiredTeamsPerGame = value; fixtureRepechage.DesiredTeamsPerGame = value; } }

		public PyramidHalfFixture FixtureRound { get => fixtureRound; }
		public PyramidHalfFixture FixtureRepechage { get => fixtureRepechage; }

		[Browsable(true)] [Category("Action")]
		public event EventHandler ValueChanged;

		public PyramidFixture()
		{
			InitializeComponent();
		}

		public void Idealise(double advanceRatePerPartRound)
		{
			fixtureRound.Idealise(advanceRatePerPartRound);
			fixtureRepechage.Idealise(advanceRatePerPartRound);
		}

		public string Description()
		{
			string s = "Round " + Round + ": ";
			if (RoundGamesPerTeam != 1)
				s += "You play " + RoundGamesPerTeam + " games. ";
			s += "Top " + RoundAdvance + " teams advance to Round " + (Round + 1) + ". Remaining " + RepechageTeams + " to Repêchage " + Round + ".\r\n";
			s += "Repêchage " + Round + ": ";
			if (RoundGamesPerTeam != 1)
				s += "You play 1 game. ";
			s += "Top " + RepechageAdvance + " teams advance to Round " + (Round + 1) + ". Remaining " + (TeamsIn - RoundAdvance - RepechageAdvance) + " eliminated.\r\n";

			return s;
		}

		private void HalfFixtureChanged(object sender, System.EventArgs e)
		{
			ValueChangedInternal();

			ValueChanged?.Invoke(this, e);
		}

		private void ValueChangedInternal()
		{
			fixtureRepechage.TeamsIn = Math.Max(fixtureRound.TeamsOut + prevousPlanB, 0);
			Invalidate();
		}

		private void CheckBoxRepechageCheckedChanged(object sender, System.EventArgs e)
		{
			fixtureRepechage.Visible = checkBoxRepechage.Checked;
			HalfFixtureChanged(sender, e);
			Invalidate();
		}

		/// <summary>Draw arrows etc. on the background of the control. Don't try to rewrite this to change AutoScaleDimensions, do the drawing, then change scale back: drawing that occurs after us will get the wrong scale.</summary>
		private void PyramidFixture_Paint(object sender, PaintEventArgs e)
		{
			Pen pen = new Pen(Color.FromArgb(160, 160, 160), 2 * DeviceDpi / 96F);  // Pen will be 2 pixels wide at 100% scaling.
			var g = e.Graphics;

			float xIn = fixtureRound.Left + fixtureRound.Width * 0.07F;
			float xRound = fixtureRound.Left + fixtureRound.Width * 0.57F;
			float xRepechage = fixtureRepechage.Left + fixtureRepechage.Width * 0.57F;
			float yArrowTop = fixtureRepechage.Bottom;

			float scale = DeviceDpi / 96F;

			g.FillRectangle(new SolidBrush(BackColor), 0, yArrowTop * scale, Width, Height - yArrowTop * scale);  // Clear background of old paint.

			if (HasRepechage)
				ArcAndLeft(g, pen, xRepechage, xRound + 5, yArrowTop, 7);  // Arc and line from repechage advance number.

			ArcAndLeft(g, pen, xRound, xIn + 7, yArrowTop, 7);  // Arc and line from round advance number.
			ArcAndDownArrow(g, pen, xIn, yArrowTop + 14, 7);  // Arc downward and arrowhead.

			if (PlanB > 0)
			{
				float xPlanB = fixtureRepechage.Left + fixtureRepechage.Width * 0.91F;
				float xRepIn = fixtureRepechage.Left + fixtureRepechage.Width * 0.07F;

				ArcAndLeft(g, pen, xPlanB, xRepIn + 7, yArrowTop, 14);  // Arc and line from repechage advance number.
				ArcAndDownArrow(g, pen, xRepIn, yArrowTop + 21, 7);  // Finish with arrow down to next repechage.
			}
		}

		/// <summary>Arc starts at 'right', goes downwards and turns leftwards. Line goes from there to 'left'.</summary>
		void ArcAndLeft(Graphics g, Pen p, float right, float left, float top, float radius)
		{
			g.DrawArc(p, right - radius - 1, top - radius, radius * 2, radius * 2, 0, 90);  // Arcs are drawn on x, y, width, height...
			g.DrawLine(p, right, top + radius, left, top + radius);  // but lines are drawn on x1, y1, x2, y2.
		}

		/// <summary>Arc starts from right and turns down. Line goes down and ends in arrowhead.</summary>
		void ArcAndDownArrow(Graphics g, Pen p, float x, float y, float radius)
		{
			float halfArrowWidth = 7;

			g.DrawArc(p, x, y - radius, radius * 2, radius * 2, 180, 90);
			g.DrawLine(p, x, y, x, Height - (int)halfArrowWidth - 1);

			var arrowhead = new PointF[] { new PointF(x - halfArrowWidth, Height - halfArrowWidth - 1), new PointF(x + halfArrowWidth, Height - halfArrowWidth - 1), new PointF(x, Height - 1) };
			g.FillPolygon(new SolidBrush(p.Color), arrowhead);
		}
	}
}
