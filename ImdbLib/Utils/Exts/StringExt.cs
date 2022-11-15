namespace ImdbLib.Utils.Exts;

static class StringExt
{
	public static string TruncPad(this string str, int lng) => str switch
	{
		null => throw new ArgumentException(),
		_ when str.Length < lng => str.PadRight(lng),
		_ when str.Length > lng => str[..lng],
		_ => str
	};

	public static string JoinText(this IEnumerable<string> source) => string.Join(",", source);
}