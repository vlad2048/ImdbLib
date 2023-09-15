using ImdbLib.Logic.Scraping;
using ImdbLib.Logic.Scraping.Structs.Enums;
using ImdbLib.Structs;
using ImdbLib.Utils;

namespace ImdbLib.Logic.RepoLogic;

class Repo
{
	public DataHolder Data { get; }
	public StateTracker Tracker { get; }

	public Repo(FileApi fileApi, bool disableLogging)
	{
		Data = new DataHolder(fileApi, disableLogging);
		Tracker = new StateTracker(fileApi);
	}

	public void Save(Holder holder)
	{
		Data.SaveScraped(holder.MoviesFound);
		Tracker.SaveStates(holder.Statuses);
	}
}