using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using Svg;

namespace Zoom
{
	public enum ZNumberStyle { Comma, Plain, Brief };  // Comma: use a thousands separator.  Brief: convert larger numbers to 2k, 3M, etc.

	public enum ZAlignment { None, Left, Right, Center, Float, Integer };  // Float and Integer are the same as Right, but give a hint as to the type of data contained in cell.Data. "Integer" means that the underlying data is made of integers (e.g. average score, average rank) not that the number displayed in the cell is an integer.

	public static class ZAlignmentExtensions
	{
		public static string ToHtml(this ZAlignment alignment)
		{
			switch (alignment)
			{
				case ZAlignment.None:   return "";
				case ZAlignment.Left:   return " align=\"left\"";
				case ZAlignment.Center: return " align=\"center\"";
				default:                return " align=\"right\"";
			}
		}
	}

	public enum OutputFormat { Svg = 0, HtmlTable, Tsv, Csv, Png };

	public static class OutputFormatExtensions
	{
		public static string ToExtension(this OutputFormat outputFormat)
		{
			switch (outputFormat) {
				case OutputFormat.Svg:
				case OutputFormat.HtmlTable: return "html";
				case OutputFormat.Tsv: return "tsv";
				case OutputFormat.Csv: return "csv";
				case OutputFormat.Png: return "png";
				default: return "";
			}
		}

		public static bool IsHtml(this OutputFormat outputFormat)
		{
			return (outputFormat == OutputFormat.Svg || outputFormat == OutputFormat.HtmlTable);
		}
	}

	/// <summary>This is the abstract parent of ZColumn and ZCell, holding only a few fields common to "rectangular areas that can contain text".</summary>
	public class ZBlock
	{
		/// <summary>If this cell or column heading contains text which links to another report, put the URL of that report here.</summary>
		public string Hyper { get; set; }
		/// <summary>Optional background color.</summary>
		public Color Color { get; set; }
		/// <summary>Can hold whatever data the caller wants.</summary>
		public object Tag { get; set; }
	}

	/// <summary>This is just the header row(s) and metadata for a column -- does not include the actual cells.</summary>
	public class ZColumn: ZBlock
	{
		/// <summary>Text to appear in the column header.</summary>
		public string Text { get; set; }
		/// <summary>Optional. Appears *above* heading text.</summary>
		public string GroupHeading { get; set; }
		/// <summary>left, right or center</summary>
		public ZAlignment Alignment { get; set; }
		/// <summary>If true, we should try rotating the header text to make it fit better.</summary>
		public bool Rotate { get; set; }
		/// <summary>If true, try to adjust text spacing so that all cells in this column have text that fills the whole available width.</summary>
		public bool FillWidth { get; set; }
		public List<Arrow> Arrows { get; }
		/// <summary>Optional background colour.</summary>

		public ZColumn(string text = null, ZAlignment alignment = ZAlignment.Left, string groupHeading = null)
		{
			Arrows = new List<Arrow>();
			Text = text;
			Alignment = alignment;
			GroupHeading = groupHeading;
		}

		public override string ToString()
		{
			return Text;
		}

		/// <summary>Adds a simple arrow with one From and one To, both in the specified row and of the specified width.</summary>
		public void AddArrow(int row, int width, Color color = default, bool expand = false)
		{
			var arrow = new Arrow { Color = color };
			Arrows.Add(arrow);
			arrow.From.Add(new ZArrowEnd(row, width) { Expand = expand } );
			arrow.To.Add(new ZArrowEnd(row, width) { Expand = expand } );
		}
	}

	/// <summary>Start or end of an Arrow. An Arrow can have multiple starts and multiple ends.</summary>
	public class ZArrowEnd: IComparable
	{
		public int Row { get; set; }
		public double Width { get; set; }
		/// <summary>If true, look to see if this From end can be moved leftwards into empty cells, or this To end can be moved rightwards into empty cells.</summary>
		public bool Expand { get; set; }

		/// <summary>If set, paint a number on this arrow end.</summary>
		public int? Number { get; set; }

		public ZArrowEnd(int row, double width)
		{
			Row = row;
			Width = width;
		}

		int IComparable.CompareTo(object obj)
		{
			return Row - ((ZArrowEnd)obj).Row;
		}

		public override string ToString()
		{
			return "Row " + Row + "; Width " + Width + (Expand ? " Expand" : "");
		}
	}

	/// <summary>An Arrow is a connection between some cells. Cells in the column to the left are "From" entries. Cells in the column to the right are "To". The arrow will join all these in a pretty way.</summary>
	public class Arrow
	{
		public List<ZArrowEnd> From { get; set; } // Cells in the column to the left of the arrow to draw starting points from.
		public List<ZArrowEnd> To { get; set; }  // Cells in the column to the right of the arrow to draw to.
		public Color Color { get; set; }
		
		public Arrow()
		{
			From = new List<ZArrowEnd>();
			To = new List<ZArrowEnd>();
		}

		public double MaxWidth()
		{
			return Math.Max(From.Any() ? From.Max(x => x.Width) : 0, To.Any() ? To.Max(x => x.Width) : 0);
		}
	}

	[Flags]
	public enum ChartType { None = 0, Bar = 1, Rug = 2, BoxPlot = 4, Histogram = 8, KernelDensityEstimate = 16, Area = 32, XYScatter = 64};
	public static class ChartTypeExtensions
	{
		public static ChartType ToChartType(string value)
		{
			ChartType chartType = ChartType.None;
			if (string.IsNullOrEmpty(value)) return chartType;

			if (value.Contains("rug")) chartType |= ChartType.Rug;
			if (value.Contains("bar")) chartType |= ChartType.Bar;
			if (value.Contains("box")) chartType |= ChartType.BoxPlot;
			if (value.Contains("histogram")) chartType |= ChartType.Histogram;
			if (value.Contains("kernel")) chartType |= ChartType.KernelDensityEstimate;
			if (value.Contains("columns")) chartType |= ChartType.Area;
			return chartType;
		}
	}

	public class ChartPoint
	{
		public Color Color { get; set; }
		public DateTime X { get; set; }
		public double Y { get; set; }
		
		public ChartPoint(DateTime x, double y, Color color)
		{
			Color = color;
			X = x;
			Y = y;
		}

		public override string ToString()
		{
			return "ChartPoint " + X.ToString() + ", " + Y.ToString();
		}
	}

	/// <summary>Represents a single cell in a table. The cell can optionally have a horizontal chart bar.</summary>
	public class ZCell: ZBlock
	{
		string text;

		/// <summary>Text to display in the cell.</summary>
		public string Text
		{
			get 
			{
				if (text == null)
				{
					if (Number is double n)
					{
						if (double.IsNegativeInfinity(n))
							return "-\u221E";
						else if (double.IsInfinity(n))
							return "\u221E";
						else if (double.IsNaN(n))
							return "-";
						else if (string.IsNullOrEmpty(NumberFormat))
							return n.ToString();
						else
							return n.ToString(NumberFormat, CultureInfo.CurrentCulture);
					}
					else
						return "";
				}
				else
					return text;
			}
			
			set { text = value; }
		}

		/// <summary>Like Text, but can contain markup.</summary>
		public string Html { get; set; }

		/// <summary>Like Text, but can contain markup.</summary>
		public string Svg { get; set; }

		/// <summary>For multiple classes, separate each class with a space. (Blame CSS.)</summary>
		public string CssClass { get; set; }

		/// <summary>If this cell contains a number, put it here.</summary>
		public double? Number { get; set; }
		/// <summary>A standard .NET-style numeric format string like N, F, P, etc.; optionally followed by a numeric precision.
		/// If you specify a format starting with 'E' or 'G', it changes values like 0.0000123 to a style like 1.23x10^-5, but with Unicode superscript digits.
		/// Special format lowercase 'f' or 'n' will trim trailing 0's after a decimal where the value is a whole number, so "1.00" becomes "1"; "1.20" remains "1.20".</summary>
		public string NumberFormat { get; set; }
		/// <summary>If set, show a chart for this cell. If ChartCell is not set, use this cell's number; otherwise use the cell specified in ChartCell.</summary>
		public ChartType ChartType { get; set; }
		/// <summary>Optional pointer to cell whose value we are to show as a chart. If no ChartType is set, we assume ChartType.Bar.</summary>
		public ZCell ChartCell { get; set; }
		/// <summary>Color of text in the cell.</summary>
		public Color TextColor { get; set; }
		/// <summary>Color of optional horizontal chart bar.</summary>
		public Color BarColor { get; set; }
		/// <summary>Color of optional cell outline.</summary>
		public Color Border { get; set; }
		/// <summary>List of values to be shown as a scatter plot / quartile plot / stem-and-leaf plot / rug map / kernel density estimation.</summary>
		public List<double> Data { get; private set; }

		public ZCell()
		{
		}

		public ZCell(string text = "", Color color = default, ZCell barCell = null)
		{
			Text = text;
			Color = color;
			ChartCell = barCell;
		}

		public ZCell(double? number, ChartType chartType = ChartType.None, string numberFormat = "", Color color = default)
		{
			Number = number;
			NumberFormat = numberFormat;
			Color = color;
			ChartType = chartType;
			Data = new List<double>();
		}

		public bool Empty()
		{
			return string.IsNullOrEmpty(text) && Number == null;
		}

		public bool EmptyOrNaN()
		{
			return string.IsNullOrEmpty(text) && (Number == null || double.IsNaN((double)Number));
		}

		public string OutputText(OutputFormat of)
		{
			return
				of == OutputFormat.Svg && !string.IsNullOrEmpty(Svg) ? Svg :
				of == OutputFormat.HtmlTable && !string.IsNullOrEmpty(Html) ? Html :
				of == OutputFormat.Tsv || of == OutputFormat.Csv ? Text :
				FormatNumber(WebUtility.HtmlEncode(Text), Number, NumberFormat);
		}

		/// <summary>Formats the value of a cell.
		/// If you specify a format starting with 'E' or 'G', it changes values like 0.0000123 to a style like 1.23x10^-5, but with Unicode superscript digits.
		/// Special format lowercase 'f' or 'n' will trim trailing 0's after a decimal where the value is a whole number, so "1.00" becomes "1   "; "1.20" remains "1.20".</summary>
		private string FormatNumber(string text, double? number, string format)
		{
			if (number == null)
				return text;

			char formatSpecifier = format?.Length >= 1 ? format[0] : ' ';

			if (formatSpecifier != 'E' && formatSpecifier != 'G' && formatSpecifier != 'f' && formatSpecifier != 'n')
				return text;

			var n = (double)number;
			var s2 = new StringBuilder();

			int precision = format?.Length >= 2 ? int.Parse(format.Substring(1)) : 0;

			string decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

			if ((formatSpecifier == 'f' || formatSpecifier == 'n') && precision > 0 && n == (int)n)  // The format specifies decimals, but this is a whole number.
			{
				int pos = text.IndexOf(decimalSeparator);
				if (pos > -1)
				{
					s2.Append(text.Substring(0, pos));  // Append the whole-number part of the text.
					s2.Append('\u2008', decimalSeparator.Length);  // Pad with a punctuation space (width of a .)
					s2.Append('\u2002', precision);  // and an en space for each decimal that we're not showing.
				}
				else
					s2.Append(text);
			}
			else if (double.IsNaN(n) || double.IsInfinity(n))
				s2.Append(text);
			else if (formatSpecifier == 'G' && n == 0 || Math.Abs(n) > 0.51 * Math.Pow(10, -precision))
			{
				// Print out the number
				s2.Append(text);

				// and right-pad it, with a decimal-sized space if there's no decimal, and digit-sized spaces if there's not enough digits after the decimal.
				var decimalPos = text.IndexOf(decimalSeparator);
				if (decimalPos == -1)
					s2.Append("\u2008\u2002\u2002\u2002\u2002");  // Add padding spaces equal in width to .0000
				else
				{
					var digitsAfterDecimal = text.Length - decimalPos - 1;

					if (digitsAfterDecimal < 4)
						s2.Append('\u2002', 4 - digitsAfterDecimal);  // Add padding en spaces (so these non-exponential form numbers will line up nicely with exponential form numbers).
				}
			}
			else if (formatSpecifier == 'G' && Math.Abs(n) > 0.0001 && Math.Abs(n) < 100000)
			{
				int magnitude = (int)Math.Floor(Math.Log10(Math.Abs(n)));
				if (-magnitude < precision)
					return text;

				// Print an optional '-', then a "0.", then some '0's then [precision] non-zero digits.
				if (n < 0)
					s2.Append(CultureInfo.CurrentCulture.NumberFormat.NegativeSign);
				
				s2.Append("0.");
				s2.Append('0', -magnitude - 1);
				s2.AppendFormat("{0:N0}", Math.Abs(n) * Math.Pow(10, -magnitude + precision - 1));
			}
			else
			{
				// Convert into scientific notation: 1.23 x 10(superscript digits)
				int magnitude = n == 0 ? 0 : (int)Math.Floor(Math.Log10(Math.Abs(n)));
				char[] digits = "\u2070\u00B9\u00B2\u00B3\u2074\u2075\u2076\u2077\u2078\u2079".ToCharArray();  // superscript 0123456789

				s2.AppendFormat("{0:F" + (precision - 1).ToString() + "}", n * Math.Pow(10, -magnitude));
				s2.Append('\u00D7'); // multiply symbol
				s2.Append("10");
				if (magnitude < 0)
					s2.Append('\u207B');  // superscript -

				if (magnitude == 0)
					s2.Append('\u2070');  // superscript 0
				else
					for (int i = (int)Math.Log10(Math.Abs(magnitude)); i >= 0; i--)
						s2.Append(digits[(int)((Math.Abs(magnitude) / Math.Pow(10, i)) % 10)]);
			}

			return s2.ToString();
		}

		public Color GetBarColor(Color? rowBackground = null, Color? barNone = null)
		{
			if (BarColor != Color.Empty)
				return BarColor;

			if (Color != Color.Empty)
				return ZReportColors.Darken(ZReportColors.AddDark(Color, rowBackground));

			if (barNone != null && barNone != Color.Empty)
				return ZReportColors.AddDark((Color)barNone, rowBackground);
			
			return ZReportColors.Darken(Color.White);
		}

		public override string ToString()
		{
			return Text;
		}
	}

	public class ZRow: List<ZCell>
	{
		public string CssClass { get; set; }
		/// <summary>Optional background color.</summary>
		public Color Color { get; set; }

		/// <summary>Just like Add(), but returns the added cell.</summary> 
		public ZCell AddCell(ZCell cell)
		{
			Add(cell);
			return cell;
		}

