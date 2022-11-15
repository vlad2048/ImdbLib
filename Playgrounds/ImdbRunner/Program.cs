using ImdbLib;
using ImdbLib.Logic.Scraping.Logic;

namespace ImdbRunner;

static class Program
{
	public static async Task Main(string[] args)
	{
		await RunScraper(args);
		//await ScrapeSingle(62847);
	}


	private static async Task RunScraper(string[] args)
	{
		using var imdb = new ImdbScraper(opt =>
		{
			opt.DbgUseSmallDatasets = false;
			opt.DbgLimitTodoCount = null;
			opt.ScrapeParallelism = 4;
			opt.ScrapeBatchSize = 64;
			SetOpt(val => opt.ScrapeParallelism = val, args, 0);
			SetOpt(val => opt.ScrapeBatchSize = val, args, 1);
		});
		await imdb.Init();
		imdb.Start();

		L("Press");
		L("  1 to start");
		L("  2 to stop");
		L("  Q to quit");
		while (true)
		{
			var key = Console.ReadKey().Key;
			switch (key)
			{
				case ConsoleKey.D1:
					L("Start");
					imdb.Start();
					break;

				case ConsoleKey.D2:
					L("Stop");
					imdb.Stop();
					break;

				case ConsoleKey.Q:
					L("Quit");
					imdb.Stop();
					Thread.Sleep(TimeSpan.FromSeconds(1));
					return;
			}
		}
	}

	private static void SetOpt(Action<int> setter, string[] args, int idx)
	{
		if (idx < args.Length && int.TryParse(args[idx], out var val))
			setter(val);
	}



	

	private static async Task ScrapeSingle(int id)
	{
		var pager = new HtmlPager();
		var cancelSource = new CancellationTokenSource();
		var cancelToken = cancelSource.Token;

		var movie = await HtmlScraper.Scrape(id, pager, cancelToken);

		var abc = 123;
	}



	private static void L(string s) => Console.WriteLine(s);
}