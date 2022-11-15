using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ImdbLib.Logic.Datasets.Structs;
using ImdbLib.Logic.RepoLogic;
using ImdbLib.Logic.Scraping.Structs;
using ImdbLib.Logic.Scraping.Utils;
using PowRxVar;

namespace ImdbLib.Logic.Scraping;


class Scraper : IDisposable
{
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();

	private readonly ISubject<TitleNfo[]> whenInit = new Subject<TitleNfo[]>();
	private readonly ISubject<Unit> whenStart = new Subject<Unit>();
	private readonly ISubject<Unit> whenStop = new Subject<Unit>();
	private readonly ISubject<ScrapeState> whenUpdate = new Subject<ScrapeState>();
	private IObservable<TitleNfo[]> WhenInit => whenInit.AsObservable();
	private IObservable<Unit> WhenStart => whenStart.AsObservable();
	private IObservable<Unit> WhenStop => whenStop.AsObservable();


	public void Init(TitleNfo[] allTitles) => whenInit.OnNext(allTitles);
	public void Start() => whenStart.OnNext(Unit.Default);
	public void Stop() => whenStop.OnNext(Unit.Default);
	public IObservable<ScrapeState> WhenUpdate => whenUpdate.AsObservable();


	public Scraper(
		Repo repo,
		ImdbScrapeDirection scrapeDirection,
		int parallelism,
		int batchSize,
		int? dbgLimitTodoCount
	)
	{
		var titlesTodo = new List<TitleNfo>();

		WhenInit.Subscribe(titlesAll =>
		{
			titlesTodo.Clear();
			titlesTodo.AddRange(
				repo.Tracker.FilterTitlesToDo(titlesAll)
					.OrderByScrapeDirection(scrapeDirection)
					.TakeOpt(dbgLimitTodoCount)
			);
		}).D(d);

		ScraperRxUtils.HookInterruptableOperation(WhenStart, WhenStop, async (cancelToken, interrupt) =>
		{
			whenUpdate.OnNext(repo.Tracker.GetScrapeState(true, 0));
			while (titlesTodo.Any() && !cancelToken.IsCancellationRequested)
			{
				var holder = BatchLogic.Start(titlesTodo.Take(batchSize));
				var wasInterrupted = await BatchLogic.Run(holder, parallelism, cancelToken, interrupt);
				var state = BatchLogic.Finish(titlesTodo, holder, repo);
				whenUpdate.OnNext(state);

				if (wasInterrupted)
				{
					Console.WriteLine();
					Console.WriteLine("***************************************");
					Console.WriteLine("* Rate limit defense detected -> Wait *");
					Console.WriteLine("***************************************");
					Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss}");
					Console.WriteLine();
					var delay = TimeSpan.FromMinutes(10);
					Console.Write($"Waiting: {delay} ... ");
					await Task.Delay(delay);
					Console.WriteLine("Done");
				}
			}
			whenUpdate.OnNext(repo.Tracker.GetScrapeState(false, 0));
		}).D(d);
	}
}
