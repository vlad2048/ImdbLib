using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using ImdbLib.Logic.Datasets.Structs;
using ImdbLib.Logic.RepoLogic;
using ImdbLib.Logic.Scraping.Logic;
using ImdbLib.Logic.Scraping.Structs;
using ImdbLib.Logic.Scraping.Structs.Enums;
using ImdbLib.Structs;
using ImdbLib.Utils;
using ImdbLib.Utils.Concurrency;
using PowMaybe;
using PowMaybeErr;

namespace ImdbLib.Logic.Scraping;

class Holder
{
	public FlowMeter FlowMeter { get; } = new(FlowMeter.Unit.Minute);
	public TitleNfo[] TitlesToDo { get; }
	public ConcurrentBag<TitleNfo> TitlesDone { get; } = new();
	public ConcurrentDictionary<int, StatusErr> Statuses { get; } = new();
	public ConcurrentBag<Movie> MoviesFound { get; } = new();
	public Holder(IEnumerable<TitleNfo> titlesToDo)
	{
		TitlesToDo = titlesToDo.ToArray();
	}
}

static class BatchLogic
{
	public static Holder Start(
		IEnumerable<TitleNfo> titles
	)
	{
		Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
		return new Holder(titles);
	}

	public static async Task<bool> Run(
		Holder holder,
		int parallelism,
		CancellationToken cancelToken,
		Action interrupt
	)
	{
		var wasInterrupted = false;
		try
		{
			await Looper.Loop(
				holder.TitlesToDo,
				parallelism,
				cancelToken,
				async title => await ProcessMovie(title, holder, cancelToken, () =>
				{
					wasInterrupted = true;
					interrupt();
				})
			);
		}
		catch (OperationCanceledException)
		{
			Console.WriteLine("Operation Cancelled");
		}

		return wasInterrupted;
	}

	public static ScrapeState Finish(
		List<TitleNfo> allTitles,
		Holder holder,
		Repo repo
	)
	{
		var flow = holder.FlowMeter.Measure();
		var watch = Stopwatch.StartNew();

		repo.Save(holder);

		foreach (var titleDone in holder.TitlesDone)
			allTitles.Remove(titleDone);
		var state = repo.Tracker.GetScrapeState(true, flow);
		var sb = new StringBuilder();
		sb.Append($" found {holder.MoviesFound.Count} in {watch.Elapsed.TotalSeconds:F2}sec.");
		sb.Append($" {state.GetConsoleMessage()}");
		Console.WriteLine(sb.ToString());
		return state;
	}



	private static async Task ProcessMovie(
		TitleNfo title,
		Holder holder,
		CancellationToken cancelToken,
		Action interrupt
	)
	{
		var mayScrape = await HtmlScraper.Scrape(title.Id, Pagers.Html, cancelToken);
		if (mayScrape.IsNone(out var scrape))
		{
			if (!cancelToken.IsCancellationRequested) throw new ArgumentException("This should only happen when cancelled");
			return;
		}

		if (scrape.StatusErr.Status == MovieStatus.RateLimitDefense)
		{
			interrupt();
			return;
		}

		holder.Statuses[title.Id] = scrape.StatusErr;
		if (scrape.Movie.IsSome(out var scrapeMovie))
		{
			if (scrape.StatusErr.Status != MovieStatus.OK) throw new ArgumentException();
			var movie = Movie.Make(title, scrapeMovie.Title, scrapeMovie.Reviews);
			holder.MoviesFound.Add(movie);
		}
		holder.TitlesDone.Add(title);
		holder.FlowMeter.Increment();

		Console.Write('.');
	}
}