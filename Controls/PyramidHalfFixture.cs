using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Zoom;

namespace Torn5.Controls
{
	public partial class PyramidHalfFixture : UserControl
	{
		bool isRound;
		[Browsable(true)]
		/// <summary>True: we're a round. False: we're a repechage.</summary>
		public bool IsRound { get => isRound;
			set
			{
				isRound = value;
				RefreshTitle();
			}
		}

		int roundNumber;
		[Browsable(true)]
		public int RoundNumber
		{
			get => roundNumber;
			set
			{
				roundNumber = value;
				RefreshTitle();
				labelAdvance.Text = roundNumber == 3 ? "To Finals" : "To Round " + (roundNumber + 1).ToString();
				Centre(labelAdvance, numericAdvance);
			}
		}

		void RefreshTitle()
		{
			if (isRound)
			{
				labelTitle.Text = "Round " + roundNumber.ToString();
				labelEliminate.Text = "Eliminate";
			}
			else
			{
				labelTitle.Text = "Repêchage " + roundNumber.ToString();
				labelEliminate.Text = "To Rep " + (roundNumber + 1).ToString();
				labelEliminate.Visible = roundNumber < 3;
				numericEliminate.Visible = roundNumber < 3;
			}

			Centre(labelTitle, labelTeamsIn);
			Centre(labelEliminate, numericEliminate);
		}

		/// <summary>Centre one control above another.</summary>
		void Centre(Control controlToCentre, Control controlToAlignOn)
		{
			int newLeft = controlToAlignOn.Left + controlToAlignOn.Width / 2 - controlToCentre.Width / 2;
			controlToCentre.Left = Math.Min(Math.Max(newLeft, 0), this.Width - controlToCentre.Width);
		}

		public string Title { get => labelTitle.Text; }

		int teamsIn;
		public int TeamsIn { get => teamsIn; set { teamsIn = value; ValueChangedInternal(); } }
		public int TeamsOut { get => teamsIn - Advance - Eliminate; }
		public int Games { get => (int)numericGames.Value; set { if (value > 0) numericGames.Value = value; ValueChangedInternal(); } }
		public int Advance { get => (int)numericAdvance.Value; set { numericAdvance.Value = value; ValueChangedInternal(); } }
		public int Eliminate { get => isRound ? (int)numericEliminate.Value : teamsIn - Advance; }
		public int PlanB { get => isRound ? 0 : (int)numericEliminate.Value; }

		int desiredTeamsPerGame = -1;
		public int DesiredTeamsPerGame { get => desiredTeamsPerGame; set { desiredTeamsPerGame = value; ValueChangedInternal(); } }

		int gamesPerTeam = 1;
		public int GamesPerTeam
		{
			get => gamesPerTeam; set
			{
				gamesPerTeam = value;
				labelTeamsPerGame.Text = (teamsIn * GamesPerTeam / numericGames.Value).ToString();
				ValueChangedInternal();
			}
		}

		[Browsable(true)]
		[Category("Action")]
		public event EventHandler ValueChanged;

		public PyramidHalfFixture()
		{
			InitializeComponent();
		}

		double _advanceRatePerPartRound = -1;
		public void Idealise(double advanceRatePerPartRound)
		{
			_advanceRatePerPartRound = advanceRatePerPartRound;
			Games = (int)Math.Ceiling(1.0 * TeamsIn * GamesPerTeam / desiredTeamsPerGame);
			Advance = (int)Math.Round(1.0 * TeamsIn * advanceRatePerPartRound);
		}

		private void NumericChanged(object sender, System.EventArgs e)
		{
			ValueChangedInternal();

			ValueChanged?.Invoke(this, e);
		}

		private void ValueChangedInternal()
		{
			labelTeamsIn.Text = teamsIn.ToString();

			decimal tpg = teamsIn * GamesPerTeam / numericGames.Value;
			labelTeamsPerGame.Text = Truncate00(tpg);

			if (teamsIn > 0)
				labelAdvancePercent.Text = Truncate00(numericAdvance.Value / teamsIn * 100) + "%"; //String.Format("{0:0.00%}", numericAdvance.Value / teamsIn);

			if (desiredTeamsPerGame != - 1)
			{
				double ratio = (double)tpg / desiredTeamsPerGame;

				if (ratio > 1)
				{
					labelTeamsPerGame.BackColor = ZReportColors.Mix(SystemColors.Control, Color.Red, 0.5);
					toolTip1.SetToolTip(labelTeamsPerGame, "Too many teams per game. Try increasing number of Games.");
				}
				else if (tpg != (int)tpg)
				{
					labelTeamsPerGame.BackColor = ZReportColors.Mix(SystemColors.Control, Color.Orange, 0.5);
					toolTip1.SetToolTip(labelTeamsPerGame, "Teams Per Game is not a whole number. Try adjusting previous Games and/or Advance numbers to get this to a whole number.");
				}
				else if (ratio < 0.8)
				{
					labelTeamsPerGame.BackColor = ZReportColors.Mix(SystemColors.Control, Color.Orange, ratio);
					toolTip1.SetToolTip(labelTeamsPerGame, "Too few teams per game. Try decreasing number of Games.");
				}
				else
				{
					labelTeamsPerGame.BackColor = SystemColors.Control;
					toolTip1.SetToolTip(labelTeamsPerGame, null);
				}
			}

			if (_advanceRatePerPartRound != -1)
			{
				double ratio = (double)numericAdvance.Value / teamsIn / _advanceRatePerPartRound;

				if (numericGames.Value / GamesPerTeam > numericAdvance.Value)
				{
					labelAdvancePercent.BackColor = ZReportColors.Mix(SystemColors.Control, Color.Red, 0.5);
					toolTip1.SetToolTip(labelAdvancePercent, "Less than one team per game is advancing: " + (numericGames.Value / GamesPerTeam - numericAdvance.Value).ToString() + " teams will win their game but still not advance. Try advancing more teams.");
				}
				else if (ratio > 1.1)
				{
					labelAdvancePercent.BackColor = ZReportColors.Mix(SystemColors.Control, Color.Orange, 1 / ratio);
					toolTip1.SetToolTip(labelAdvancePercent, "Advance percentage is higher than ideal.");
				}
				else if (ratio < 0.9)
				{
					labelAdvancePercent.BackColor = ZReportColors.Mix(SystemColors.Control, Color.Orange, ratio);
					toolTip1.SetToolTip(labelAdvancePercent, "Advance percentage is lower than ideal.");
				}
				else
				{
					labelAdvancePercent.BackColor = SystemColors.Control;
					toolTip1.SetToolTip(labelAdvancePercent, null);
				}
			}
		}

		/// <summary>Return x as a string. If x is a whole number, return it padded with spaces the width of ".00"; otherwise return it with two decimal places.</summary>
		string Truncate00(decimal x)
		{
			if (x == (int) x)  // If x is a whole number, print it as a whole number, plus invisible characters the width of ".00"
				return x.ToString("F0", CultureInfo.CurrentCulture) + '\u2008' + '\u2002' + '\u2002';  // punctutation space (width of a .), en space (nut), en space (nut).
			else  //  else print it with its two actual decimal places showing.
				return x.ToString("F2", CultureInfo.CurrentCulture);
		}

		private void NumericKeyUp(object sender, KeyEventArgs e)
		{
			var _ = ((NumericUpDown)sender).Value;  // This black magic forces the control's ValueChanged to fire after the user edits the text in the control.
		}

		public override string ToString()
		{
			return (IsRound ? "Round " : "Repêchage ") + roundNumber;
		}
	}
}
