using ImdbLib.Logic.Scraping.Structs.Enums;
using ImdbLib.Logic.Scraping.StructsScrape;
using PowMaybe;

namespace ImdbLib.Logic.Scraping.Structs;

/*record MovieScrapeResult(
	int Id,
	StatusErr StatusErr,
	Maybe<ScrapeMovie> Movie
);*/

class MovieScrapeResult
{
	public int Id { get; }
	public StatusErr StatusErr { get; }
	public Maybe<ScrapeMovie> Movie { get; }

	private MovieScrapeResult(int id, StatusErr statusErr, Maybe<ScrapeMovie> movie)
	{
		Id = id;
		StatusErr = statusErr;
		Movie = movie;
	}

	public static Maybe<MovieScrapeResult> MakeError(int id, MovieStatus status, string? err) =>
		May.Some(
			new MovieScrapeResult(
				id,
				new StatusErr(status, err),
				May.None<ScrapeMovie>()
			)
		);

	public static Maybe<MovieScrapeResult> MakeSuccess(int id, ScrapeMovie movie) =>
		May.Some(
			new MovieScrapeResult(
				id,
				new StatusErr(MovieStatus.OK, null),
				May.Some(movie)
			)
		);
}