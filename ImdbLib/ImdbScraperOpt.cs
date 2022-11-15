using ImdbLib.Logic.Datasets.Structs;

namespace ImdbLib;

public enum ImdbScrapeDirection
{
	FromNewestToOldest,
	FromOldestToNewest
}

public class ImdbScraperOpt
{
	public string DataFolder { get; set; } = @"C:\caches\imdb";
	public TimeSpan DatasetRefreshPeriod { get; set; } = TimeSpan.FromDays(60);

	public Func<TitleBasicsRec, bool> TitleFilter { get; set; } = e =>
		!e.IsAdult &&
		e.Type == "movie" &&
		e.StartYear is >= 1970;

	public bool RefreshTitleFilter { get; set; } = false;

	public ImdbScrapeDirection ScrapeDirection { get; set; } = ImdbScrapeDirection.FromNewestToOldest;

	public int ScrapeBatchSize { get; set; } = 64;
	public int ScrapeParallelism { get; set; } = 4;

	public bool DbgUseSmallDatasets { get; set; } = false;
	public bool DbgAnalyzeOnInit { get; set; } = false;
	public int? DbgLimitTodoCount { get; set; } = null;

	internal static ImdbScraperOpt Build(Action<ImdbScraperOpt>? optFun)
	{
		var opt = new ImdbScraperOpt();
		optFun?.Invoke(opt);
		return opt;
	}
}