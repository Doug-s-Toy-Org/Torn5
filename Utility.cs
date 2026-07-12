using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Xml;
using Torn5.Properties;
using System.Windows.Forms;

namespace Torn
{
	/// <summary>Static methods, including extension methods, used in misc places.</summary>
	public static class Utility
	{
		/// <summary>Return a string like "Today", "Yesterday", "Sun 12th", or the date, depending on how far in time we're looking.</summary>
		public static string FriendlyDate(this DateTime date)
		{
			int daysAgo = (int)DateTime.Now.Date.Subtract(date.Date).TotalDays;

			if (daysAgo == 0) return "Today";
			if (daysAgo == 1) return "Yesterday";
			if (daysAgo > 0 && daysAgo < 7) return date.DayOfWeek.ToString().Substring(0, 3) + " " + Ordinate(date.Day);
			return date.ToShortDateString();
		}

		public static string FriendlyDateTime(this DateTime dateTime)
		{
			return FriendlyDate(dateTime) + dateTime.ToString(" HH:mm");
		}

		public static string JustPlayed(this DateTime date)
		{
			int hoursAgo = (int)DateTime.Now.Date.Subtract(date.Date).TotalHours;

			if (hoursAgo == 0) 
				return "Just played";
			if (hoursAgo < 48)
				return "Played " + hoursAgo.ToString() + " hours ago";
			return "Played on " + date.Date.ToLongDateString();
		}

		public static string ShortDateTime(this DateTime date)
		{
			string s = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
			int i = s.IndexOf(":ss");
			if (i > -1)
				s = s.Remove(i, 3);
			return date.ToShortDateString() + " " + date.ToString(s);
		}

		/// https://stackoverflow.com/questions/10100088/is-there-a-simple-function-for-rounding-a-datetime-down-to-the-nearest-30-minute
		public static DateTime TruncDateTime(this DateTime dateTime, TimeSpan delta)
		{
			return new DateTime((dateTime.Ticks / delta.Ticks) * delta.Ticks);
		}

		public static Color StringToColor(string s)
		{
			double hash = 0;
			foreach (char c in s)
				hash = hash * 0.99 + Convert.ToInt32(c);

			return ColorFromHSV((int)(hash % 360), Math.Abs(Math.Sin(hash * 19 + 2) * 0.9) + 0.1, Math.Abs(Math.Sin(hash * 23 + 4) * 0.6) + 0.2);
		}

		/// https://stackoverflow.com/questions/1335426/is-there-a-built-in-c-net-system-api-for-hsv-to-rgb
		/// Ranges are 0 - 360 for hue, and 0 - 1 for saturation or value.
		public static Color ColorFromHSV(double hue, double saturation, double value)
		{
			int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
			double f = hue / 60 - Math.Floor(hue / 60);

			value *= 255;
			int v = Convert.ToInt32(value);
			int p = Convert.ToInt32(value * (1 - saturation));
			int q = Convert.ToInt32(value * (1 - f * saturation));
			int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

			if (hi == 0)
				return Color.FromArgb(255, v, t, p);
			else if (hi == 1)
				return Color.FromArgb(255, q, v, p);
			else if (hi == 2)
				return Color.FromArgb(255, p, v, t);
			else if (hi == 3)
				return Color.FromArgb(255, p, q, v);
			else if (hi == 4)
				return Color.FromArgb(255, t, p, v);
			else
				return Color.FromArgb(255, v, p, q);
		}

		/// <summary>Return a mixture of two source colors, mixing R, G and B separately.</summary>
		/// <param name="mix">Proportion of color1 to use. Value between 0 and 1.</param>
		/// <returns>A mixture of color1 and color2.</returns>
		public static Color MixColors(Color color1, Color color2, double mix)
		{
			return Color.FromArgb((int)(color1.R * mix + color2.R * (1 - mix)),
			                      (int)(color1.G * mix + color2.G * (1 - mix)),
			                      (int)(color1.B * mix + color2.B * (1 - mix)));
		}

		public static string GetString(this XmlNode node, string name, string defaultValue = null)
		{
			var child = node.SelectSingleNode(name);
			return child == null ? defaultValue : child.InnerText;
		}

		public static double GetDouble(this XmlNode node, string name)
		{
			var child = node.SelectSingleNode(name);
			return child == null ? 0.0 : double.Parse(child.InnerText, CultureInfo.InvariantCulture);
		}

		public static decimal GetDecimal(this XmlNode node, string name)
		{
			var child = node.SelectSingleNode(name);
			return child == null ? 0 : decimal.Parse(child.InnerText, CultureInfo.InvariantCulture);
		}

		public static int GetInt(this XmlNode node, string name)
		{
			var child = node.SelectSingleNode(name);
			return child == null ? 0 : int.Parse(child.InnerText, CultureInfo.InvariantCulture);
		}

		public static void AppendNode(this XmlDocument doc, XmlNode parent, string name, string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				XmlNode node = doc.CreateElement(name);
				node.AppendChild(doc.CreateTextNode(value));
				parent.AppendChild(node);
			}
		}