		/// <summary>If there is exactly one chart in this row (because exactly one cell has its ChartType set, or because all the ChartCell 
		/// values specified in this row point to just one cell, return the index of the cell whose value is used for the bar.</summary>
		public ZCell OneBarCell()
		{
			ZCell result = null;
			bool foundOneBar = false;
			for (int col = 0; col < this.Count; col++)
			{
				var cell = this[col];
				if (foundOneBar)
				{
					if ((cell.ChartCell != null && cell.ChartCell != result) || (cell.ChartType != ChartType.None && result != cell))
						return null;
				}
				else  // No Bar found yet.
				{
					if (cell.ChartCell != null)
					{
						foundOneBar = true;
						result = cell.ChartCell;
					}
					else if (cell.ChartType != ChartType.None)
					{
						foundOneBar = true;
						result = cell;
					}
				}
			}
			return foundOneBar ? result : null;
		}
	}

	public class ZReportColors
	{
		public Color TitleFontColor { get; set; }
		public Color TitleBackColor { get; set; }
		public Color TextColor { get; set; }
		public Color BackgroundColor { get; set; }
		/// <summary>Colour used for background of odd-numbered rows.</summary>
		public Color OddColor { get; set; }
		public Color BarColor { get; set; }
		/// <summary>Colour used for bars if no cell colours are set.</summary>
		public Color BarNone { get; set; }

		public ZReportColors()
		{
			TitleFontColor = Color.White;
			TitleBackColor = Color.Gray;
			TextColor = Color.Black;
			BackgroundColor = Color.White;
			OddColor = Color.FromArgb(0xEE, 0xFF, 0xFF);
			BarColor = Color.Empty;
			BarNone = Color.FromArgb(255, 192, 160);
		}

		public Color GetBackColor(ZRow row, bool odd, Color color = default)
		{
			return color != Color.Empty ? color : 
				row.Color != Color.Empty ? row.Color : 
				odd ? OddColor : BackgroundColor;
		}

		public static Color Darken(Color color)
		{
			return Color.FromArgb(color.R * 9 / 11, color.G * 9 / 11, color.B * 9 / 11);
		}

		public static Color AddDark(Color color1, Color? color2)
		{
			return color2 == null || color2 == Color.Empty ?
				color1 :
				Color.FromArgb(Math.Max(color1.R + ((Color)color2).R - 255, 0), Math.Max(color1.G + ((Color)color2).G - 255, 0), Math.Max(color1.B + ((Color)color2).B - 255, 0));
		}
	}

	public abstract class ZoomReportBase
	{
		/// <summary>Title for the whole report.</summary>
		public string Title { get; set; }
		/// <summary>If the report title contains text which links to another report, put the URL of that report here.</summary>
		public virtual string TitleHyper { get; set; }
		internal ZReportColors colors;
		public ZReportColors Colors
		{
			get
			{
				if (colors == null)
					colors = new ZReportColors();
				return colors;
			}
			set { colors = value;  } 
		}

		/// <summary>Export to character-separated value.</summary>
		public abstract string ToCsv(char separator);
		/// <summary>Export to an HTML table.</summary>
		public abstract string ToHtml();
		/// <summary>Export to an HTML SVG element.</summary>
		/// <param name="pure">True if this is standalone SVG that will not be embedded in an HTML file, and therefore cannot contain xlinks or javascript text resizing.</param>
		public abstract string ToSvg(bool pure = false);
		public abstract void ToSvg(StringBuilder sb, double? aspectRatio = null, bool pure = false);
		public abstract IEnumerable<Color> BarCellColors();
	}

	public static class ZoomUtility
	{
		public static bool Valid<T>(this IList<T> list, int i)
		{
			return 0 <= i && i < list.Count;
		}

		public static T Force<T>(this IList<T> list, int i) where T: new()
		{
			if (list.Valid(i))
				return list[i];

			while (list.Count < i)
				list.Add(new T());

			T item = new T();
			list.Add(item);
			return item;
		}
	}

	public delegate void CalculateFill(ZRow row, int col, double chartMin, double chartMax, ref double? fill);  // Callback to custom-set bar cell filledness.

	public class ZoomReport: ZoomReportBase
	{
		public List<ZColumn> Columns { get; private set; }
		public List<ZRow> Rows { get; set; }
		public string Description { get; set; }
		public string HtmlDescription { get; set; }
		public ZNumberStyle NumberStyle { get; set; }
		public string CssClass { get; set; }
		/// <summary>If true, scale bar charts in each column separately.</summary>
		public bool MaxChartByColumn { get; set; }
		/// <summary>If true, scale bar charts in each column separately.</summary>
		public bool MaxChartByRow { get; set; }
		/// <summary>For scaling bar charts against.</summary>
		public double? MaxChart { get; set; }
		public ZoomReport Owner { get; set; }
		/// <summary>If true, it is OK to try to render this in two (or more) "columns" a la Word, thus doubling the row height and halving the width of each column.</summary>
		public bool MultiColumnOK { get; set; }
		/// <summary>True if HTML tables should show bar charts.</summary>
		public bool Bars { get; set; }
		/// <summary>Add groups of columns that should be rendered with the same width here.</summary>
		public List<List<ZColumn>> SameWidths { get; private set; }
		/// <summary>Callback to custom-set bar cell filledness.</summary>
		public CalculateFill CalculateFill { get; set; }

		//public delegate void PaintBar (ZoomReport report, int row, int col, TCanvas canvas, Rect rect, Color Color, Color BarColor);  // callback to custom-paint bar background
		//public PaintBar PaintBar { get; set; }

		public ZoomReport(string title, string headings = "", string alignments = "", string groupHeadings = "")
		{
			Columns = new List<ZColumn>();
			Rows = new List<ZRow>();
			SameWidths = new List<List<ZColumn>>();

			Title = title;

			AddColumns(headings, alignments, groupHeadings);

			Bars = true;
		}

		public void AddColumns(string headings, string alignments = "", string groupHeadings = "")
		{
			int firstAdded = Columns.Count();

			if (!string.IsNullOrEmpty(headings))
				foreach (string heading in headings.Split(','))
					Columns.Add(new ZColumn(heading));
			
			if (!string.IsNullOrEmpty(alignments))
			{
				string[] alignmentList = alignments.Split(',');
				for (int i = 0; firstAdded + i < Columns.Count && i < alignmentList.Length; i++)
					Columns[firstAdded + i].Alignment = (ZAlignment)Enum.Parse(typeof(ZAlignment), alignmentList[i], true);
			}

			if (!string.IsNullOrEmpty(groupHeadings))
			{
				string[] groupList = groupHeadings.Split(',');
				for (int i = 0; firstAdded + i < Columns.Count && i < groupList.Length; i++)
					Columns[firstAdded + i].GroupHeading = groupList[i];
			}
		}

		///<summary>Just like Add(), but returns the added ZColumn.</summary> 
		public ZColumn AddColumn(ZColumn col)
		{
			Columns.Add(col);
			return col;
		}

		///<summary>Just like Add(), but returns the added ZRow.</summary> 
		public ZRow AddRow(ZRow row)
		{
			Rows.Add(row);
			return row;
		}

		static void AppendStrings(StringBuilder builder, params string[] strings)
		{
			foreach (string s in strings)
				builder.Append(s);
		}

		///<summary>Get a cell by its row number and its column title text.</summary>
		public ZCell Cell(ZRow row, string columnText)
		{
			int i = Columns.FindIndex(x => x.Text == columnText);
			return row.Valid(i) ? row[i] : null;
		}

		public bool ColumnEmpty(int i)
		{
			foreach(ZRow row in Rows)
				if (i < row.Count && !row[i].Empty())
					return false;

			return true;
		}

		public bool ColumnZeroOrNaN(int i)
		{
			if (Columns[i].Arrows.Any())
				return false;

			foreach(ZRow row in Rows)
				if (i < row.Count && !row[i].EmptyOrNaN() && row[i].Number != 0)
					return false;

			return true;
		}

		public void RemoveColumn(int i)
		{
			if (Columns.Valid(i))
			{
				foreach (var sw in SameWidths)
					sw.Remove(Columns[i]);

				Columns.RemoveAt(i);
			}

			foreach(ZRow row in Rows)
				if (row.Valid(i))
					row.RemoveAt(i);
		}

		/// <summary>Remove columns which contain no arrows and whose data is all empty, NaN or 0.</summary>
		public void RemoveZeroColumns()
		{
			for (int i = Columns.Count - 1; i >= 0; i--)
				if (ColumnZeroOrNaN(i))
					RemoveColumn(i);
		}

		/// <summary>Return the largest total width of arrows at any single point in this column.</summary>
		double MaxArrowWidth(int col)
		{
			if (!Columns[col].Arrows.Any(a => a.From.Any() && a.To.Any()))
				return 0.0;

			double maxWidth = 0.0;
			for (int row = Columns[col].Arrows.Min(a => Math.Min(a.From.Min(x => x.Row), a.To.Min(x => x.Row))); row < Columns[col].Arrows.Max(a => Math.Max(a.From.Max(x => x.Row), a.To.Max(x => x.Row))); row++)
				maxWidth = Math.Max(maxWidth, Columns[col].Arrows.Where(a => (a.From.Any(x => x.Row <= row) && a.To.Any(x => x.Row >= row)) || (a.From.Any(x => x.Row >= row) && a.To.Any(x => x.Row <= row))).Sum(a => a.MaxWidth()));

			return maxWidth;
		}

		/// <summary>For each column, calculate a pixel width, and find the min and max values. Also calculate header height.</summary>
		/// <param name="widths">Width in pixels for each colunm.</param>
		/// <param name="mins">Lowest value in each column (in either the cells themselves or in Data points in those cells).</param>
		/// <param name="maxs">Highest value in each column (in either the cells themselves or in Data points in those cells).</param>
		/// <param name="maxPoints">Largest number of Data points in any chart in any cell.</param>
		/// <param name="maxLines">Number of lines of text used by the column heading that wraps onto the most lines, plus 1 if that column has a GroupHeading.</param>
		/// <returns>Vertical height the title will consume, and vertical height the column headers will consume (including the blank pixel below the headers).</returns>
		(int, int) Metrics(List<float> widths, List<double> mins, List<double> maxs, out int maxPoints)
		{
			maxPoints = 0;
			float spaceWidth = TextWidth(" ");
			const string egFormat = ".\u00D710\u207B\u2079";

			double headingsHeight = RowHeight;
			bool hasGroupHeadings = HasGroupHeadings();
			bool anyRotate = Columns.Exists(col => col.Rotate);

			for (int col = 0; col < Columns.Count; col++)
			{
				var column = Columns[col];
				var maxArrowWidth = MaxArrowWidth(col);
				float widest = maxArrowWidth == 0 ? 1 : (float)maxArrowWidth * 3;
				float total = widest;
				int count = 1;
				double min = 0.0;
				double max = 0.0;
				string numberFormat = "";
				bool hasNumber = false;

				for (int row = 0; row < Rows.Count; row++)
				{
					if (col < Rows[row].Count)
					{
						var cell = Rows[row][col];
						if (cell.Data != null && cell.Data.Any() && cell.ChartType != ChartType.None && cell.ChartType != ChartType.Bar)  // For charts including rug, box plot, etc., let's get the min/max of the data points within the cell, instead of just the cell's display value.
						{
							hasNumber = true;
							min = Math.Min(min, cell.Data.Min());
							max = Math.Max(max, cell.Data.Max());

							if (Math.Abs(min) > max)
							{
								max = min;
							}
							maxPoints = Math.Max(maxPoints, cell.Data.Count);
						}
						else if (cell.Tag is List<ChartPoint> points && points.Any())
						{

							if (min == 0.0)
								min = double.MaxValue; // For most data types we want an all-positive data series to have a min of 0, not its actual series minimum. But for chart points, where X is a date/time, that starts the chart at 1900, which is bad. So one-off setting the min to MaxValue means we'll end with a min of the earliest time in the series. 
							hasNumber = true;
							min = Math.Min(min, points.Min(p => p.X.Ticks));
							max = Math.Max(max, points.Max(p => p.X.Ticks));
							maxPoints = Math.Max(maxPoints, points.Count);
						}
						else if (cell.Number is double n && !double.IsNaN(n) && !double.IsInfinity(n) && !double.IsNaN(n))
						{
							hasNumber = true;
							min = Math.Min(min, n);
							max = Math.Max(max, n);
						}
						else if (!string.IsNullOrEmpty(cell.Text))
						{
							float width = TextWidth(cell.Text);
							total += width;
							count++;
							widest = Math.Max(widest, width);
						}

						if (hasNumber && !string.IsNullOrEmpty(cell.NumberFormat))
							numberFormat = cell.NumberFormat;
					}
				}

				float thisColumnWidth;
				if (hasNumber)
				{
					double furthestFromZero = max > -min ? max : min;
					string stringMax = string.IsNullOrEmpty(numberFormat) ? furthestFromZero.ToString() : furthestFromZero.ToString(numberFormat);
					if (numberFormat.Length >= 2 && (numberFormat[0] == 'E' || numberFormat[0] == 'G'))
						stringMax = new string('9', int.Parse(numberFormat.Substring(1))) + egFormat;
					else if (double.IsInfinity(max) && !string.IsNullOrEmpty(numberFormat) && numberFormat.Length > 1 && numberFormat[0] == 'P' && stringMax.Length < 4)  // Deal with "infinity percent" case.
						stringMax = 9.99.ToString(numberFormat);

					thisColumnWidth = TextWidth(stringMax);
				}
				else
				{
					thisColumnWidth = widest > 2 * total / count ?  // Are there are a few pathologically wide fields?
										total / count * 1.4f :        // Just use the average, plus some padding.
										widest;
				}

				mins.Add(min);
				maxs.Add(max);

				// Height / width of column title:
				List<int> spaces = null;
				List<float> wordWidths = null;
				double thisTitleHeight = RowHeight;

				if (column.Rotate)
				{
					thisColumnWidth = Math.Max(thisColumnWidth, RowHeight);

					if (hasGroupHeadings)
						thisTitleHeight = TextWidth(column.Text) + (string.IsNullOrWhiteSpace(column.GroupHeading) ? 0 : RowHeight + 1);  // Text rotated 90 degrees.
					else
						thisTitleHeight = (TextWidth(column.Text) + RowHeight) / Math.Sqrt(2);  // Text rotated 45 degrees.
				}
				else
				{
					spaces = Spaces(column.Text);
					wordWidths = WordWidths(column.Text, spaces);
					thisColumnWidth = Math.Max(thisColumnWidth, wordWidths.Any() ? wordWidths.Max() : 0);

					var titleLineWidth = wordWidths.Sum() + (spaces.Count - 1) * spaceWidth;
					if (thisColumnWidth < titleLineWidth)
						thisTitleHeight = Math.Min(wordWidths.Count, (int)Math.Ceiling(titleLineWidth / thisColumnWidth)) * TextHeight + TextHeight / 3.0 + (string.IsNullOrEmpty(column.GroupHeading) ? 0 : RowHeight + 1);
				}

				headingsHeight = Math.Max(headingsHeight, thisTitleHeight);
				widths.Add(thisColumnWidth);
			}

			foreach (var sw in SameWidths)
			{
				float widestInGroup = 0;
				foreach (var c in sw)
					widestInGroup = Math.Max(widestInGroup, widths[Columns.IndexOf(c)]);

				foreach (var c in sw)
					widths[Columns.IndexOf(c)] = widestInGroup;
			}

			// Calculate vertical height the title and column headers will consume.
			int titleHeight = RowHeight * 2 + 2;  // The + 2 is for the one-pixel spaces above and below the title rectangle. This is a placeholder title height, and may be changed by SvgBegin().
			if (!Columns.Exists(col => !string.IsNullOrWhiteSpace(col.Text)))
				return (titleHeight, 0);

			return (titleHeight, (int)Math.Round(headingsHeight) + 1);  // The + 1 is for the one-pixel space between the column headings and the first data row of the table.
		}

