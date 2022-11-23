using ImdbLib.Logic.Datasets;
using ImdbLib.Logic.RepoLogic;
using ImdbLib.Logic.Scraping;
using ImdbLib.Logic.Scraping.Structs;
using ImdbLib.Structs;
using ImdbLib.Utils;
using PowRxVar;

namespace ImdbLib;

public class ImdbScraper : IDisposable
{
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();

	private readonly ImdbScraperOpt opt;
	private readonly FileApi fileApi;
	private readonly Repo repo;
	private readonly Scraper scraper;

	public IEnumerable<Movie> Movies => repo.Data.Movies;
	public IObservable<ScrapeState> WhenUpdate => scraper.WhenUpdate;

	public ImdbScraper(Action<ImdbScraperOpt>? optFun = null)
	{
		opt = ImdbScraperOpt.Build(optFun);
		fileApi = new FileApi(opt.DataFolder, opt.DbgUseSmallDatasets);
		repo = new Repo(fileApi);
		scraper = new Scraper(
			repo,
			opt.ScrapeDirection,
			opt.FetchTimeout,
			opt.ScrapeParallelism,
			opt.ScrapeBatchSize,
			opt.DbgLimitTodoCount
		).D(d);
	}

	public async Task Init()
	{
		await DatasetGetter.Init(
			fileApi,
			opt.DatasetRefreshPeriod,
			opt.TitleFilter,
			opt.RefreshTitleFilter
		);
		if (opt.DbgAnalyzeOnInit)
			DatasetGetter.AnalyzeDatasets(fileApi);

		repo.Data.Load();

		var titlesAll = DatasetGetter.LoadTitles(fileApi);
		repo.Tracker.Init(titlesAll.Length);
		scraper.Init(titlesAll);
	}

	public void Start() => scraper.Start();

	public void Stop() => scraper.Stop();
}