using HtmlAgilityPack;
using ImdbLib.Logic.Scraping.Structs;
using ImdbLib.Logic.Scraping.Structs.Enums;
using ImdbLib.Logic.Scraping.StructsScrape;
using ImdbLib.Logic.Scraping.Utils;
using ImdbLib.Utils;
using ImdbLib.Utils.Exts;
using PowMaybe;
using PowMaybeErr;

namespace ImdbLib.Logic.Scraping.Logic;

static class HtmlScraper
{
	// **********
	// * Public *
	// **********
	public static async Task<Maybe<MovieScrapeResult>> Scrape(
		int id,
		IPager pager,
		CancellationToken cancelToken
	)
	{
		var mayPages = await PageReader.ReadPages(id, pager, cancelToken);
		if (mayPages == null) return May.None<MovieScrapeResult>();
		if (mayPages.IsNone(out var pages, out var pagesErr))
			return MovieScrapeResult.MakeError(id, MovieStatus.DownloadError, pagesErr);

		var nfo =
			from title in ScrapeTitle(pages.TitleStr)
			from reviews in ScrapeReviews(pages.ReviewsStr, pages.MoreReviewsStr)
			select new ScrapeMovie(title, reviews);

		var (scrapeStatus, err) = MovieStatusUtils.ExtractStatus(nfo);

		return scrapeStatus switch
		{
			MovieStatus.OK => MovieScrapeResult.MakeSuccess(id, nfo.Ensure()),
			_ => MovieScrapeResult.MakeError(id, scrapeStatus, err)
		};
	}


	// *********
	// * Title *
	// *********
	private static MaybeErr<ScrapeTitle> ScrapeTitle(string titleStr)
	{
		// Director								Lorcan Finnegan
		// Star									Imogen Poots | Danielle Ryan | Molly McCann
		string MkPrincipalXPath(string key) => $"(//li[ @data-testid='title-pc-principal-credit' and ./*[self::span or self::a or self::button][contains(text(), '{key}')]])[1]/div/ul/li";

		// title-details-releasedate			March 27, 2020 (United Kingdom)
		// title-details-origin					Ireland | Belgium | Denmark
		// title-details-languages				English
		string MkDetailsXPath(string key) => $"//li[@data-testid='{key}']/div/ul/li";

		Func<string, string> MkErr(string name) => e => $"Cannot find '{name}' ({e})";

		return
			from root in Html.LoadFromString(titleStr)
			from _ in root.CheckRateLimitDefense()
			from __ in root.CheckUptoDate()
			from ___ in root.CheckNotInDevelopment()
			from ____ in root.CheckPageIsOnServer()

			from plot in root.GetText("//span[@data-testid='plot-xl']")
				.MapError(MkErr("Plot"))

			from director in root.GetText(MkPrincipalXPath("Director"))
				.WithError(MovieStatus.DirectorNotFound)

			from stars in root.GetTextArray(MkPrincipalXPath("Star"))
				.FailWith(Array.Empty<string>())

			from releaseDateStr in root.GetText(MkDetailsXPath("title-details-releasedate"))
				.WithError(MovieStatus.ReleaseDateNotFound)
			from releaseDate in ParseReleaseDate(releaseDateStr)

			from countries in root.GetTextArray(MkDetailsXPath("title-details-origin"))
				.FailWith(Array.Empty<string>())

			from languages in root.GetTextArray(MkDetailsXPath("title-details-languages"))
				.FailWith(Array.Empty<string>())

			from scoreDecimal in root.GetTextAs<decimal>("//div[@data-testid='hero-rating-bar__aggregate-rating__score']/span")
				.WithError(MovieStatus.RatingNotFound)
			let score = (byte)(scoreDecimal * 10)

			from imgUrl in root.GetAttr("src", "//img[@class='ipc-image']")
				.WithError(MovieStatus.ImageNotFound)

			select new ScrapeTitle(
				score,
				imgUrl,
				plot,
				director,
				stars,
				releaseDate,
				countries,
				languages
			);
	}

	private static MaybeErr<bool> CheckRateLimitDefense(this HtmlNode root) =>
		(
			from node in root.QueryNode("//div[@class='error-page-quote-bubble-text']")
			from text in node.GetText()
			where text.StartsWith("Error")
			select true
		)
		.NegateMaybe($"{MovieStatus.RateLimitDefense}");

