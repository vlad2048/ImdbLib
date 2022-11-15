using ImdbLib.Logic.Datasets.Structs;

namespace ImdbLib.Logic.Scraping.Utils;

static class FilterExt
{
	public static IEnumerable<TitleNfo> OrderByScrapeDirection(this IEnumerable<TitleNfo> source, ImdbScrapeDirection scrapeDirection) =>
		scrapeDirection switch
		{
			ImdbScrapeDirection.FromNewestToOldest => source.OrderByDescending(e => e.Year),
			ImdbScrapeDirection.FromOldestToNewest => source.OrderBy(e => e.Year),
			_ => throw new ArgumentException()
		};

	public static IEnumerable<TitleNfo> TakeOpt(this IEnumerable<TitleNfo> source, int? count) => count switch
	{
		not null => source.Take(count.Value),
		null => source
	};
}