		public override IEnumerable<Color> BarCellColors()
		{
			return Rows.SelectMany(row => row.Where(cell => cell.ChartCell != null)
			                   .Select(cell => cell.GetBarColor())).Distinct();
		}

		SvgDocument document;
		bool _pure;  // true if this is to be rendered to a bitmap by SvgDocument.Draw(), as opposed to saving out for rendering by a web browser.

		/// <summary>Render a report to bitmap. If report has different aspect ratio to width/height, then returned bitmap will be either narrower than width, or shorter than height.</summary>
		/// <param name="width">Maximum width to render.</param>
		/// <param name="height">Maximum height to render.</param>
		/// <param name="force">Force a from-scratch render even if one has already been done.</param>
		/// <returns>The bitmap.</returns>
		public Bitmap ToBitmap(int width, int height, bool force = false)
		{
			if (force || document == null)
				document = SvgDocument.FromSvg<SvgDocument>(ToSvg(1.0 * width / height));

			double aspectRatio = document.ViewBox.Width / document.ViewBox.Height;

			if (aspectRatio > 1.0 * width / height)
			{
				document.Width = new SvgUnit(SvgUnitType.Pixel, width);
				document.Height = new SvgUnit(SvgUnitType.Pixel, (int)(width / aspectRatio));
			}
			else
			{
				document.Width = new SvgUnit(SvgUnitType.Pixel, (int)(height * aspectRatio));
				document.Height = new SvgUnit(SvgUnitType.Pixel, height);
			}
			return document.Draw();
		}

		/// <summary>Render a report to bitmap.</summary>
		public Bitmap ToBitmap(float scale)
		{
			if (document == null)
				document = SvgDocument.FromSvg<SvgDocument>(ToSvg(true));

			document.Width = new SvgUnit(SvgUnitType.Pixel, document.ViewBox.Width * scale);
			document.Height = new SvgUnit(SvgUnitType.Pixel, document.ViewBox.Height * scale);

			return document.Draw();
		}

		public override string ToCsv(char separator)
		{
			StringBuilder s = new StringBuilder();

			if (!string.IsNullOrEmpty(Title))
				AppendStrings(s, Title, "\n");

			bool hasgroupheadings = false;
			foreach (ZColumn col in Columns)
				hasgroupheadings |= !string.IsNullOrEmpty(col.GroupHeading);

			if (hasgroupheadings)
			{
				foreach (ZColumn col in Columns)
				{
					s.Append(col.GroupHeading);
					s.Append(separator);
				}
				s.Append('\n');
			}

			foreach (ZColumn col in Columns)
			{
				s.Append(col.Text);
				s.Append(separator);
			}
			s.Append('\n');

			for (int i = 0; i < Rows.Count; i++)
			{
				for (int j = 0; j < Rows[i].Count; j++)
				{
					ZCell cell = Rows[i][j];
					if (string.IsNullOrEmpty(cell.Text))
					{
						// If the cell is otherwise empty, and it contains the start of an arrow that has one From and one To, 
						var arrows = Columns[j].Arrows.FindAll(a => a.From.Count == 1 && a.To.Count == 1 && a.From[0].Row == i);
						if (arrows.Count == 1)
							s.Append(arrows[0].To[0].Row - arrows[0].From[0].Row);  // output a number that shows how many rows the arrow goes up/down.
					}
					s.Append(cell.Text);
					s.Append(separator);
				}
				s.Append('\n');
			}

			return s.ToString();
		}

		private string ColorToTColor(Color color)
		{
			if(color.IsEmpty)
            {
				return "clNone";
            }
			var r = color.R.ToString("X2");
			var g = color.G.ToString("X2");
			var b = color.B.ToString("X2");

			return "$02" + b + g + r;
		}

		public string ToTBoard()
		{
			string separator = ",";
			StringBuilder s = new StringBuilder();

			s.Append("DISPLAYREPORTS");
			s.Append(separator);
			s.Append(ColorToTColor(Colors.TitleFontColor));
			s.Append(separator);
			s.Append(ColorToTColor(Colors.TitleBackColor));
			s.Append(separator);
			s.Append(ColorToTColor(Colors.BackgroundColor));
			s.Append(separator);
			s.Append(ColorToTColor(Colors.OddColor));
			s.Append(separator);
			s.Append("\"" + (Title ?? "") + "\"" );

			s.Append(separator);
			s.Append("\"");

			foreach (ZColumn header in Columns)
			{
				s.Append("\"\"");
				string heading = header.GroupHeading ?? "";
				s.Append(heading);
				s.Append(heading == "" ? "" : " ");
				s.Append(header.Text);
				s.Append("\"\"");
				if (Columns.IndexOf(header) != Columns.Count() - 1)
				{ 
					s.Append(separator);
				}
			}
			s.Append("\"");
			s.Append(separator);
			s.Append("\"");

			List<string> groupHeadings = new List<string>();

			foreach (ZColumn header in Columns)
			{
				string heading = header.GroupHeading ?? "";
				if(heading != "")
                {
					if(!groupHeadings.Contains(heading))
                    {
						groupHeadings.Add(heading);
                    }
                }
			}


			foreach (ZColumn header in Columns)
			{
				string align = header.Alignment.ToString().ToLower();

				if(align != "left" && align != "right" && align != "center")
                {
					align = "right";
                }

				s.Append(align);
				if (Columns.IndexOf(header) != Columns.Count() - 1)
				{
					s.Append(separator);
				}
			}

			s.Append("\"");

			for (int i = 0; i < Rows.Count; i++)
			{
				for (int j = 0; j < Rows[i].Count; j++)
				{
					ZCell cell = Rows[i][j];
					string text = cell.Text;
					if (string.IsNullOrEmpty(cell.Text) && Columns.Count > j)
					{
						// If the cell is otherwise empty, and it contains the start of an arrow that has one From and one To, 
						var arrows = Columns[j].Arrows.FindAll(a => a.From.Count == 1 && a.To.Count == 1 && a.From[0].Row == i);
						if (arrows.Count == 1)
							text = (arrows[0].To[0].Row - arrows[0].From[0].Row).ToString();  // output a number that shows how many rows the arrow goes up/down.
					}
					s.Append(separator);
					s.Append("\"");
					s.Append(cell.Number != null && text.Contains(",") ? cell.Number.ToString() : text);
					s.Append("\"");
					s.Append(separator);
					s.Append(ColorToTColor(cell.Color));
					s.Append(separator);
					s.Append(j);
				}
				s.Append(separator);
				s.Append("EOREOR");
			}

			s.Append(separator);
			s.Append("EOTEOT");
			s.Append(separator);
			s.Append("EOSEOS");

			return s.ToString();
		}

		/// <summary>Writes an HTML fragment -- it does not include <head> or <body> tags etc.</summary>
		public override string ToHtml()
		{
			List<ZColumn> columns = Columns;
			bool hasgroupheadings = false;
			foreach (ZColumn col in Columns)
				hasgroupheadings |= !string.IsNullOrEmpty(col.GroupHeading);

			StringBuilder s = new StringBuilder();

			s.Append("\n<table align=\"center\"");
			if (!string.IsNullOrEmpty(CssClass))
				AppendStrings(s, " class=\"", CssClass, "\"");

			s.Append(">\n");
			s.Append("  <thead>\n");

			// Title row
			if (!string.IsNullOrEmpty(Title))
				AppendStrings(s, "    <tr bgcolor=\"", ColorTranslator.ToHtml(Colors.TitleBackColor), "\">\n",
				              "       <th colspan=\"", columns.Count.ToString(CultureInfo.InvariantCulture), "\"><H2>", WebUtility.HtmlEncode(Title), "</H2></th>\n",
				              "    </tr>\n");

			// Group Headings row
			if (hasgroupheadings)
			{
				AppendStrings(s, "    <tr bgcolor=\"", ColorTranslator.ToHtml(Colors.TitleBackColor), "\">\n");

				int start = 0;
				while (start < columns.Count)
				{
					int end = start;
					while (end < columns.Count - 1 && columns[start].GroupHeading == columns[end + 1].GroupHeading)
						end++;

					if (start == end)
						AppendStrings(s, "      <th align=\"center\">", 
						              WebUtility.HtmlEncode(columns[start].GroupHeading), "</th>\n");
					else
						AppendStrings(s, "      <th align=\"center\" colspan=\"" + (end - start + 1).ToString(CultureInfo.InvariantCulture) + "\">",
						              WebUtility.HtmlEncode(columns[start].GroupHeading), "</th>\n");

					start = end + 1;
				}

				s.Append("    </tr>\n");
			}

			// Headings row
			AppendStrings(s, "    <tr bgcolor=\"", ColorTranslator.ToHtml(Colors.TitleBackColor), "\">\n");

			foreach (ZColumn col in Columns)
				if (string.IsNullOrEmpty(col.Hyper))
					AppendStrings(s, "      <th", col.Alignment.ToHtml(), ">", WebUtility.HtmlEncode(col.Text), "</th>\n");
				else
					AppendStrings(s, "      <th", col.Alignment.ToHtml(), "><a href=\"", col.Hyper, "\">", WebUtility.HtmlEncode(col.Text), "</a></th>\n");

			s.Append("    </tr>\n");
			s.Append("  </thead>\n");
			s.Append("  <tbody>\n");

			double max = 0;
			if (Bars)
//				max = this.Rows.SelectMany(row => row.Where(cell => cell.Number != null && (cell.Bar || cell.BarCell != null))
//				                           .Select(cell => Math.Abs((double)cell.Number))).DefaultIfEmpty(0).Max();
				foreach (var row in Rows)
					foreach (var cell in row)
						if (cell.Number != null && (cell.ChartType != ChartType.None || cell.ChartCell != null))
							max = Math.Max(max, Math.Abs((double)cell.Number));
			if (max == 0)
				max = 1;

			bool odd = true;

			foreach (ZRow row in Rows)
			{
		        // Write the <tr> tag that begins the row.
				AppendStrings(s, "    <tr style=\"background-color: ", ColorTranslator.ToHtml(Colors.GetBackColor(row, odd)), "\"");

				if (!string.IsNullOrEmpty(row.CssClass))
					AppendStrings(s, " class=\"", row.CssClass, "\"");

				s.Append(">\n");

				ZCell oneBarCell = row.OneBarCell();

				for (int col = 0; col < columns.Count && col < row.Count; col++)
				{
					// find colour for the cell
					string cellColor;
					if (row[col].Color == Color.Empty)
						cellColor = "";
					else
						cellColor = " style=\"background-color: " + ColorTranslator.ToHtml(Colors.GetBackColor(row, odd, row[col].Color)) + '"';

					if (Bars && /*!oneBar &&*/ row[col].Number != null && (row[col].ChartType != ChartType.None || row[col].ChartCell == row[col]))
					{
						// find the max value to scale the bar against
//						thisbar = MaxBarByColumn ? FMaxBars[col] :
//								  MaxBarByRow    ? FMaxBars[j] : FMaxBar;
						
						var barColor = row[col].GetBarColor(Colors.GetBackColor(row, odd), Colors.BarNone);

						AppendStrings(s, "      <td class=\"barcontainer\"", cellColor, ">\n");
						s.AppendFormat("        <div class=\"bar{0:X2}{1:X2}{2:X2}\" style=\"width: {3}%\" />\n", 
						               barColor.R, barColor.G, barColor.B, (int)((double)(row[col].Number) * 100 / (double)max));
						AppendStrings(s, "        <div", columns[col].Alignment.ToHtml(), " class=\"bartext\">");
					}
					else
					{
						AppendStrings(s, "      <td", columns[col].Alignment.ToHtml(), cellColor);
						if (!string.IsNullOrEmpty(row[col].CssClass))
							AppendStrings(s, " class=\"", row[col].CssClass, "\"");
						s.Append(">");
					}

					string text = row[col].Html ?? WebUtility.HtmlEncode(row[col].Text);

					if (string.IsNullOrEmpty(row[col].Hyper))
						s.Append(text);
					else
						AppendStrings(s, "<a href=\"", row[col].Hyper, "\">", text, "</a>");

					if (Bars && /*!oneBar &&*/ row[col].Number != null && (row[col].ChartType != ChartType.None || row[col].ChartCell == row[col]))
						s.Append("</div>");

					s.Append("</td>\n");
				}
				s.Append("    </tr>\n");

				odd = !odd;
			}

			s.Append("</tbody></table>\n");

			if (!string.IsNullOrEmpty(HtmlDescription))
				AppendStrings(s, "<p>", HtmlDescription, "</p>\n");
			else if (!string.IsNullOrEmpty(Description))
				AppendStrings(s, "<p>", Description.Replace("\n", "<br/>\n"), "</p>\n");

			s.Append("<br /><br />\n\n");
			return s.ToString();
		}

		public PrintDocument ToPrint()
		{
			var pd = new PrintDocument();
			pd.PrintPage += new PrintPageEventHandler(this.PrintPage);
			pd.OriginAtMargins = true;
			return pd;
		}

		private void PrintPage(object sender, PrintPageEventArgs ev)
		{
			var bounds = ev.MarginBounds;
			var image = ToBitmap((int)(bounds.Width * ev.PageSettings.PrinterResolution.X / 100), (int)(bounds.Height * ev.PageSettings.PrinterResolution.Y / 100));  // Page dimensions are in 1/100ths of an inch.
			float imageRatio = 1.0F * image.Width / image.Height;
			float pageRatio = 1.0F * bounds.Width / bounds.Height;
			if (imageRatio > pageRatio)  // Report is wider than page: leave white space at bottom of page.
				ev.Graphics.DrawImage(image, 0, 0, bounds.Width, bounds.Width / imageRatio);
			else  // Report is taller than page: leave space at left and right.
			{
				float newWidth = bounds.Height * imageRatio;
				ev.Graphics.DrawImage(image, (bounds.Width - newWidth) / 2, 0, newWidth, bounds.Height);
			}
		}

