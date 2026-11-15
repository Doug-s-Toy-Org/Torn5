using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Torn;
using Torn.Grids;

namespace Torn5.Forms
{
	public partial class FormEditFixture : Form
	{
		public Fixture Fixture
		{
			get => gridEditor.Fixture;
			set { gridEditor.Fixture = value; }
		}
		public bool Rings { get; set; }
		public string Details { get => gridFinder?.Details;	}

		public Sessions Sessions { get => gridEditor.Sessions; set { gridEditor.Sessions = value; } }

		private GridFinder gridFinder;

		public FormEditFixture()
		{
			InitializeComponent();
		}

		private void FormEditFixture_Shown(object sender, EventArgs e)
		{
			FormEditFixture_ResizeEnd(sender, e);
			GridEditorChanged(sender, e);
		}

		private void GridEditorChanged(object sender, EventArgs e)
		{
			gridFinder = new GridFinder();
			gridFinder.Colours.AddRange(gridEditor.ColoursUsed);
			gridFinder.Sessions = Sessions;
			gridFinder.SetupFromFixture(Fixture);
			textPlayCounts.Text = gridFinder.PlayCounts(Rings);

			var sb = new StringBuilder();
			gridFinder.BackToBacks(sb);
			sb.AppendLine("");
			gridFinder.DaysAndNights(sb);
			textScore.Text = sb.ToString();
		}

		private void FormEditFixture_ResizeBegin(object sender, EventArgs e)
		{
			gridEditor.ResizeBegin();
		}

		private void FormEditFixture_ResizeEnd(object sender, EventArgs e)
		{
			splitContainerTop.SplitterDistance = gridEditor.UsefulWidth(splitContainerTop.SplitterDistance) + 1;
			gridEditor.ResizeEnd();

			textPlayCounts.Font = new System.Drawing.Font("Lucida Console", gridEditor.CellSize() / 2F);
		}

		private void SplitterMoved(object sender, SplitterEventArgs e)
		{
			gridEditor.Invalidate();
		}

		private void ButtonPasteClick(object sender, EventArgs e)
		{
			if (Clipboard.ContainsText())
			{
				Fixture.Games.Clear();
				var lines = Clipboard.GetText().Split(new string[1] { "\r\n" }, StringSplitOptions.None);

				if (Sessions.Any())
					Fixture.Games.Parse(lines, Fixture.Teams, Sessions.First().Start, Sessions.Between);
				else
					Fixture.Games.Parse(lines, Fixture.Teams, DateTime.Now, TimeSpan.FromMinutes(15));

				gridEditor.Invalidate();
			}
		}

		private void ButtonCopyClick(object sender, EventArgs e)
		{
			Clipboard.SetText(string.Join("\r\n", Fixture.Games.ToGrid(Fixture.Teams)));
		}
	}
}
