using ImdbLib.Logic.Datasets.TsvLoading.Structs;
using ImdbLib.Utils.Exts;

namespace ImdbLib.Logic.Datasets.TsvLoading.Utils;

class ColLogger
{
	private readonly ICol col;
	private readonly int rowCount;

	public ColLogger(ICol col, int rowCount)
	{
		this.col = col;
		this.rowCount = rowCount;
	}

	public void LogStart() => LTitle($"[{col.Index}] " + col.Name.TruncPad(16) + $"({col.ColType})");
	public void Log(string key, int val) => L(key, $"{val}");
	public void Log(string key, string val) => L(key, val);
	public void LogPerc(string key, int cnt) => L(key, $"{cnt}/{rowCount} ({Perc(cnt)})");
	public void LogEnd() { }

	public string Perc(int cnt) => rowCount switch
	{
		0 => "_",
		_ => $"{cnt * 100 / rowCount:F2}%"
	};

	private static void L(string s) => Console.WriteLine($"  {s}");

	private static void LTitle(string s)
	{
		L(s);
		//L(new string('=', s.Length));
	}

	private static void L(string key, string val)
	{
		Console.Write($"    {key}:".TruncPad(32));
		Console.WriteLine(val);
	}
}