		// Write a <rect> tag, and a <text> tag on top of it.
		void SvgRectText(StringBuilder s, int indent, double x, double y, double width, double height, Color fontColor, Color backColor, ZAlignment alignment, string text)
		{
			SvgRect(s, indent, x, y, width, height, backColor);
			SvgText(s, indent, (int)x, (int)y, (int)width, (int)(height * 0.75), fontColor, alignment, text, null, null);
		}

		void SvgRect(StringBuilder s, int indent, double x, double y, double width, double height, Color fill, Color outline = default)
		{
			if ((fill == default && outline == default) || width == 0 || height == 0)
				return;

			s.Append('\t', indent);

			if (width > Right - x - 1)  // If we're at the extreme right edge of the report,
				width = Right - x - 1;  // tuck in so we don't draw off the edge.

			if (outline == default)
			{
				int len = s.Length;
				if (width > 0)
					s.AppendFormat("<rect x=\"{0:F1}\" y=\"{1:F0}\" width=\"{2:F1}\" height=\"{3:F0}\" style=\"", x, y, width, height);
				else
					s.AppendFormat("<rect x=\"{0:F1}\" y=\"{1:F0}\" width=\"{2:F1}\" height=\"{3:F0}\" style=\"", x + width, y, -width, height);

				if (fill.A < 255)
					s.AppendFormat("fill-opacity:{0};", fill.A / 256.0);

				s.Replace(".0", "", len, s.Length - len);
				s.Append("fill:");
				s.Append(ColorTranslator.ToHtml(fill));
			}
			else
			{
				int len = s.Length;
				s.AppendFormat("<rect x=\"{0:F1}\" y=\"{1:F1}\" width=\"{2:F1}\" height=\"{3:F0}\" style=\"", x - 0.5, y - 0.5, width + 1, height + 1);
				s.Replace(".0", "", len, s.Length - len);

				if (fill == default)
					s.Append("fill-opacity:0");
				else
				{
					s.Append("fill:");
					s.Append(ColorTranslator.ToHtml(fill));
				}

				s.Append(";stroke:");
				s.Append(ColorTranslator.ToHtml(outline));
			}

			s.Append("\" />\n");
		}

		/// <summary>Like SvgRect() but with 2 decimal places of precision for x,y,width,height instead of 1.</summary>
		void SvgRect2(StringBuilder s, int indent, double x, double y, double width, double height, Color fillColor)
		{
			s.Append('\t', indent);
			s.AppendFormat("<rect x=\"{0:F2}\" y=\"{1:F2}\" width=\"{2:F2}\" height=\"{3:F2}\" style=\"fill:", x, y, width, height);
			s.Append(ColorTranslator.ToHtml(fillColor));
			s.Append("\" />\n");
		}

		void SvgBeginText(StringBuilder s, int indent, int x, int y, int width, double fontSize, Color fontColor, ZAlignment alignment, string cssClass, string hyper, bool fillWidth)
		{
			s.Append('\t', indent);
			if (!string.IsNullOrEmpty(hyper))
				AppendStrings(s, "<a href=\"", hyper, "\">");

			s.Append("<text ");
			if (!string.IsNullOrEmpty(cssClass))  // cssClass is so that cells can be given a CSS class that can e.g. be used later by JavaScript.
				AppendStrings(s, "class=\"", cssClass, "\" ");

			if (!string.IsNullOrEmpty(hyper))
				s.Append("text-decoration=\"underline\" ");

			if (fillWidth)
				s.AppendFormat("textLength=\"{0}\" lengthAdjust=\"spacing\" ", width);

			switch (alignment)
			{
				case ZAlignment.Left: 
					s.AppendFormat("x=\"{0}\"", x + 1);
				break;
				case ZAlignment.Center:
					s.AppendFormat("text-anchor=\"middle\" x=\"{0}\"", x + width / 2);
				break;
			default: // i.e. Right, Float, Integer
					s.AppendFormat("text-anchor=\"end\" x=\"{0}\"", x + width - 1);
				break;
			}
			s.AppendFormat(" y=\"{0}\" width=\"{1}\" font-size=\"{2:G2}\"", y, width, fontSize);

			if (!string.IsNullOrEmpty(hyper) || fontColor != Color.Black)
			{
				s.Append(" fill=\"");
				s.Append(!string.IsNullOrEmpty(hyper) && fontColor == Color.Black ? "Navy" : ColorTranslator.ToHtml(fontColor));
				s.Append('\"');
			}

			s.Append('>');
		}

		void SvgEndText(StringBuilder s, string hyper = null)
		{
			s.Append("</text>");

			if (!string.IsNullOrEmpty(hyper))
				s.Append("</a>");

			s.Append('\n');
		}

		void SvgText(StringBuilder s, int indent, int x, int y, int width, int fontSize, Color fontColor, ZAlignment alignment, string text, string cssClass = null, string hyper = null, bool fillWidth = false, bool htmlEncode = true)
		{
			if (string.IsNullOrEmpty(text) || fontSize <= 0)
				return;

			float textWidth = _pure ? Math.Max(width, TextWidth(text, fontSize)) : width;

			SvgBeginText(s, indent, x, y + fontSize, width, width / textWidth * fontSize * (_pure ? 0.9 : 1), fontColor, alignment, cssClass, _pure ? null : hyper, fillWidth);
			s.Append(htmlEncode ? WebUtility.HtmlEncode(text) : text);
			SvgEndText(s, _pure ? null : hyper);
		}

		/// <summary>Formats the value of a cell then calls the other overload.</summary>
		void SvgText(StringBuilder s, int indent, int x, int y, int width, int height, ZColumn column, ZCell cell)
		{
			SvgText(s, indent, x, y, width, height * 3 / 4, cell.TextColor == Color.Empty ? Colors.TextColor : cell.TextColor, column.Alignment, cell.OutputText(OutputFormat.Svg), cell.CssClass, cell.Hyper, column.FillWidth, false);
		}

		/// <summary>Print text, but see if we can make the ultimate font size bigger by breaking the text into multiple lines, instead of just shrinking the text to fit on a single line.</summary>
		void SvgMultilineText(StringBuilder s, int indent, int x, int y, int width, int rectHeight, int textHeight, Color fontColor, ZAlignment alignment, string text, string cssClass = null, string hyper = null)
		{
			var textWidth = TextWidth(text, textHeight);
			var oversizeRatio = textWidth / width;  // Is the text too wide to fit in the cell without shrinking to fit? If so, how much by?
			int baseline = y + rectHeight - textHeight / 3;  // Space up one third of a line to leave room for descenders (gjpqy) and room to "breathe".

			if (oversizeRatio <= 1)  // Our text is not oversized for this width.
				SvgText(s, indent, x, baseline - textHeight, width, textHeight, fontColor, alignment, text, cssClass, hyper, false);  // Just print it on one row.
			else  // Text won't fit on one line without shrinking.
			{
				int linesToUse = (int)Math.Ceiling(oversizeRatio);  // Number of lines we would like to break the text into, to avoid having to shrink the text.
				int linesAvailable = (rectHeight - textHeight / 3) / textHeight;
				float lineHeight = textHeight;

				if (Math.Ceiling(oversizeRatio) > linesAvailable) // The rect we're fitting into is not tall enough to directly fit the number of lines we want to break the text into.
				{
					var shrinkRatio = Math.Sqrt(oversizeRatio);
					int linestoUseByRectHeight = (int)Math.Max(shrinkRatio * rectHeight / textHeight, 1);
					linesToUse = Math.Max(Math.Max(Math.Min((int)shrinkRatio, linestoUseByRectHeight), rectHeight / textHeight), 1);
					lineHeight = Math.Min(textHeight, (float)rectHeight / linesToUse);
				}

				(string[] lines, _) = BreakText(text, linesToUse);
				for (int i = 0; i < lines.Length; i++)
					SvgText(s, indent, x, baseline + (i - lines.Length) * textHeight, width, textHeight, fontColor, alignment, lines[i], cssClass, hyper, false);
			}
		}

		/// <summary>Print text on multiple lines.</summary>
		void SvgMultilineText(StringBuilder s, int indent, int x, int bottom, int width, float textHeight, Color fontColor, ZAlignment alignment, string[] text, string cssClass = null, string hyper = null)
		{
			int baseline = bottom - (int)textHeight / 3;  // Space up one third of a line to leave room for descenders (gjpqy) and room to "breathe".

			for (int i = 0; i < text.Length; i++)
				SvgText(s, indent, x, (int)(baseline + (i - text.Length) * textHeight), width, (int)textHeight, fontColor, alignment, text[i], cssClass, hyper, false);
		}

		/// <summary>Break text (at spaces) into several lines.</summary>
		/// <returns>Array containing text broken into lines, and width of the widest line.</returns>
		(string[], float) BreakText(string text, int lines)
		{
			List<int> spaces = Spaces(text);

			if (spaces.Count == 1)
				return (new string[1] { text }, TextWidth(text));
			else if (spaces.Count - 1 < lines)  // Not enough spaces (or the exact correct number of spaces) -- just break the text at every space.
			{
				var strings = new string[spaces.Count];
				strings[0] = text.Substring(0, spaces[0]);

				for (int i = 1; i < spaces.Count; i++)
					strings[i] = text.Substring(spaces[i - 1] + 1, spaces[i] - spaces[i - 1] - 1);

				return (strings, WordWidths(text, spaces).Max());
			}
			else  // We've got more spaces than we need -- gotta go through and pick which ones we want.
			{
				float spaceWidth = TextWidth(" ");
				List<float> wordWidths = WordWidths(text, spaces);

				var lineBreaks = new List<int>();  // Pointers into the spaces list.
				var lineWidths = new List<float>();

				float targetWidth = (wordWidths.Sum() + spaceWidth * (wordWidths.Count - 1)) / lines;
				int endWord = 0;
				for (int i = 0; i < lines - 1; i++)
				{
					float lineWidth = wordWidths[endWord];
					while (lineWidth < targetWidth &&  // Stop looping if this line is already wider than target. (Going a bit beyond target is OK, as some lines will be longer than average. But going more than one word beyond target is never optimal.)
						endWord < wordWidths.Count - 1)  // Stop looping if we've reached the word _before_ the last word. (This leaves the last word to fill the last line).
					{
						float nextWordWidth = spaceWidth + wordWidths[endWord + 1];
						if (lineWidth + nextWordWidth / 2 > targetWidth)  // Adding the next word would move us further away from targetWidth.
							break;

						endWord++;
						lineWidth += spaceWidth + wordWidths[endWord];
					}

					lineBreaks.Add(endWord);
					lineWidths.Add(lineWidth);

					targetWidth = (wordWidths.Skip(endWord + 1).Sum() + spaceWidth * (wordWidths.Count - endWord - 2)) / (lines - i - 1);
					endWord++;
				}

				lineBreaks.Add(spaces.Count - 1);  // Add dummy line break at end, pointing at final dummy space.
				lineWidths.Add(targetWidth);

				var strings = new string[lines];
				strings[0] = text.Substring(0, spaces[lineBreaks[0]]);
				for (int i = 1; i < strings.Length; i++)
					strings[i] = text.Substring(spaces[lineBreaks[i - 1]] + 1, spaces[lineBreaks[i]] - spaces[lineBreaks[i - 1]] - 1);

				return (strings, lineWidths.Max());
			}
		}

		/// <summary>Return indexes of whitespaces in a string, plus a dummy space at the position just past the end.</summary>
		private static List<int> Spaces(string text)
		{
			var spaces = new List<int>();

			if (string.IsNullOrEmpty(text))
				return spaces;

			char[] textChars = text.ToCharArray();
			int length = textChars.Length;

			for (int n = 0; n < length; n++)
				if (char.IsWhiteSpace(textChars[n]))
					spaces.Add(n);

			spaces.Add(length);  // Add a dummy space at the end.

			return spaces;
		}

		/// <summary>Width of each word in pixels, not including any leading/trailing whitespace.</summary>
		private List<float> WordWidths(string text, List<int> spaces)
		{
			var wordWidths = new List<float>();

			if (string.IsNullOrEmpty(text))
				return wordWidths;

			wordWidths.Add(TextWidth(text.Substring(0, spaces[0])));

			for (int w = 1; w < spaces.Count; w++)
				wordWidths.Add(TextWidth(text.Substring(spaces[w - 1] + 1, spaces[w] - spaces[w - 1] - 1)));

			return wordWidths;
		}

		void SvgCircle(StringBuilder s, int indent, double x, double y, double radius, Color fillColor)
		{
			if (fillColor != Color.Empty)
			{
				s.Append('\t', indent);
				s.AppendFormat("<circle cx=\"{0:F2}\" cy=\"{1:F2}\" r=\"{2:F2}\" style=\"fill:", x, y, radius);
				s.Append(ColorTranslator.ToHtml(fillColor));
				s.Append("\" />\n");
			}
		}

		/// <summary>points x values are scaled; points y values are unscaled and should be in the range 0 .. yMax.</summary>
		void SvgPolygon(StringBuilder s, int indent, List<Tuple<double, double>> points, double rowTop, double rowHeight, double yMax, Color fillColor)
		{
			s.Append('\t', indent);
			s.Append("<polygon points=\"");
			for (int i = 0; i < points.Count; i++)
			{
				double y0 = i == 0 ? double.MinValue : rowTop + rowHeight - Scale(points[i - 1].Item2, rowHeight, 0, yMax);
				double y1 = rowTop + rowHeight - Scale(points[i].Item2, rowHeight, 0, yMax);
				double y2 = i == points.Count - 1 ? double.MaxValue : rowTop + rowHeight - Scale(points[i + 1].Item2, rowHeight, 0, yMax);
				if (Math.Round(y0, 1) != Math.Round(y1, 1) || Math.Round(y1, 1) != Math.Round(y2, 1))  // Only write the point if the y before or the y after is different -- otherwise we have a very expensive way of writing a horizontal line.
					s.AppendFormat("{0:F1},{1:F1} ", points[i].Item1, y1);
			}
			s.Append("\" style=\"fill:");
			s.Append(ColorTranslator.ToHtml(fillColor));
			s.Append("\" />\n");
		}

		enum TopBottomType { Left, Right, Both }; // Does the top of this arrow have an end from the left? An end to the right? One of each? What about the bottom of the arrow?

