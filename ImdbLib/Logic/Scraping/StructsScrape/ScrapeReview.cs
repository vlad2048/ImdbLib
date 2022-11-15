using ImdbLib.Structs;

namespace ImdbLib.Logic.Scraping.StructsScrape;

record ScrapeReview(
	byte Score,
	string Title
)
{
	public MovieReview ToMovieReview() => new(Score, Title);
}