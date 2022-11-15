using ImdbLib.Logic.Datasets.TsvLoading.Structs;
using ImdbLib.Utils.Exts;

namespace ImdbLib.Logic.Datasets.TsvLoading.Utils;

static class ColUtils
{
	public static void AnalyzeData(object?[][] data, ICol[] cols, string title)
	{
		static void LBig(string s)
		{
			var str = $"* {s} *";
			var pad = new string('*', str.Length);
			Console.WriteLine(pad);
			Console.WriteLine(str);
			Console.WriteLine(pad);
		}

		LBig($"{title} - rows:{data.Length}");
		foreach (var col in cols)
		{
			var vals = data.SelectToArray(e => e[col.Index]);
			var logger = new ColLogger(col, data.Length);
			logger.LogStart();
			col.Analyze(vals, logger);
			logger.LogEnd();
		}

		Console.WriteLine();
	}

	public static string ParseString(string s) => s switch
	{
		@"\N" => string.Empty,
		_ => s
	};

	public static int? ParseNumberOpt(string s) => s switch
	{
		@"\N" => null,
		_ => int.Parse(s)
	};

	public static bool ParseBool(string s) => s switch
	{
		@"\N" => false, // rare, only in Title.Akas -> IsOriginalTitle
		_ => int.Parse(s) != 0
	};

	public static string[] ParseCategoryArray(string s) => s
		.Split(',')
		.Where(e => e != @"\N")
		.ToArray();
}