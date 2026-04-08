using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Torn;

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

		public int Rounds { get => fixtureRound.Rounds;
			set
			{
				fixtureRound.Rounds = value; FixtureRepechage.Rounds = value;
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
			string nextRound = round == Rounds ? "Finals" : "Round " + (round + 1).ToString();

			var s = new StringBuilder();
			s.Append("Round " + Round + ": ");
			if (RoundGamesPerTeam != 1)
				s.Append("You play " + RoundGamesPerTeam + " games. ");
			s.Append("Top " + RoundAdvance + " teams ");
			TopWhat(s, fixtureRound);
			s.Append("advance to " + nextRound + ".\r\nRemaining " + RepechageTeams + " to Repêchage " + Round + ".\r\n");
			s.Append("Repêchage " + Round + ": ");
			if (RoundGamesPerTeam != 1)
				s.Append("You play 1 game. ");
			s.Append("Top " + RepechageAdvance + " teams ");
			TopWhat(s, fixtureRepechage);
			s.Append("advance to " + nextRound + ".\r\nRemaining " + (TeamsIn - RoundAdvance - RepechageAdvance) + " eliminated.\r\n");

			return s.ToString();
		}

		void TopWhat(StringBuilder s, PyramidHalfFixture r)
		{
			if ((round == 1 && r == fixtureRound) || r.GamesPerTeam > 1)  // Unseeded, and so shouldn't compare within each game? Or more than one game per team, and so can't compare within each game?
			{
				s.Append("(overall) ");
				return;
			}

			s.AppendFormat("(top {0:N0} from each game", r.Advance / r.Games);

			if (r.Advance % r.Games != 0)
			{
				s.Append(" plus ");

				if (r.Advance % r.Games == 1)
					s.Append("the");
				else
					s.Append(r.Advance % r.Games);

				s.AppendFormat(" best {0} places", (r.Advance / r.Games + 1).Ordinate());
			}
			s.Append(") ");
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
