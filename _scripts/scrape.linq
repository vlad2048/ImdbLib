<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\ImdbLib\ImdbLib\bin\Debug\net6.0\ImdbLib.dll</Reference>
  <Namespace>ImdbLib</Namespace>
  <Namespace>ImdbLib.Logic.Datasets.Structs</Namespace>
  <Namespace>ImdbLib.Utils.Exts</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>ImdbLib.Logic.Scraping.Structs</Namespace>
  <Namespace>ImdbLib.Logic.Scraping.Structs.Enums</Namespace>
</Query>

async Task Main()
{
	Util.NewProcess = true;
	
	using var imdb = new ImdbScraper(opt =>
	{
		opt.DbgUseSmallDatasets = false;
		opt.DbgLimitTodoCount = null;
		opt.ScrapeParallelism = 4;
		opt.ScrapeBatchSize = 64;
	});
	await imdb.Init();
	
	imdb.Start();
	Util.ReadLine("Press enter to stop");
	imdb.Stop();
}




void FixStates()
{
	var statesPrev = @"C:\caches\imdb_prev\scraping\title-states.json".LoadJson<Dictionary<int, St>>();
	statesPrev.GroupBy(e => e.Value.Status).Select(e => new
	{
		Status = e.Key,
		Cnt = e.Count()
	}).Dump();
	var fileNext = @"C:\caches\imdb\scraping\title-states.json";
	var statesNext = fileNext.LoadJson<Dictionary<int, TitleState>>();
	foreach (var (id, st) in statesPrev)
	{
		if (st.Status >= 2 && !statesNext.ContainsKey(id))
		{
			var e = (MovieStatus)st.Status;
			var stNext = new TitleState(e + 8, $"{e}", DateTime.Now);
			statesNext[id] = stNext;
		}
	}
	fileNext.SaveJson(statesNext);
}
record St(int Status, DateTime LastUpdate);