		float FindArrowLeft(ZArrowEnd end, int col, List<float> widths)
		{
			if (end.Expand)
				while (Columns.Valid(col - 1) &&
						(!Rows[end.Row].Valid(col - 1) || Rows[end.Row][col - 1].Empty()) &&
						!Columns[col - 1].Arrows.Exists(a => a.To.Exists(t => t.Row == end.Row)))
					col--;

			return widths.Take(col).Sum(w => w + 1) + 0.5F;
		}

		float FindArrowRight(ZArrowEnd end, int col, List<float> widths)
		{
			if (end.Expand)
				while (Columns.Valid(col + 1) &&
						(!Rows[end.Row].Valid(col + 1) || Rows[end.Row][col + 1].Empty()) &&
						!Columns[col + 1].Arrows.Exists(a => a.From.Exists(f => f.Row == end.Row)))
					col++;

			return widths.Take(col + 1).Sum(w => w + 1) - 1.5F;
		}

		/// <summary>Draw one complete vertical arrow plus its horizontal ends.</summary>
		void SvgArrow(StringBuilder s, int indent, int col, Arrow arrow, List<float> widths, float top, float rowHeight)
		{
			if (arrow.From.Count == 0 && arrow.To.Count == 0)
				return;

			Color c = arrow.Color == default ? Color.Gray : arrow.Color;
			arrow.From.Sort();
			arrow.To.Sort();

			var topRow = Math.Min(arrow.From.Count == 0 ? 999 : arrow.From.First().Row, arrow.To.Count == 0 ? 999 : arrow.To.First().Row);
			var bottomRow = Math.Max(arrow.From.Count == 0 ? 0 : arrow.From.Last().Row, arrow.To.Count == 0 ? 0 : arrow.To.Last().Row);

			TopBottomType topType;
			TopBottomType bottomType;

			// Determine types of top and bottom of arrow.
			if (!arrow.From.Any())
			{
				topType = TopBottomType.Right;
				bottomType = TopBottomType.Right;
			}
			else if (!arrow.To.Any())
			{
				topType = TopBottomType.Left;
				bottomType = TopBottomType.Left;
			}
			else
			{
				topType = arrow.From.First().Row == arrow.To.First().Row ? TopBottomType.Both : arrow.From.First().Row < arrow.To.First().Row ? TopBottomType.Left : TopBottomType.Right;
				bottomType = arrow.From.Last().Row == arrow.To.Last().Row ? TopBottomType.Both : arrow.From.Last().Row > arrow.To.Last().Row ? TopBottomType.Left : TopBottomType.Right;
			}

			s.Append('\t', indent);

			var halfScaleWidth = arrow.MaxWidth() / rowHeight * 2;  // Scaling factor used to convert an unscaled arrow width into half a scaled arrow width. 
			var halfArrowH = arrow.MaxWidth() * halfScaleWidth;  // Half the width of the vertical part of the arrow, in output SVG units.

			if (arrow.From.Count == 1 && arrow.To.Count == 1 && arrow.From[0].Width == arrow.To[0].Width)  // Arrow has one start, one end, of constant width: draw one spline-curved arrow.
			{
				var leftEnd = arrow.From[0];
				var rightEnd = arrow.To[0];
				float fullArrow = (float)(leftEnd.Width * halfScaleWidth * 2);

				float left = FindArrowLeft(leftEnd, col, widths);
				float width = FindArrowRight(rightEnd, col, widths) - left;

				var mid = (rightEnd.Row - leftEnd.Row) * rowHeight / 2;  // mid is -ve if the arrow is going up.
				s.AppendFormat("<path d=\"M {0:F1},{1:F1} ", left, RowMid(top, leftEnd.Row, rowHeight));

				if (leftEnd == rightEnd)  // Draw the straight arrow line.
					s.AppendFormat("h {0:F1}\" ", width);
				else  // Draw the curved arrow line with two SVG "q" splines, like: <path d=\"M 100,100 h1 q3,0 6,11 q3,11 6,11 h1" stroke="color" stroke-width="4"/>
					s.AppendFormat("h 1 q {0:F1},0 {1:F1},{2:F1} q {0:F1},{2:F1} {1:F1},{2:F1} h 1\" ", width / 4 - 1, width / 2 - 2, mid);

				s.AppendFormat("fill=\"none\" stroke-width=\"{0:F1}\" stroke=\"", fullArrow);
				s.Append(ColorTranslator.ToHtml(c));
				s.Append("\" /> ");

				s.AppendFormat("<path d=\"M {0:F1},{1:F1} ", left + width - fullArrow / 2F, RowMid(top, rightEnd.Row, rowHeight));
				s.AppendFormat("v {0:F1} l {0:F1},{1:F1} l {1:F1},{1:F1} z\" fill=\"", fullArrow, -fullArrow);
			}
			else  // Complex arrow: draw an assembly with multiple starts and/or ends, connected by a vertical "bus" via 90 degree turns.
			{
				float left = widths.Take(col).Sum(w => w + 1) + 0.5F;
				float width = widths[col] + 0.5F;
				float centre = left + width / 2 - (float)halfArrowH / 2;

				if (topType == TopBottomType.Right)
				{
					// Start in the horizontal middle, draw the top left corner arc. 
					s.AppendFormat("<path d=\"M {0:F1},{1:F1} ", centre + halfArrowH, RowMid(top, arrow.To.First().Row, rowHeight) - arrow.To.First().Width * halfScaleWidth);
					s.AppendFormat("a {0:F1},{0:F1} 0 0 0 {1:F1},{0:F1} ", halfArrowH * 2, -halfArrowH * 2);
					if (!arrow.From.Any())
						s.AppendFormat("V {2:F1}\n", RowMid(top, arrow.To.Last().Row, rowHeight) + arrow.To.Last().Width * halfScaleWidth);
				}
				else  // Move cursor to top right end of first arrow.
					s.AppendFormat("<path d=\"M {0:F1},{1:F1} ", centre - halfArrowH, RowMid(top, topRow, rowHeight) - arrow.From.First().Width * halfScaleWidth);

				// Draw left-side "From" ends.
				for (int i = 0; i < arrow.From.Count; i++)
				{
					var end = arrow.From[i];
					var halfArrow = end.Width * halfScaleWidth;

					if (end.Row != topRow)
					{
						s.AppendFormat("V {0:F1} ", RowMid(top, end.Row, rowHeight) - halfArrow * 2);
						s.AppendFormat("a {0:F1},{0:F1} 0 0 1 {1:F1},{0:F1} ", halfArrow, -halfArrow);
					}
					// Paint a left end: left, down, right.
					s.AppendFormat("H {0:F1} ", FindArrowLeft(end, col, widths));
					s.AppendFormat("v {0:F1} ", halfArrow * 2);
					if (end.Row != bottomRow)
					{
						s.AppendFormat("H {0:F1} ", centre - halfArrowH - halfArrow);
						s.AppendFormat("a {0:F1},{0:F1} 0 0 1 {0:F1},{0:F1} ", halfArrow);
					}
				}

				// Handle transition between "From" and "To": draw the very bottom.
				if (bottomType == TopBottomType.Left)
				{
					s.AppendFormat("H {0:F1} ", centre - halfArrowH);
					s.AppendFormat("a {0:F1},{0:F1} 0 0 0 {0:F1},{1:F1} \n", halfArrowH * 2, -halfArrowH * 2);
				}
				else if (bottomType == TopBottomType.Both)
				{
					s.AppendFormat("H {0:F1} ", centre - halfArrowH);
					if (arrow.From.Last().Width != arrow.To.Last().Width)
						s.AppendFormat("c {0:F1},0 {0:F1},{2:F1} {1:F1},{2:F1}\n", halfArrowH, halfArrowH * 2, (arrow.To.Last().Width - arrow.From.Last().Width) / 2);
				}
				else if (bottomType == TopBottomType.Right)
				{
					s.AppendFormat("V {0:F1} ", RowMid(top, arrow.To.Last().Row, rowHeight) + arrow.To.Last().Width * halfScaleWidth - halfArrowH * 2);
					s.AppendFormat("a {0:F1},{0:F1} 0 0 0 {0:F1},{0:F1} \n", halfArrowH * 2);
				}

				// Draw right-side "To" ends.
				for (int i = arrow.To.Count - 1; i >= 0; i--)
				{
					var end = arrow.To[i];
					var halfArrow = end.Width * halfScaleWidth;

					s.Append('\t', indent);
					if (end.Row != bottomRow)
					{
						s.AppendFormat("V {0:F1} ", top + (end.Row + 0.5) * rowHeight + halfArrow * 2);
						s.AppendFormat("a {0:F1},{0:F1} 0 0 1 {0:F1},{1:F1} ", halfArrow, -halfArrow);
					}

					// Don't need to draw an arrowhead if this arrow has Expand set and there's a matching From arrow end in a future arrow, with no cells in between this arrow end and that arrow end.
					// Search for a matching arrow end for Expand.
					bool found = false;
					if (end.Expand)
						for (int col2 = col + 1; col2 < Columns.Count && !found; col2++)
							foreach (var arrow2 in Columns[col].Arrows)
								if (arrow2.From.Any(e => e.Row == end.Row))
								{
									found = true;
									for (int cc = col + 1; cc < col2; cc++)
										found &= Rows[end.Row][cc].Empty();

									if (found)
										break;
								}

					s.AppendFormat("H {0:F1} ", FindArrowRight(end, col, widths) - halfArrow);

					if (found)
						s.AppendFormat("v {0:F1} ", -halfArrow * 2);
					else
					{
						// Paint a right end arrowhead, starting at its bottom left: down, up/right, up/left, down.
						s.AppendFormat("v {0:F1} ", halfArrow);
						s.AppendFormat("l {0:F1},{1:F1} ", halfArrow * 2, -halfArrow * 2);
						s.AppendFormat("l {0:F1},{0:F1} ", -halfArrow * 2);
						s.AppendFormat("v {0:F1} ", halfArrow);
					}

					s.AppendFormat("H {0:F1} ", centre + halfArrowH + halfArrow);

					if (end.Row != topRow)
						s.AppendFormat("a {0:F1},{0:F1} 0 0 1 {1:F1},{1:F1} ", halfArrow, -halfArrow);
				}

				// Bring us back to top right and finish off.
				if (topType == TopBottomType.Left)
				{
					s.AppendFormat("V {0:F1} ", top + (arrow.From.First().Row + 0.5) * rowHeight - arrow.From.First().Width * halfScaleWidth + halfArrowH * 2);
					s.AppendFormat("a {0:F1},{0:F1} 0 0 0 {1:F1},{1:F1} ", halfArrowH * 2, -halfArrowH * 2);
				}
				else if (topType == TopBottomType.Both && arrow.From.First().Width != arrow.To.First().Width)
					s.AppendFormat("c {0:F1},0 {0:F1},{2:F1} {1:F1},{2:F1} ", -halfArrowH, -halfArrowH * 2, (arrow.To.First().Width - arrow.From.First().Width) / 2);
				s.Append("Z\" fill=\"");
			}
			s.Append(ColorTranslator.ToHtml(c));
			s.Append("\" />\n");

			foreach (var end in arrow.From.Where(e => e.Number != null))
			{
				var left = FindArrowLeft(end, col, widths);
				float right = widths.Take(col + (end.Expand ? 0 : 1)).Sum(w => w + 1);
				SvgArrowNumber(s, left, right, top, end);
			}

			foreach (var end in arrow.To.Where(e => e.Number != null))
			{
				var right = FindArrowRight(end, col, widths);
				float left = widths.Take(col + (end.Expand ? 1 : 0)).Sum(w => w + 1);
				SvgArrowNumber(s, left, right, top, end);
			}
		}

		bool filterEmitted = false;
		void SvgArrowNumber(StringBuilder s, float left, float right, float top, ZArrowEnd end)
		{
			string pureFilter = @"
	<filter id=""blur"">
		<feGaussianBlur in=""SourceGraphic"" stdDeviation=""1"" />
	</filter>
";

			string filter = @"
	<filter id=""outline"">
		<feMorphology in=""SourceAlpha"" operator=""dilate"" radius=""1"" result=""A""></feMorphology>
		<feFlood flood-color=""#FFF"" flood-opacity=""1"" result=""B""></feFlood>
		<feComposite in=""B"" in2=""A"" operator=""in"" result=""C""></feComposite>
		<feMerge>
			<feMergeNode in=""C"" />
			<feMergeNode in=""SourceGraphic"" />
		</feMerge>
	</filter>
";
			if (!filterEmitted)
			{
				s.Append(_pure ? pureFilter : filter);  // SvgDocument.Draw() doesn't implement many filters, including feFlood, so when _pure == true, use feGaussianBlur instead...
				filterEmitted = true;
			}
			if (_pure)  // ...and then emit white text blurred twice (to intensify it) and then black text on top of that. And then accept that "white" is actually a bit grey.
			{
				s.AppendFormat("\t<text text-anchor=\"middle\" x=\"{0:F0}\" y=\"{1:F0}\" font-size=\"{2:F0}\" fill=\"White\" filter=\"url(#blur)\">{3:N0}</text>\n",
					left + (right - left) / 2, top + (end.Row + 0.67) * RowHeight, RowHeight * 0.5, end.Number);
				s.AppendFormat("\t<text text-anchor=\"middle\" x=\"{0:F0}\" y=\"{1:F0}\" font-size=\"{2:F0}\" fill=\"White\" filter=\"url(#blur)\">{3:N0}</text>\n",
					left + (right - left) / 2, top + (end.Row + 0.67) * RowHeight, RowHeight * 0.5, end.Number);
				s.AppendFormat("\t<text text-anchor=\"middle\" x=\"{0:F0}\" y=\"{1:F0}\" font-size=\"{2:F0}\" >{3:N0}</text>\n",
					left + (right - left) / 2, top + (end.Row + 0.67) * RowHeight, RowHeight * 0.5, end.Number);
			}
			else
				s.AppendFormat("\t<text text-anchor=\"middle\" x=\"{0:F0}\" y=\"{1:F0}\" font-size=\"{2:F0}\" filter=\"url(#outline)\">{3:N0}</text>\n",
					left + (right - left) / 2, top + (end.Row + 0.67) * RowHeight, RowHeight * 0.5, end.Number);
		}

		/// <summary>Determine how many lines to break report title into.</summary>
		/// <returns>Array containing text broken into lines, and height of text that will fit.</returns>
		(string[], float) ScaleTitle(int width)
		{
			float textWidth = TextWidth(Title, TextHeight * 2);  // Desired title text size is double regular text size.
			float oversizeRatio = textWidth / width;
			int desiredLines = (int)Math.Ceiling(oversizeRatio);
			int linesToUse = desiredLines <= 3 ? desiredLines : desiredLines <= 7 ? desiredLines / 2 + 1 : desiredLines / 2;  // If the title wants a lot of lines, let's restrict that down a bit.
			(string[] lines, float widest) = BreakText(Title, linesToUse);
			float scaling = width / widest * (_pure ? 1f : 1.1f);  // Scale so that the widest line of the title will neatly fill the full width
			float lineHeight = TextHeight * Math.Min(scaling, 2);  // but limit to at most double regular text size.

			return (lines, lineHeight);
		}

