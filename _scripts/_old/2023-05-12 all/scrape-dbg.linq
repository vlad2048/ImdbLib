<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\ImdbLib\ImdbLib\bin\Debug\net7.0\ImdbLib.dll</Reference>
  <NuGetReference>HtmlAgilityPack</NuGetReference>
  <NuGetReference>PowMaybeErr</NuGetReference>
  <Namespace>HtmlAgilityPack</Namespace>
  <Namespace>ImdbLib.Logic.Datasets.Structs</Namespace>
  <Namespace>ImdbLib.Logic.Scraping.Logic</Namespace>
  <Namespace>ImdbLib.Logic.Scraping.Structs</Namespace>
  <Namespace>ImdbLib.Logic.Scraping.Structs.Enums</Namespace>
  <Namespace>ImdbLib.Logic.Scraping.StructsScrape</Namespace>
  <Namespace>ImdbLib.Logic.Scraping.Utils</Namespace>
  <Namespace>ImdbLib.Utils</Namespace>
  <Namespace>ImdbLib.Utils.Exts</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowMaybeErr</Namespace>
  <Namespace>RestSharp</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

static readonly IPagerWithSave FilePager = Pagers.File(@"C:\Dev_Nuget\Libs\ImdbLib\_infos\imdb-pages");
static readonly Lazy<int[]> TitleIdsLazy = new(() => @"C:\caches\imdb\datasets\titles.json".LoadJson<TitleNfo[]>().SelectToArray(e => e.Id));
const string TitleStatesFile = @"C:\caches\imdb\scraping\title-states.json";
static readonly Lazy<Dictionary<int, TitleState>> TitleStatesLazy = new(() => TitleStatesFile.LoadJson<Dictionary<int, TitleState>>());
static int[] TitleIds => TitleIdsLazy.Value;
static Dictionary<int, TitleState> TitleStates => TitleStatesLazy.Value;




int Main()
{
	//KnownMovieRegressionChecker.Check();return 0;
	
	
	// ***********************************
	// * Check the Prod scraping process *
	// ***********************************
	// Show unknown issues
	// -------------------
	//return ProdCheckIssues();

	// Reset the states for a set of movies
	// ------------------------------------
	//var cutoff = new DateTime(2022, 11, 17);
	//var idsToRemove = TitleStates.Where(e => e.Value.LastUpdate >= cutoff && e.Value.Status != MovieStatus.OK).SelectToArray(e => e.Key);
	//return ProdClearTitleStates(idsToRemove);

	// Show the latest scraped movies
	// ------------------------------
	//return ProdCheckLatestAdded();


	// ***********************************************************
	// * Save a movie html pages to disk and parse it from there *
	// ***********************************************************
	//var id = 110912;
	//SaveId(id);
	//LoadId(id, FilePager).Dump();
	
	/*var cutoff = new DateTime(2022, 11, 17);
	var ids = TitleStates.Where(e => e.Value.LastUpdate >= cutoff && e.Value.Status != MovieStatus.OK).Select(e => e.Key).Shuffle(null);
	ids.Length.Dump();
	if (ids.Length > 0)
	{
		var id = ids[0];
		//SaveId(id);
		//LoadId(id, FilePager).Dump();
		LoadId(id, Pagers.Html).Dump();
	}
	return 0;*/


	// *********************************************
	// * Scrape some random movies and show errors *
	// *********************************************
	/*TitleIds
		.Shuffle(4)
		.Take(100)
		.Select(id => LoadId(id, Pagers.Html))
		.PrintDot()
		.ShowErrors()
		.Dump();*/

	// **********************
	// * Investigate errors *
	// **********************
	//return new[] { 6839852 }.Investigate();

	return 0;
}

int ProdCheckIssues()
{
	var states = TitleStates
		.Where(e => e.Value.Status == MovieStatus.DownloadError || e.Value.Status == MovieStatus.UnknownError)
		.ToArray();
		
	states
		.Select(e => new
		{
			Id = e.Key,
			Url = ImdbUrlUtils.MakeTitleUrl(e.Key),
			Status = new StatusErr(e.Value.Status, e.Value.Err)
		})
		.Dump();

	states.Select(e => $"{e.Key}").JoinText().Dump();

	return 0;
}

int ProdClearTitleStates(params int[] ids)
{
	var isAnyOk = ids.Select(e => TitleStates[e].Status).Any(e => e == MovieStatus.OK);
	if (isAnyOk) throw new ArgumentException("Cannot clear the state of a movie with Status = OK (I need to sync the movie json file to do this)");

	foreach (var id in ids)
		TitleStates.Remove(id);
	TitleStatesFile.SaveJson(TitleStates);
	return 0;
}

int ProdCheckLatestAdded()
{
	TitleStates.OrderByDescending(e => e.Value.LastUpdate).Take(20).Dump();
	return 0;
}

void SaveId(int id)
{
	var pages = PageReader.ReadPages(id, Pagers.Html, CancellationToken.None).Result!.Ensure();
	FilePager.SaveTitle(id, pages.TitleStr);
	FilePager.SaveReviews(id, pages.ReviewsStr);
	if (pages.MoreReviewsStr.IsSome(out var moreReviewsStr))
		FilePager.SaveMoreReviews(id, moreReviewsStr);
}

internal static MovieScrapeResult LoadId(int id, IPager pager) => Scrape(id, pager);



static MovieScrapeResult Scrape(int id, IPager pager) => HtmlScraper.Scrape(id, pager, CancellationToken.None).Result!.Ensure();




static class EnumExt
{
	public static int Investigate(this IEnumerable<int> ids, int? idx = null)
	{
		ids
			.SelectIdx(idx)
			.Select(id => LoadId(id, Pagers.Html))
			.Select(e => new { Url = ImdbUrlUtils.MakeTitleUrl(e.Id), Res = e })
			.Dump();
		return 0;
	}

	private static IEnumerable<T> SelectIdx<T>(this IEnumerable<T> source, int? idx) => idx switch
	{
		not null => source.Skip(idx.Value).Take(1),
		null => source
	};
	
	public static IEnumerable<object> ShowErrors(this IEnumerable<MovieScrapeResult> source)
	{
		var list = source
			.Where(e => e.StatusErr.Status == MovieStatus.DownloadError || e.StatusErr.Status == MovieStatus.UnknownError)
			.Select(e => new
			{
				Id = e.Id,
				Url = ImdbUrlUtils.MakeTitleUrl(e.Id),
				Status = e.StatusErr
			})
			.ToList();
		Console.WriteLine();
		string.Join(", ", list.Select(e => $"{e.Id}")).Dump();
		return list;
	}
	
	public static IEnumerable<T> PrintDot<T>(this IEnumerable<T> source) => source.Select(e => { Console.Write("."); return e; });

	public static T[] Shuffle<T>(this IEnumerable<T> source, int? seed)
	{
		var rnd = seed switch
		{
			not null => new Random(seed.Value),
			null => new Random((int)DateTime.Now.Ticks)
		};
		var array = source.ToArray();
		var n = array.Length;
		for (var i = 0; i < (n - 1); i++)
		{
			var r = i + rnd.Next(n - i);
			T t = array[r];
			array[r] = array[i];
			array[i] = t;
		}
		return array;
	}
}



static class HtmlScraper
{
	// **********
	// * Public *
	// **********
	public static async Task<Maybe<MovieScrapeResult>> Scrape(int id, IPager pager, CancellationToken cancelToken)
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