	private static MaybeErr<bool> CheckUptoDate(this HtmlNode root) =>
		(
			from node in root.QueryNode("//div[@data-testid='tm-box-up-date']")
			from text in node.GetText()
			where text.StartsWith("Expected ")
			select true
		)
		.NegateMaybe($"{MovieStatus.NotReleased}");

	private static MaybeErr<bool> CheckNotInDevelopment(this HtmlNode root) =>
		(
			from node in root.QueryNode("//div[@data-testid='tm-box-up-title']")
			from text in node.GetText()
			where text == "In Development"
			select true
		)
		.NegateMaybe($"{MovieStatus.InDevelopment}");

	private static MaybeErr<bool> CheckPageIsOnServer(this HtmlNode root) =>
		(
			from node in root.QueryNode("//span[contains(text(), 'The requested URL was not found on our server')]")
			select true
		)
		.NegateMaybe($"{MovieStatus.PageNotFoundOnServer}");

	private static MaybeErr<DateTime> ParseReleaseDate(string s)
	{
		Maybe<DateTime> TryFormat(string fmt)
		{
			var parts = s.Split('(');
			if (parts.Length == 0) return May.None<DateTime>();
			var str = parts[0].Trim();
			if (!DateTime.TryParseExact(str, fmt, null, System.Globalization.DateTimeStyles.None, out var date)) return May.None<DateTime>();
			return May.Some(date);
		}
		Func<Maybe<DateTime>> MkFun(string fmt) => () => TryFormat(fmt);

		return
			new[]
				{
					"MMMM d, yyyy",
					"MMMM yyyy",
					"yyyy",
				}
				.Select(MkFun)
				.AggregateMay()
				.ToMaybeErrWithMsg($"Failed to parse ReleaseDate: '{s}'");
	}


	// ***********
	// * Reviews *
	// ***********
	private static MaybeErr<ScrapeReviews> ScrapeReviews(string reviewsStr, Maybe<string> moreReviewsStr) =>
		from root in Html.LoadFromString(reviewsStr)
		from totalReviewCountStr in root.GetText("//section[@class='article']//div[@class='header']/div/span")
		from totalReviewCount in totalReviewCountStr.ExtractReviewCount()
		from _ in totalReviewCount.CheckTotalReviewCountNonZero()

		let mayDataKey = root.GetAttr("data-key", "//div[@class='load-more-data']")

		let reviews = mayDataKey.IsSome() switch
		{
			true => root.ReadReviews()
				.Concat(
					Html.LoadFromString(moreReviewsStr.Ensure()).ReadReviews()
				)
				.ToArray(),
			false => root.ReadReviews()
		}
		from __ in reviews.CheckReviewsCountNonZero()
		select new ScrapeReviews(totalReviewCount, reviews);



	private static ScrapeReview[] ReadReviews(this MaybeErr<HtmlNode> mayRoot) => mayRoot.IsSome(out var root) switch
	{
		true => root!.ReadReviews(),
		false => Array.Empty<ScrapeReview>()
	};

	private static ScrapeReview[] ReadReviews(this HtmlNode root) =>
		(
			from reviewNode in root.QueryNodes("//div[@class='lister-list']/div").FailWithValue(Array.Empty<HtmlNode>())
			select ExtractReview(reviewNode)
		)
		.WhereSome()
		.ToArray();

	private static MaybeErr<bool> CheckTotalReviewCountNonZero(this int reviewCount) => Err.MakeIf(reviewCount == 0, MovieStatus.ZeroReviews);
	private static MaybeErr<bool> CheckReviewsCountNonZero(this ScrapeReview[] reviews) => Err.MakeIf(reviews.Length == 0, MovieStatus.ZeroReviews);

	private static MaybeErr<ScrapeReview> ExtractReview(this HtmlNode node) =>
		from scoreDecimal in node.GetTextAs<decimal>(".//span[@class='rating-other-user-rating']/span[1]")
		from score in scoreDecimal.ConvertScore()
		from title in node.GetText(".//a[@class='title']")
		select new ScrapeReview(score, title.Trim());

	private static MaybeErr<int> ExtractReviewCount(this string str)
	{
		var parts = str.Split(' ');
		if (parts.Length != 2) return MayErr.None<int>("parts.Length != 2");
		var cntStr = parts[0].Replace(",", "");
		if (!int.TryParse(cntStr, out var cnt)) return MayErr.None<int>("failed to parse review count");
		return MayErr.Some(cnt);
	}
}