		/// <summary>Write the opening svg tag and the title row.</summary>
		/// <param name="s"></param>
		/// <param name="width">Width of entire report (including any multicolumns).</param>
		/// <param name="height">Height of entire report.</param>
		/// <returns>Height consumed by the title, including the blank pixels above and below the title.</returns>
		int SvgBegin(StringBuilder s, int width, ref int height)
		{
			if (!_pure)
				s.AppendFormat("<div>");

			(string[] lines, float lineHeight) = ScaleTitle(width - 4);
			int titleHeight = (int)Math.Ceiling(lines.Length * lineHeight + lineHeight / 3);
			height += titleHeight - 44;
			int zoomButtonY = (int)((lines.Length == 1 ? RowHeight : lineHeight) * 3 / 2 + 1);

			s.AppendFormat("<svg viewBox=\"0 0 {0} {1}\" width=\"{0}\" align=\"center\">\n", width, height);

			SvgRect(s, 1, 1, 1, width - 2, titleHeight, Colors.TitleBackColor);  // Paint title "row" background.

			if (!_pure)
			{
				// Add '-' and '+' zoom button text.
				s.AppendFormat("\t<text text-anchor=\"middle\" x=\"15\" y=\"{0:F0}\" width=\"30\" font-size=\"22\" fill=\"Navy\">&#160;+&#160;</text>\n" +
							   "\t<text text-anchor=\"middle\" x=\"45\" y=\"{0:F0}\" width=\"30\" font-size=\"22\" fill=\"Navy\">&#160;&#8722;&#160;</text>\n", zoomButtonY);
			}

			SvgMultilineText(s, 1, 2, titleHeight, width - 4, lineHeight, Colors.TitleFontColor, ZAlignment.Center, lines, null, TitleHyper);  // Paint title "row" text.
			s.Append('\n');

			if (!_pure)
			{
				// Add '-' and '+' zoom buttons (with transparent text, so the text added above appears behind the report title text).
				s.AppendFormat("\t<text text-anchor=\"middle\" x=\"15\" y=\"{0:F0}\" width=\"30\" font-size=\"22\" fill-opacity=\"0\" onclick=\"this.parentNode.setAttribute('width', Math.min(this.parentNode.getAttribute('width') * 1.42, document.documentElement.clientWidth - 2))\">&#160;+&#160;</text>\n" +
							   "\t<text text-anchor=\"middle\" x=\"45\" y=\"{0:F0}\" width=\"30\" font-size=\"22\" fill-opacity=\"0\" onclick=\"this.parentNode.setAttribute('width', this.parentNode.getAttribute('width') / 1.42)\">&#160;&#8722;&#160;</text>\n", zoomButtonY);
			}

			return titleHeight + 2;
		}

		/// <summary>Write the column header row(s). Returns the amount of vertical height it has consumed. Doesn't render the Title -- that's in SvgBegin().</summary>
		/// <param name="left">If multiColumn > 1, this is the left edge of _this_ set of column headers.</param>
		/// <param name="top">Top of this header section.</param>
		/// <param name="height">Height of this header section, not including the one-pixel gap below it to separate it from the first data row.</param>
		/// <param name="widths">Widths of all columns.</param>
		void SvgHeader(StringBuilder s, int left, int top, int height, List<float> widths)
		{
			if (!Columns.Exists(col => !string.IsNullOrWhiteSpace(col.Text)))
				return;

			int bottom = top + height;

			bool hasGroupHeadings = HasGroupHeadings();
			if (hasGroupHeadings)
			{
				int start = 0;
				while (start < Columns.Count)
				{
					int end = start;
					while (end < Columns.Count - 1 && Columns[start].GroupHeading == Columns[end + 1].GroupHeading)
						end++;

					if (!string.IsNullOrWhiteSpace(Columns[start].GroupHeading))
						SvgRectText(s, 1, widths.Take(start).Sum() + start + left, top, widths.Skip(start).Take(end - start + 1).Sum() + end - start, RowHeight,
									Colors.TitleFontColor, Colors.TitleBackColor, ZAlignment.Center, Columns[start].GroupHeading);  // Paint group heading.

					start = end + 1;
				}

				s.Append('\n');
			}

			float x = left;
			float text45Offset = float.MaxValue;

			for (int col = 0; col < Columns.Count; col++)
			{
				var column = Columns[col];
				int headSpace = string.IsNullOrWhiteSpace(column.GroupHeading) ? 0 : RowHeight + 1;  // If this column has a group heading, its top is lower.
				Color backColor = column.Color == default ? Colors.TitleBackColor : column.Color;
				Color textColor = column.Color == default ? Colors.TitleFontColor : backColor.GetBrightness() < 0.63 ? Color.White : Color.Black;
				bool nextRotated = Columns.Valid(col + 1) && Columns[col + 1].Rotate;

				// Paint column heading background.
				if (hasGroupHeadings || (!column.Rotate && !nextRotated))
					SvgRect(s, 1, x, top + headSpace, widths[col], height - headSpace, backColor);  // Paint column heading rectangle for no rotate or 90 degrees rotate.
				else  // Various 45 degree rotation cases.
				{
					float right = Math.Min(x + widths[col], Right - 1);

					// Let's handle cases where the column headings need special shapes because this column and/or the next or previous columns are rotated 45 degrees.
					// It's possible for column headings further away to impinge on the region we're painting for this one, but
					// (a) this doesn't happen in any reports I actually generate; and (b) when it does, nothing messy occurs. So I don't bother to handle these.
					string format;
					if (column.Rotate && nextRotated)  // This column heading and next are both rotated: left and right sides are at 46 degrees.
					{
						if (right - height < 1)  // Top two points of a parallelogram would be off left edge of report.
							format = "\t<polygon points=\"1,{7:F0} {2:F0},{5:F0} {3:F0},{5:F0} 1,{6:F0}\" style=\"fill:";  // Paint a trapezoid. Start at top left; go down and right at 45 degrees to right edge of column; left by widths[col]; up and right at 45 degrees to left edge of report; then let it close itself.
						else if (x - height < 1)  // Top left point of a parallelogram would be off left edge of report.
							format = "\t<polygon points=\"1,{4:F0} {1:F0},{4:F0} {2:F0},{5:F0} {3:F0},{5:F0} 1,{6:F0}\" style=\"fill:";  // Paint a pentagon. Start at top left; go right by widths[col]; down and right at 45 degrees by headingsHeight,headingsHeight; left by widths[col]; close.
						else
							format = "\t<polygon points=\"{0:F0},{4:F0} {1:F0},{4:F0} {2:F0},{5:F0} {3:F0},{5:F0}\" style=\"fill:";  // Paint a parallelogram. Start at top left; go right by widths[col]; down and right at 45 degrees by headingsHeight,headingsHeight; left by widths[col]; close.
					}
					else if (column.Rotate && !nextRotated)  // This column heading is rotated but next column heading is flat: left at 45 degrees, right vertical.
					{
						if (x - height < 1)  // Top left point would be off left edge of report.
							format = "\t<polygon points=\"1,{4:F0} {2:F0},{4:F0} {2:F0},{5:F0} {3:F0},{5:F0} 1,{6:F0}\" style=\"fill:";  // Paint a pentagon. Start at top left; go right; down by headingsHeight; left by widths[col]; up and right at 45 degrees to left edge of report; close.
						else
							format = "\t<polygon points=\"{0:F0},{4:F0} {2:F0},{4:F0} {2:F0},{5:F0} {3:F0},{5:F0}\" style=\"fill:";  // Paint a trapezoid. Start at top left, then go right, down by headingsHeight, left by widths[col], then let it close itself.
					}
					else  // This column heading is flat but next column heading is rotated: left vertical, right at 45 degrees.
					{
						if (right - height < 1)  // We are constrained by next column's heading above us.
							format = "\t<polygon points=\"1,{7:F0} {2:F0},{5:F0} 1,{5:F0}\" style=\"fill:";  // Paint a triangle. Start at upper left; go down and right at 45 degrees to bottom; then left; close.
						else
							format = "\t<polygon points=\"{3:F0},{4:F0} {1:F0},{4:F0} {2:F0},{5:F0} {3:F0},{5:F0}\" style=\"fill:";  // Paint a trapezoid. Start at top left; go right; down and right at 45 degrees by headingsHeight,headingsHeight; left by widths[col]; close.
					}

					// x positions:        0: top left; 1: top right;   2: bottom right; 3: bottom left;
					s.AppendFormat(format, x - height,  right - height, right,           x,
					// y positions: 4: header top; 5: bottom; 6 & 7: heights along the left edge which represent how much is truncated off the left edge.
									top,           bottom,    bottom - x + 1, bottom - right + 1);

					s.Append(ColorTranslator.ToHtml(backColor));
					s.Append("\" />\n");
				}

				// Paint text.
				if (hasGroupHeadings && column.Rotate)  // Rotate this column heading text by 90 degrees.
				{
					s.Append("\t<text alignment-baseline=\"middle\" ");

					s.AppendFormat("text-anchor=\"end\" x=\"{0:F0}\" y=\"{1:F0}\" width=\"{2:F0}\" transform=\"rotate(90 {0:F0},{1:F0})\" font-size=\"{3}\" fill=\"",
									x + widths[col] / 2 - RowHeight / 4, bottom - RowHeight / 4, height, Math.Min((RowHeight * (_pure ? 0.681 : 0.75)), widths[col]));
					s.Append(ColorTranslator.ToHtml(textColor));
					s.Append("\">");

					s.Append(WebUtility.HtmlEncode(column.Text));
					s.Append("</text>\n");
					text45Offset = float.MaxValue;
				}
				else if (!hasGroupHeadings && column.Rotate)  // Paint column heading text rotated 45 degrees.
				{
					s.Append("\t");

					if (!_pure && !string.IsNullOrEmpty(column.Hyper))
						AppendStrings(s, "<a href=\"", column.Hyper, "\">");

					// Draw text inside parallelogram.
					s.Append("<text ");

					bool previousRotated = Columns.Valid(col - 1) && Columns[col - 1].Rotate;
					float colWidth = previousRotated ? Math.Min(widths[col - 1], widths[col]) : widths[col];
					float previousOffset = text45Offset;
					text45Offset = widths[col] / 2 - (nextRotated ? RowHeight / 2F : 0);

					s.AppendFormat("text-anchor=\"end\" x=\"{0:F0}\" y=\"{1:F0}\" width=\"{2:F0}\" transform=\"rotate(45 {0:F0},{1:F0})\" font-size=\"{3}\" fill=\"",
								   x + Math.Min(previousOffset, text45Offset), bottom - 3, height * 1.41 - RowHeight * 3 / 4, RowHeight * 3 / 4);
					s.Append(ColorTranslator.ToHtml(textColor));
					s.Append("\">");

					s.Append(WebUtility.HtmlEncode(column.Text));
					s.Append("</text>");

					if (!_pure && !string.IsNullOrEmpty(column.Hyper))
						s.Append("</a>");
					s.Append("\n");
				}
				else  // Paint column heading text flat.
				{
					SvgMultilineText(s, 1, (int)x, top + headSpace, (int)widths[col] - (nextRotated && !hasGroupHeadings ? RowHeight : 0), height - headSpace, TextHeight,
						textColor, column.Alignment, column.Text, null, column.Hyper);

					text45Offset = float.MaxValue;
				}
				x += widths[col] + 1;
			}

			s.Append('\n');
		}

		/// <summary>value, scaleMin and scaleMax are all in the before-scaling ordinate system. outputWidth gives the range of the after-scaling ordinate system.</summary>
		double Scale(double value, double outputWidth, double scaleMin, double scaleMax)
		{
			return (value - scaleMin) / (scaleMax - scaleMin) * outputWidth;
		}

		/// <summary>value, scaleMin and scaleMax are all in the before-scaling ordinate system. outputWidth gives the range of the after-scaling ordinate system.</summary>
		double ScaleWidth(double value, double outputWidth, double scaleMin, double scaleMax)
		{
			return value / (scaleMax - scaleMin) * outputWidth;
		}

		/// <summary>scaleMin and scaleMax are in the before-scaling ordinate system. pixelValue and outputWidth are in the after-scaling ordinate system.</summary>
		double AntiScale(double pixelValue, double outputWidth, double scaleMin, double scaleMax)
		{
			return (pixelValue / outputWidth * (scaleMax - scaleMin)) + scaleMin;
		}

		/// <summary>Return the y ordinate of the middle of the stated row. top is the top of the top row of the table. row is the 0-based row number. </summary>
		double RowMid(float top, int row, float rowHeight)
		{
			return top + (row + 0.5) * rowHeight;
		}

		/// <summary>Return normal distribution value for this x. https://en.wikipedia.org/wiki/Normal_distribution </summary>
		double Gauss(double x, double mean, double variance)
		{
			return 1 / Math.Sqrt(2 * Math.PI * variance) * Math.Exp(-Math.Pow(x - mean, 2) / 2 / variance);
		}

