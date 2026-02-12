using System;
using System.Windows.Forms;
using Torn.Grids;

namespace Torn5.Controls
{
	public partial class SessionControl : UserControl
	{
		public DateTime Start { get => dateTimeStart.Value; set => dateTimeStart.Value = value; }
		public TimeSpan Between { get => TimeSpan.FromMinutes((double)numericBetween.Value); set => numericBetween.Value = (decimal)value.TotalMinutes; }
		public int Games { get => (int)numericGames.Value; set => numericGames.Value = value; }
		public DateTime End { get => Start.Add(TimeSpan.FromTicks(Between.Ticks * Games)); }
		public Action Changed { get; set; }

		public SessionControl()
		{
			InitializeComponent();
		}

		public Session Session(int first, BreakType breakType)
		{
			return new Session() { Start = Start, Between = Between, First = first, Last = first + Games - 1, Break = breakType };
		}

		private void ValueChanged(object sender, EventArgs e)
		{
			Changed?.Invoke();
		}
	}
}
