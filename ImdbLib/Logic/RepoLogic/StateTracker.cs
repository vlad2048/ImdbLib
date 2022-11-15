using System.Collections.Concurrent;
using ImdbLib.Logic.Datasets.Structs;
using ImdbLib.Logic.Scraping.Structs;
using ImdbLib.Logic.Scraping.Structs.Enums;
using ImdbLib.Utils;
using ImdbLib.Utils.Exts;

namespace ImdbLib.Logic.RepoLogic;

class StateTracker
{
	private readonly FileApi fileApi;
	private readonly ConcurrentDictionary<int, TitleState> map;
	private int totalTitleCount;

	public StateTracker(FileApi fileApi)
	{
		this.fileApi = fileApi;
		map = fileApi.GetTitleStatesFile().LoadJson(() => new ConcurrentDictionary<int, TitleState>());
	}

	public void Init(int pTotalTitleCount) => totalTitleCount = pTotalTitleCount;

	public IEnumerable<TitleNfo> FilterTitlesToDo(TitleNfo[] titles) => titles
		.OrderBy(e => e.Year)
		.Where(e => !map.ContainsKey(e.Id));

	public ScrapeState GetScrapeState(bool running, double flow) => new(
		running,
		totalTitleCount,
		map.Values.Count(e => e.Status == MovieStatus.OK),
		map.Values.Count(e => e.Status != MovieStatus.OK),
		flow
	);

	public void SaveStates(IReadOnlyDictionary<int, StatusErr> statuses)
	{
		foreach (var (titleId, statusErr) in statuses)
			map[titleId] = new TitleState(statusErr.Status, statusErr.Err, DateTime.Now);
		Save();
	}

	private void Save() => fileApi.GetTitleStatesFile().SaveJson(map);
}