		void SvgChart(StringBuilder s, int top, int height, double left, double width, double chartMin, double chartMax, int maxPoints, Color backColor, Color chartColor, ZCell cell, ZRow row, int column, bool lastRow)
		{
			if (cell.Color != Color.Empty || cell.Border != Color.Empty)
				SvgRect(s, 1, left, top, width, height, backColor, cell.Border);  // Paint chart cell(s) background.

			if ((cell.ChartType == ChartType.None || cell.ChartType.HasFlag(ChartType.Bar)) && cell.Number != null)  // Bar
			{
				double? fill = ScaleWidth(cell.Number ?? 0, 1, 0, chartMax - chartMin);
				if (CalculateFill != null)
					CalculateFill?.Invoke(row, column, chartMin, chartMax, ref fill);
				if (fill != null)
					SvgRect(s, 1, left + Scale(0, width, chartMin, chartMax), top, (double)fill * width, height, chartColor);  // Paint bar.
			}

			int count = 0;
			if (cell.ChartType.HasFlag(ChartType.Rug) || cell.ChartType.HasFlag(ChartType.BoxPlot) || 
			    cell.ChartType.HasFlag(ChartType.Histogram) || cell.ChartType.HasFlag(ChartType.KernelDensityEstimate) || cell.ChartType.HasFlag(ChartType.Area))
			{
				cell.Data.Sort();
				count = cell.Data.Count;
			}

			if (cell.ChartType.HasFlag(ChartType.BoxPlot) && count > 1)  // BoxPlot
			{
				if (cell.Number != null)
					SvgRect(s, 1, left + Scale((double)cell.Number, width, chartMin, chartMax) - 0.5, top, 1, height * 0.9, ZReportColors.AddDark(chartColor, Color.FromArgb(255, 223, 255)));  // Paint mean stripe.

				double median = count % 2 == 0 ? (cell.Data[count / 2 - 1] + cell.Data[count / 2]) / 2 : cell.Data[count / 2];
				double firstQuartile = count % 4 == 0 ? cell.Data[count / 4] : cell.Data[count / 4];
				double thirdQuartile = count % 4 == 0 ? cell.Data[(count * 3 / 4) - 1] : cell.Data[count * 3 / 4];
				int percentile2 = (int)(count * 0.02);
				int percentile98 = (int)(count * 0.98) - 1;

				SvgRect(s, 1, left + Scale(cell.Data[percentile2], width, chartMin, chartMax), top + height * 0.4,
				        ScaleWidth(cell.Data[percentile98] - cell.Data[percentile2], width, chartMin, chartMax), height * 0.1, chartColor);  // Whisker from 2nd percentile to 98th percentile -- contains all data within 2 std deviations.

				SvgRect(s, 1, left + Scale(firstQuartile, width, chartMin, chartMax), top + height * 0.1,
				        ScaleWidth(thirdQuartile - firstQuartile, width, chartMin, chartMax), height * 0.7, chartColor);  // Second quartile / third quartile box.

				SvgRect(s, 1, left + Scale(median, width, chartMin, chartMax) - 0.5, top + height * 0.1, 1, height * 0.7, backColor);  // Median white stripe.
				
				for (int i = 0; i < percentile2; i++)
					SvgRect2(s, 1, left + Scale(cell.Data[i], width, chartMin, chartMax) - 0.5, top + height * 0.42, 1, height * 0.06, chartColor);  // Paint outlying data point.
				for (int i = percentile98; i < count; i++)
					SvgRect2(s, 1, left + Scale(cell.Data[i], width, chartMin, chartMax) - 0.5, top + height * 0.42, 1, height * 0.06, chartColor);  // Paint outlying data point.
			}

			if (cell.ChartType.HasFlag(ChartType.Histogram) && count > 1)  // Histogram
			{
				int bins = (int)Math.Ceiling(2 * Math.Pow(maxPoints, 1.0/3));  // number of bars our histogram will have, from Rice's Rule.
				double binWidth = Math.Round(width / bins + 0.05, 1);  // in "pixels"
				bins = (int)Math.Round(width / binWidth);
				var heights = new List<int>();  // Heights of each bar, in counts of values that fall into that bin.
				double valuesPerBin = (chartMax - chartMin + 1) / bins;  // Width of each bar in source numbering.
				if (Columns[column].Alignment == ZAlignment.Integer)
				{
					bins = Math.Min(bins, (int)(chartMax - chartMin + 1));  // Ensure we don't have more bins than integers in our range.
					bins = (int)((chartMax - chartMin + 1) / Math.Round((chartMax - chartMin + 1) / bins));
					int intsPerBin = (int)Math.Round(chartMax - chartMin + 1) / bins;
					valuesPerBin = intsPerBin;
					for (int j = (int)chartMin; j <= chartMax; j += intsPerBin)
						heights.Add(cell.Data.Count(d => j <= d && d < j + intsPerBin));
				}
				else
				{
					int i = 0; // Index into sourceCell.Data for where we're up to right now.
					for (double xx = 0; xx < width; xx += binWidth)
					{
						int binHeight = 0;
						double binEnd = AntiScale(xx + binWidth, width, chartMin, chartMax);
						while (i < count && cell.Data[i] < binEnd)
						{
							binHeight++;
							i++;
						}
						heights.Add(binHeight);
					}
				}
				for (int i = 0; i < heights.Count; i++)
					if (heights[i] > 0)
						SvgRect(s, 1, left + width * i / bins, top + height - height * heights[i] / heights.Max(), 
						        width / bins - 0.1, height * heights[i] / heights.Max(), chartColor);

				SvgRect2(s, 1, left + Scale(cell.Number ?? 0, width, chartMin, chartMax) - 0.05, top, 0.1, height, Color.Gray);  // Paint mean stripe.

				if (lastRow)  // Write some tiny numbers at the bottom of the row, showing the minimum and maximum values within the Data, and the bin size of each bar.
				{
					SvgText(s, 1, (int)left, (int)(top + height * 0.8), (int)width, height / 6, Color.Black, ZAlignment.Left, chartMin.ToString());
					SvgText(s, 1, (int)(left + Math.Min(width * 1 / bins, 2)), (int)(top + height * 0.8), (int)width, height / 6, Color.Black, ZAlignment.Left, valuesPerBin.ToString());
					SvgText(s, 1, (int)left, (int)(top + height * 0.8), (int)Math.Round(width), height / 6, Color.Black, ZAlignment.Right, chartMax.ToString());
				}
			}

			if (cell.ChartType.HasFlag(ChartType.KernelDensityEstimate) && count > 1)  // Kernel Density Estimate
			{
				double sum = cell.Data.Sum();
				double squaredSum = cell.Data.Sum(x => x * x);
				double mean = sum / count;
				double stddev = count <= 1 ? 0 : Math.Sqrt((squaredSum - (sum * sum / count)) / (count - 1));
				double bandwidth =  1.06 * stddev * Math.Pow(count, -0.2);

				int n = width < 100 ? (int)width * 10 : (int)width * 10 / (int)(width / 50);  // Number of points in curve for our kernel density estimate polygon.

				var points = new List<Tuple<double, double>>
				{
					new Tuple<double, double>(left, 0)
				};
				double yMax = 0;
				for (double xx = chartMin; xx < chartMax; xx += (chartMax - chartMin) / n)
				{
					double y = 0;
					foreach (double d in cell.Data)
						y += Gauss(xx, d, bandwidth);
					yMax = Math.Max(yMax, y);
					points.Add(new Tuple<double, double>(left + Scale(xx, width, chartMin, chartMax), y));
				}
				points.Add(new Tuple<double, double>(left + width, 0));
				SvgPolygon(s, 1, points, top, height, yMax, chartColor);
				SvgRect(s, 1, left + Scale(mean, width, chartMin, chartMax) - 0.05, top, 0.1, height, backColor);  // Paint mean stripe.
				SvgRect(s, 1, left + Scale(mean - stddev, width, chartMin, chartMax) - 0.05, top + height * 0.393, 0.1, height * 0.607, backColor);  // -1 stddev.
				SvgRect(s, 1, left + Scale(mean + stddev, width, chartMin, chartMax) - 0.05, top + height * 0.393, 0.1, height * 0.607, backColor);  // +1 stddev.
				SvgRect(s, 1, left + Scale(mean - 2 * stddev, width, chartMin, chartMax) - 0.05, top + height * 0.865, 0.1, height * 0.135, backColor);  // -2.
				SvgRect(s, 1, left + Scale(mean + 2 * stddev, width, chartMin, chartMax) - 0.05, top + height * 0.865, 0.1, height * 0.135, backColor);  // +2.
				double height3 = Math.Max(height * 0.011, 0.1);
				SvgRect2(s, 1, left + Scale(mean - 3 * stddev, width, chartMin, chartMax) - 0.05, top + height - height3, 0.1, height3, backColor);  // -3.
				SvgRect2(s, 1, left + Scale(mean + 3 * stddev, width, chartMin, chartMax) - 0.05, top + height - height3, 0.1, height3, backColor);  // +3.
			}

			if (cell.ChartType.HasFlag(ChartType.Rug))  // Rug
		    {
				int markNumber = 0;  // This is going to be 0 for most marks, but where marks coincide or overlap we will increment this to prevent them overpainting.
				double lastCentre = double.MinValue;
				double markWidth = Math.Max(Math.Min(width / Math.Max(count * 2, 100.0), height * 0.1), 0.2); // Width of a mark is 1/100th of the row width, or smaller if there's lots of data points, or the mark height; whichever smallest. If less than 0.2, round up to 0.2.
				int marksPerRow = Math.Min(Math.Max((int)Math.Sqrt(count), 9), 75);
				double markHeight = height * 0.9 / marksPerRow;
				
				foreach (double d in cell.Data)
				{
					double markCentre = Scale(d, width, chartMin, chartMax);
					if (markCentre - lastCentre < markWidth * 1.1)  // Clear of other marks? Drop down to bottom of the row. Not clear? Go in above the last mark, unless we've reached top of the row, in which case modulo back down to 0.
						markNumber = (markNumber + 1) % marksPerRow;
					else
					{
						markNumber = 0;
						lastCentre = markCentre;
					}

					Color markColor = cell.ChartType.HasFlag(ChartType.KernelDensityEstimate) || 
						(cell.ChartType.HasFlag(ChartType.Bar) && 0 < d && d < cell.Number) ? backColor : chartColor;
					SvgRect2(s, 1, left + markCentre - markWidth / 2, top + height - (markNumber + 1) * markHeight, markWidth, markHeight, markColor);  // Paint mark.
				}
			}

			if (cell.ChartType.HasFlag(ChartType.Area))  // Area
			{
				var points = new List<Tuple<double, double>>
				{
					new Tuple<double, double>(left, 0)
				};
				for (int i = 0; i < count; i++)
					points.Add(new Tuple<double, double>(left + Scale(i, width, 0, count - 1), cell.Data[i]));
				points.Add(new Tuple<double, double>(left + width, 0));
				SvgPolygon(s, 1, points, top, height, chartMax, chartColor);
				SvgRect2(s, 1, left, top + height - Scale(0.5, height, chartMin, chartMax) - 0.1, width, 0.1, chartColor); // Paint mean stripe.
				SvgRect2(s, 1, left, top + height - Scale(0.5, height, chartMin, chartMax), width, 0.1, backColor); // Paint mean stripe.
			}

			if (cell.ChartType.HasFlag(ChartType.XYScatter) && cell.Tag is List<ChartPoint> points2)  // XYScatter
			{
				var minY = Math.Min(0, points2.Min(p => double.IsNaN(p.Y) ? 0 : p.Y));
				var maxY = Math.Max(1, points2.Max(p => p.Y));
				var radius = Math.Min(Math.Max(width / maxPoints / 4, 0.1), 2.0);

				SvgRect2(s, 1, left, top + height - Scale(1, height, minY, maxY) - 0.05, width, 0.1, chartColor); // Paint "full height" stripe. (Points will actually appear above this line because scaling.)
				SvgRect2(s, 1, left, top + height - Scale(0.5, height, minY, maxY) - 0.05, width, 0.1, chartColor); // Paint mean stripe.
				SvgRect2(s, 1, left, top + height - Scale(0, height, minY, maxY) - 0.05, width, 0.1, chartColor); // Paint baseline.

				foreach (var point in points2)
					if (!double.IsNaN(point.Y))
						SvgCircle(s, 1, left + Scale(point.X.Ticks, width, chartMin, chartMax), top + height - Scale(point.Y, height, minY, maxY), radius, point.Color);
			}
		}

		/// <summary>Write a single table row.</summary>
		void SvgRow(StringBuilder s, int top, int height, int left, List<float> widths, List<double> mins, List<double> maxs, int maxPoints, int width, ZRow row, bool odd)
		{
			SvgRect(s, 1, left, top, width, height, Colors.GetBackColor(row, odd));  // Paint the background for the whole row.

			// Ensure ChartCells point to themselves, where they're part of a multi-cell chart.
			foreach (var cell in row)
				if (cell.ChartCell == null && row.Any(c => c.ChartCell == cell))
					cell.ChartCell = cell;
			
			// Paint any chart cells for this row.
			int start = 0;
			while (start < Math.Min(Columns.Count, row.Count))
			{
				int end = start;
				while (end < Math.Min(Columns.Count, row.Count) - 1 && row[start].ChartCell != null && row[start].ChartCell == row[end + 1].ChartCell)
					end++;

				if (start == end && row[start].ChartType != ChartType.None && row[start].ChartCell == null)  // This is a one-column wide bar.
					row[start].ChartCell = row[start];
				var sourceCell = row[start].ChartCell ?? row[start];
				int barSource = row.FindIndex(cell => cell == sourceCell);

				if (sourceCell.Color != Color.Empty || sourceCell.Border != Color.Empty || sourceCell.ChartCell != null)
					SvgChart(s, top, height, widths.Take(start).Sum() + start + left, widths.Skip(start).Take(end - start + 1).Sum() + end - start,
					         MaxChartByColumn ? mins[barSource] : mins.Min(), MaxChartByColumn ? maxs[barSource] : maxs.Max(), maxPoints,
					         Colors.GetBackColor(row, odd, sourceCell.Color), sourceCell.GetBarColor(Colors.GetBackColor(row, odd), Colors.BarNone), sourceCell, row, barSource, row == Rows.Last());

				start = end + 1;
			}
			s.Append('\n');

			float x = left;
			for (int col = 0; col < Columns.Count && col < row.Count; col++)
			{
				SvgText(s, 1, (int)x, top, (int)widths[col], height, Columns[col], row[col]);  // Write a data cell.

				x += widths[col] + 1;
			}
			s.Append('\n');
		}

		/// <summary>Render report to SVG.</summary>
		/// <param name="pure">true if this is to be rendered to a bitmap by SvgDocument.Draw(), as opposed to saving out for rendering by a web browser.</param>
		public override string ToSvg(bool pure = false)
		{
			StringBuilder sb = new StringBuilder();
			ToSvg(sb, null, pure);
			return sb.ToString();
		}

		public string ToSvg(double aspectRatio)
		{
			StringBuilder sb = new StringBuilder();
			ToSvg(sb, aspectRatio, true);
			return sb.ToString();
		}

		private bool? hasGroupHeadings = null;
		/// <summary>Do any columns have their GroupHeading set?</summary>
		bool HasGroupHeadings()
		{
			if (!hasGroupHeadings.HasValue)
			{
				hasGroupHeadings = false;
				foreach (ZColumn col in Columns)
					hasGroupHeadings |= col != null && !string.IsNullOrEmpty(col.GroupHeading);
			}

			return (bool)hasGroupHeadings;
		}

