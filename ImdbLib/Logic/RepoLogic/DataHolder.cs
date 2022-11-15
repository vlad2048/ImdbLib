using System.Collections.Concurrent;
using System.Diagnostics;
using ImdbLib.Structs;
using ImdbLib.Utils;
using ImdbLib.Utils.Exts;

namespace ImdbLib.Logic.RepoLogic;

class DataHolder
{
	private readonly FileApi fileApi;

	private readonly ConcurrentBag<Movie> movies = new();

	public IEnumerable<Movie> Movies => movies;

	public DataHolder(FileApi fileApi)
	{
		this.fileApi = fileApi;
	}

	public void Load()
	{
		L("Loading");
		movies.AddRange(
			from file in fileApi.GetScrapeFiles()
			from movie in LoadFile(file)
			select movie
		);
		LDone();
	}

	public void SaveScraped(IEnumerable<Movie> moviesAddSource)
	{
		var moviesAdd = moviesAddSource.ToArray();
		if (moviesAdd.Length == 0) return;
		movies.AddRange(moviesAdd);
		moviesAdd
			.GroupBy(e => e.Year)
			.ForEach(grp => AddToFile(fileApi.GetScrapeFile(grp.Key), grp));
	}


	private static Movie[] LoadFile(string file) => file.LoadJson<Movie[]>();

	private static void AddToFile(string file, IEnumerable<Movie> moviesAdd)
	{
		var moviesExist = file.LoadJson(() => new List<Movie>());
		file.SaveJson(moviesExist.Concat(moviesAdd));
	}

	
	private static Stopwatch? watch;
	private static void L(string op)
	{
		if (watch != null) throw new ArgumentException();
		watch = Stopwatch.StartNew();
		var prefix = $@"[{DateTime.Now:HH:mm:ss}]";
		Console.Write($"{prefix} {op.TruncPad(40)} ... ");
	}

	private static void LDone()
	{
		if (watch == null) throw new ArgumentException();
		var time = $@"{watch.Elapsed:mm\:ss}";
		watch = null;
		Console.WriteLine($"done ({time})");
	}
}