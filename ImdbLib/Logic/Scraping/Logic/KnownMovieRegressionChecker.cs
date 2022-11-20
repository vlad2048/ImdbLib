using PowMaybe;

namespace ImdbLib.Logic.Scraping.Logic;

static class KnownMovieRegressionChecker
{
	private const int PulpFictionId = 110912;

	private static readonly string ExpDirector = "Quentin Tarantino";
	private static readonly string ExpPlot = "The lives of two mob hitmen, a boxer, a gangster and his wife, and a pair of diner bandits intertwine in four tales of violence and redemption.";
	private const int ExpMinRating = 80;
	private const int ExpMinReviewCount = 30;
	private static readonly string[] ExpCountries = { "United States" };
	private static readonly string[] ExpLanguages = { "English", "Spanish", "French" };
	private static readonly string[] ExpStars = { "John Travolta", "Uma Thurman", "Samuel L. Jackson" };

	public static void Check()
	{
		Exception Err(string msg) => new ArgumentException($"TestMovie: {msg}");

		var mayScrape = HtmlScraper.Scrape(PulpFictionId, Pagers.Html, CancellationToken.None).Result;
		if (mayScrape.IsNone(out var scrape))
			throw Err("Failed to download");

		if (scrape.Movie.IsNone(out var movie))
			throw Err($"Failed to scrape: {scrape.StatusErr}");

		var title = movie.Title;
		var reviews = movie.Reviews;
		
		if (title.Director != ExpDirector)
			throw Err("Parsed wrong director");
		
		if (title.Plot != ExpPlot)
			throw Err("Parsed wrong plot");

		if (title.Rating < ExpMinRating)
			throw Err("Parsed wrong rating");

		if (!CheckArr(title.Countries, ExpCountries))
			throw Err("Parsed wrong countries");

		if (!CheckArr(title.Languages, ExpLanguages))
			throw Err("Parsed wrong languages");

		if (!CheckArr(title.Stars, ExpStars))
			throw Err("Parsed wrong stars");

		if (reviews.TotalReviewCount < ExpMinReviewCount)
			throw Err("Parsed wrong reviews");
	}

	private static bool CheckArr(string[] actArr, string[] expArr) =>
		actArr.Length == expArr.Length &&
		actArr.Zip(expArr).All(t => t.First == t.Second);
}