		/*												WHHYYYY?????????
		 *													An essay
		 *
		 *	In the very first draft of this code, I used an 11-point font to measure how much width text would 
		 *	take up, in order to set column widths. And I (eventually) set RowHeight to 22, "enough to fit 
		 *	default-sized text". That was trial and error: if I used 22 as row height, and text height was 
		 *	three quarters of that, then the widths I has calculated using 11-point text were about correct.
		 *
		 *	Now I've changed TextWidth() to use the same height that I'm targeting for the final rendering of the 
		 *	font, and I find that I have to scale widths down by about 0.648. Which is to say, about 11/17ths.
		 *
		 *	At the same time I removed other scalings from the code, and I had expected that all the scalings would 
		 *	cancel out to nothing: that I was multiplying here, dividing there, and once I simplified all of that I 
		 *	wouldn't need any scaling at all. But I do. And I don't know why.
		 *
		 *	Part of it is the difference between points and pixels: I'm specifying font-size="17" without any units, 
		 *	which means the units are pixels, and 1 px = 0.75 pt, so 17 pixels is 12.75 point. Which is closer to 11 
		 *	point. But still different.
		*/

		internal const int RowHeight = 22;  // See above essay.
		internal const int TextHeight = 17;  // Three-quarters of RowHeight, rounded up. This leaves room for descenders (like in gjpqy), and room to "breathe" around the text.
		internal const float ScaleFromWindows96dpiPixelsToSvgUnits = 0.648f;  // See above essay.

		Graphics graphics = null;
		Font font = null;
		/// <summary>Width of text in pixels, independent of scaling of current monitor.</summary>
		float TextWidth(string text, int height = TextHeight)
		{
			if (graphics == null)
			{
				var bitmap = new Bitmap(TextHeight * 100, TextHeight);
				bitmap.SetResolution(96, 96);
				graphics = Graphics.FromImage(bitmap);

				font = new Font("Microsoft Sans Serif", TextHeight);
			}

			float width = graphics.MeasureString(text, font, new SizeF(TextHeight * 100, TextHeight)).Width * ScaleFromWindows96dpiPixelsToSvgUnits;
			return width * height / TextHeight;
		}

		/// <summary>Height of report in internal SVG "pixels". Only valid after ToSvg() has been called.</summary>
		internal int Height;
		/// <summary>Width of report in internal SVG "pixels". Only valid after ToSvg() has been called.</summary>
		internal int Width;
		/// <summary>If .multiColumns == 1, this is the same as Width. If not, it is the right edge of the group of columns we're currently drawing.</summary>
		internal int Right;


		/// This writes an <svg> tag -- it does not include <head> or <body> tags etc.
		public override void ToSvg(StringBuilder sb, double? aspectRatio, bool pure = false)
		{
			_pure = pure;
			filterEmitted = false;
			var widths = new List<float>();  // Width of each column in pixels. "float", because MeasureString().Width returns a float.
			var mins = new List<double>();   // Minimum numeric value in each column, or if all numbers are positive, 0.
			var maxs = new List<double>();   // Maximum numeric value in each column.
			(int titleHeight, int headerHeight) = Metrics(widths, mins, maxs, out int maxPoints);
			Width = (int)widths.Sum() + widths.Count + 1;  // Total width of the whole SVG -- the sum of each column, plus pixels for spacing left, right and between.
			double max = maxs.DefaultIfEmpty(1).Max();

			int left = 1;
			Height = titleHeight + headerHeight + Rows.Count * (RowHeight + 1);
			int multiColumns = MultiColumnOK && aspectRatio is double ar ? Math.Max((int)Math.Round(Math.Sqrt(ar / Width * Height)), 1) : 1;
			int rowsPerCol = (int)Math.Ceiling((double)Rows.Count / multiColumns);
			Height = titleHeight + headerHeight + rowsPerCol * (RowHeight + 1);
			Right = (int)(Width * 1.1 * multiColumns - Width * 0.1);

			titleHeight = SvgBegin(sb, Right, ref Height);
			int arrowTop = titleHeight + headerHeight;

			for (int col = 0; col < multiColumns; col++)
			{
				int thisLeft = (int)(left + Width * 1.1 * col);
				Right = thisLeft + Width;
				SvgHeader(sb, thisLeft, titleHeight, headerHeight - 1, widths);
				int rowTop = titleHeight + headerHeight;
				bool odd = true;

				for (int row = rowsPerCol * col; row < rowsPerCol * (col + 1); row++)
				{
					SvgRow(sb, rowTop, RowHeight, thisLeft, widths, mins, maxs, maxPoints, Width, row < Rows.Count ? Rows[row] : new ZRow(), odd);

					rowTop += RowHeight + 1;
					odd = !odd;
				}
			}

			for (int col = 0; col < Columns.Count; col++)
				foreach (var arrow in Columns[col].Arrows.OrderByDescending(a => (a.From.Count + a.To.Count) * 100 + Math.Abs((a.From.FirstOrDefault()?.Row - a.To.FirstOrDefault()?.Row) ?? 0)))
					SvgArrow(sb, 1, col, arrow, widths, arrowTop - 0.5F, RowHeight + 1);

			Width = (int)(Width * 1.1 * multiColumns - Width * 0.1);

			sb.Length -= 1;  // Remove trailing \n
			sb.Append("</svg>");

			if (!pure && !string.IsNullOrEmpty(HtmlDescription))
				AppendStrings(sb, "\n<p>", HtmlDescription, "</p>\n");
			else if (!pure && !string.IsNullOrEmpty(Description))
				AppendStrings(sb, "\n<p>", Description.Replace("\n", "<br/>\n"), "</p>\n");

			if (!pure)
				sb.Append("</div>\n");

			sb.Append('\n');
		}

		public override string ToString()
		{
			return "ZoomReport " + Title + ": " + Rows.Count.ToString() + " rows.";
		}
	}

	public class ZoomHtmlInclusion: ZoomReportBase
	{
		public string Literal { get; set; }

		public ZoomHtmlInclusion(string literal) { Literal = literal; }

		public override string ToCsv(char separator) { return ""; }

		public override string ToHtml() { return Literal; }

		public override string ToSvg(bool pure = false) { return Literal; }

		public override void ToSvg(StringBuilder sb, double? aspectRatio, bool pure = false) { sb.Append(Literal); }

		public override IEnumerable<Color> BarCellColors() { return new List<Color>(); }
	}

	/// <summary>An output-format-specific separator. In a multi-page printed report, this is a page break. In a CSV, it's some newlines and a "----".</summary>
	public class ZoomSeparator : ZoomReportBase
	{
		public override string ToCsv(char separator) { return "\n\n----\n\n"; }

		public override string ToHtml() { return "\n<br/>\n\n"; }

		public override string ToSvg(bool pure = false) { return "</div>\n<div style=\"display: flex; flex-flow: row wrap; justify-content: space-around;\">\n"; }

		public override void ToSvg(StringBuilder sb, double? aspectRatio, bool pure = false) { sb.Append(ToSvg(pure)); }

		public override IEnumerable<Color> BarCellColors() { return new List<Color>(); }
	}

	public class ZoomReports: List<ZoomReportBase>
	{
		string Title { get; set; }
		/// <summary>If true, show bars in HTML reports.</summary>
		bool Bars { get; set; }

		readonly ZReportColors colors;
		public ZReportColors Colors { get { return colors; } }
		
		public ZoomReports(string title = null)
		{
			Title = title;
			colors = new ZReportColors();
			Bars = true;
		}

		public new void Add(ZoomReportBase report)
		{
			base.Add(report);
			if (report.colors == null)
				report.colors = colors;
		}

		/// <summary>Return a list of colours of bar cells, over all reports.</summary>
		List<Color> BarCellColors()
		{
			var result = new List<Color>();
			foreach (var report in this)
				if (report is ZoomReport zoomReport)
				{
					bool odd = true;
					foreach (var row in zoomReport.Rows)
					{
						foreach (var cell in row)
							if (!result.Contains(cell.GetBarColor(colors.GetBackColor(row, odd))))
							    result.Add(cell.GetBarColor(colors.GetBackColor(row, odd)));
						odd = !odd;
					}
				}

			if (!result.Contains(colors.BarNone))
				result.Add(colors.BarNone);
			if (!result.Contains(ZReportColors.AddDark(colors.BarNone, colors.OddColor)))
				result.Add(ZReportColors.AddDark(colors.BarNone, colors.OddColor));

			return result;
//			return this.SelectMany(x => x.BarCellColors()).Distinct().ToList();
		}

		/// <summary>Export to the specified format.</summary>
		public string ToOutput(OutputFormat outputFormat)
		{
			switch (outputFormat) {
				case OutputFormat.Svg: return ToSvg();
				case OutputFormat.HtmlTable: return ToHtml();
				case OutputFormat.Tsv: return ToCsv('\t');
				case OutputFormat.Csv: return ToCsv(',');
				default: return "";
			}
		}

		public string ToCsv(char separator)
		{
			StringBuilder sb = new StringBuilder();
			foreach (ZoomReportBase report in this) {
				sb.Append(report.ToCsv(separator));
		 		sb.Append("\n--------\n\n");
			}
			return sb.ToString();
		}

		void HtmlBegin(StringBuilder sb)
		{
			sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"/><title>");
			if (!string.IsNullOrEmpty(Title))
				sb.Append(WebUtility.HtmlEncode(Title));
			else if (Count > 0)
				sb.Append(WebUtility.HtmlEncode(this[0].Title));
			sb.Append("</title>\n");

			sb.Append("  <style type=\"text/css\">\n");
			sb.Append("\t.back   { background: #eef; color: black; }\n");
			sb.Append("\t@media (prefers-color-scheme: dark) {\n");
			sb.Append("\t  .back { background: #112; color: white; }\n");
			sb.Append("\t}\n");
		}

		public string ToHtml()
		{
			StringBuilder sb = new StringBuilder();
			HtmlBegin(sb);

			if (Bars)
			{
				sb.Append("\t.barcontainer { position:relative }\n");
				sb.Append("\t.bar { padding-bottom: 18px; background-color: #DFDFDF }\n");
				sb.Append("\t.bartext { position: absolute; top: 0px; left: 0px; text-align: right; width: 100% }\n");

				foreach (var c in BarCellColors())
					sb.AppendFormat("\t.bar{0:X2}{1:X2}{2:X2} {{ padding-bottom: 18px; background-color: {3} }}\n", 
					                c.R, c.G, c.B, ColorTranslator.ToHtml(c));
			}
			sb.Append("  </style>\n");

			sb.Append("</head><body class=\"back\">\n");
			//sb.Append("");  // TODO: cellstyles.ToHtml here?

			foreach (ZoomReportBase report in this) {
				sb.Append(report.ToHtml());
			}

			sb.Append("</body>\n");
			return sb.ToString();
		}

		class ReportWithSize
		{
			public Bitmap Bitmap;
			public int Width;
			public int Height;
		}

		readonly List<List<ReportWithSize>> images = new List<List<ReportWithSize>>();  // Each List<ReportWithSize> represents one page worth of reports.
		int pageNumber = 0;
		public PrintDocument ToPrint()
		{
			var pd = new PrintDocument();
			pd.PrintPage += new PrintPageEventHandler(this.PrintPage);
			pd.OriginAtMargins = true;
			return pd;
		}

		private void PrintPage(object sender, PrintPageEventArgs ev)
		{
			var bounds = ev.MarginBounds;  // Area of page to be printed on, dimensioned in hundredths of an inch for some Microsoftean reason.
			List<ReportWithSize> page;

			if (!images.Any())  // This code runs on the first call of PrintPage, to build the list of images that need to be printed.
			{
				page = new List<ReportWithSize>();
				images.Add(page);
				foreach (ZoomReportBase report in this)
				{
					if (report is ZoomReport r)
					{
						var image = new ReportWithSize()
						{
							Bitmap = r.ToBitmap((bounds.Width * ev.PageSettings.PrinterResolution.X / 100), (bounds.Height * ev.PageSettings.PrinterResolution.Y / 100))
						};
						// ZoomReport Height and Width are only available _after_ it is rendered.
						image.Height = r.Height;
						image.Width = r.Width;
						page.Add(image);
					}
					else if (report is ZoomSeparator)
					{
						page = new List<ReportWithSize>();
						images.Add(page);
					}
				}
				pageNumber = 0;
			}

			page = images[pageNumber];
			var totalHeight = page.Sum(i => i.Height + ZoomReport.RowHeight * 2) - ZoomReport.RowHeight * 2;  // If there are several images on a page, put two rows' worth of "pixels" between them. These "pixels" are in the SVG's internal scale.
			float scale = 1.0F * totalHeight / bounds.Height;
			float top = 0;

			foreach (var image in page)
			{
				float pagePart = 1.0F * image.Height / totalHeight;  // How much of the available page height is dedicated to this image? From 0 to 1.
				float imageAspectRatio = 1.0F * image.Width / image.Height;
				float pagePartAspectRatio = 1.0F * bounds.Width / bounds.Height / pagePart;
				if (imageAspectRatio > pagePartAspectRatio)  // Report is wider than page part: use that full width but not the full height.
				{
					ev.Graphics.DrawImage(image.Bitmap, 0, top, bounds.Width, bounds.Width / imageAspectRatio);
					top += bounds.Width / imageAspectRatio + 50 / scale;
				}
				else  // Report is taller than page: leave space at left and right.
				{
					float newHeight = bounds.Height * pagePart;
					float newWidth = newHeight * imageAspectRatio;
					ev.Graphics.DrawImage(image.Bitmap, (bounds.Width - newWidth) / 2, top, newWidth, newHeight);
					top += newHeight + 50 / scale;
				}
			}

			pageNumber++;
			ev.HasMorePages = pageNumber < images.Count;
		}

		public string ToSvg()
		{
			StringBuilder sb = new StringBuilder();
			HtmlBegin(sb);

			sb.Append("\tsvg { box-shadow: 0 4px 20px rgba(88,88,88,0.6) }\n");
			sb.Append("  </style>\n");

			sb.Append("</head><body class=\"back\">\n");
			//sb.Append("");  // TODO: cellstyles.ToHtml here?

			if (Count == 1)
				sb.Append("<div>\n");
			else
				sb.Append("<div style=\"display: flex; flex-flow: row wrap; justify-content: space-around;\">\n");

			for (int i = 0; i < Count; i++)
				this[i].ToSvg(sb);

			sb.Append(@"</div>

<script>
function setwidths() {
  for (const svg of  document.querySelectorAll('svg'))
    if (svg.getAttribute('width') > document.documentElement.clientWidth)
      svg.setAttribute('width', document.documentElement.clientWidth - 2);
}

window.onload = function() {
  for (const text of document.querySelectorAll('text')) {
    var fit = text.getComputedTextLength() / (text.getAttribute('width') - 2);
    if (fit > 1)
      text.setAttribute('font-size', text.getAttribute('font-size') / fit);
  }
  setwidths();
}
window.onresize = setwidths;
</script>
");

			sb.Append("</body>\n");

			return sb.ToString();
		}
	}
}