		public static void AppendNode(this XmlDocument doc, XmlNode parent, string name, int value)
		{
			AppendNode(doc, parent, name, value.ToString());
		}

		public static void AppendNonZero(this XmlDocument doc, XmlNode parent, string name, double value)
		{
			if (value != 0)
				AppendNode(doc, parent, name, value.ToString());
		}

		/// <summary>Convert to an ordinal: 1 to 1st, 2 to 2nd, etc.</summary>
		public static string Ordinate(this int i)
		{
			var s = i.ToString();
			if (i % 10 == 1 && i % 100 != 11)
				return s + "st";
			if (i % 10 == 2 && i % 100 != 12)
				return s + "nd";
			if (i % 10 == 3 && i % 100 != 13)
				return s + "rd";
			return s + "th";
		}

		public static string Ordinate(this double d)
		{
			if (d - Math.Truncate(d) < 0.01)
				return Ordinate((int)d);
			else
				return d.ToString("n1");
		}

		public static bool Valid<T>(this IList<T> list, int i)
		{
			return 0 <= i && i < list.Count;
		}

		/// <summary>If s is a string containing whitespace-separated text, return the first word of s.</summary>
		public static string FirstWord(this string s)
		{
			if (string.IsNullOrWhiteSpace(s))
				return null;

			char[] whitespace = { ' ', '\t', '\r', '\n', '\v', '\f' };
			int i = s.IndexOfAny(whitespace);
			if (i == -1)
				return s;

			return s.Substring(0, i);
		}

		/// <summary>If s is a string containing whitespace-separated text, return the last word of s.</summary>
		public static string LastWord(this string s)
		{
			if (string.IsNullOrWhiteSpace(s))
				return null;

			char[] whitespace = { ' ', '\t', '\r', '\n', '\v', '\f' };
			int i = s.LastIndexOfAny(whitespace);
			if (i == -1)
				return s;

			return s.Substring(i + 1);
		}

		/// <summary>"one word" + "word two" -> "one word two". Also works with plurals: "one words" + "word two" -> "one words two".</summary>
		public static string JoinWithoutDuplicate(string one, string two)
		{
			string lastWordOne = one.LastWord();
			string firstWordTwo = two.FirstWord();
			if (lastWordOne == firstWordTwo)
				return one.Substring(0, one.Length - lastWordOne.Length) + two;
			else if (lastWordOne.Pluralise() == firstWordTwo)
				return one.Substring(0, one.Length - lastWordOne.Length) + two;
			else if (lastWordOne == firstWordTwo.Pluralise())
				return one + " " + two.Substring(firstWordTwo.Length + 1);
			else
				return one + " " + two;
		}

		public static string Pluralise(this string s)
		{
			if (string.IsNullOrEmpty(s) || s.Length == 1)
				return s + "s";

			char last = s[s.Length - 1];
			string lastTwo = s.Length < 2 ? s.Substring(s.Length - 1) : s.Substring(s.Length - 2);

			if (lastTwo == "is")
				return s.Substring(0, s.Length - 2) + "es";

			if (last == 's' || last == 'x' || last == 'z' || lastTwo == "ch" || lastTwo == "sh")
				return s + "es";

			if (last == 'y' && !(lastTwo == "ay" || lastTwo == "ey" || lastTwo == "iy" || lastTwo == "oy" || lastTwo == "uy"))
				return s.Substring(0, s.Length - 1) + "ies";

			return s + "s";
		}

		public static string CountPluralise(this string s, int i)
		{
			if (i == 1)
				return "1 " + s;
			else
				return i.ToString() + " " + Pluralise(s);
		}

		/// <summary>Convert a version string like "92.0.0" into three integers.</summary>
		public static bool ParseVersion(string version, out int major, out int minor, out int patch)
		{
			major = 0;
			minor = 0;
			patch = 0;

			string[] parts = version.Split('.');

			if (parts.Length < 3)
				return false;

			return int.TryParse(parts[0], out major) && int.TryParse(parts[1], out minor) && int.TryParse(parts[2], out patch);
		}

		public static bool IsNewerVersionAvailable(string latestVersion)
		{
			if (!ParseVersion(latestVersion, out int deployedMajor, out int deployedMinor, out int deployedPatch) ||
				!ParseVersion(Resources.version, out int currentMajor, out int currentMinor, out int currentPatch))
				return false;

			return deployedMajor > currentMajor ||
				(deployedMajor == currentMajor && deployedMinor > currentMinor) ||
				(deployedMajor == currentMajor && deployedMinor == currentMinor && deployedPatch > currentPatch);
		}

		// https://stackoverflow.com/questions/10795134/c-sharp-listview-column-text-size-is-not-the-same-on-each-computer/36974138#36974138
		public static void ScaleListViewColumns(ListView listview, float ratio)
		{
			foreach (ColumnHeader column in listview.Columns)
				column.Width = (int)Math.Round(column.Width * ratio);
		}
	}
}
