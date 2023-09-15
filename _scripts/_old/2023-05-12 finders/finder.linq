<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\ImdbLib\ImdbLib\bin\Debug\net7.0\ImdbLib.dll</Reference>
  <Namespace>ImdbLib</Namespace>
  <Namespace>ImdbLib.Logic.Scraping.Utils</Namespace>
  <Namespace>ImdbLib.Structs</Namespace>
  <Namespace>ImdbLib.Structs.Enums</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
</Query>

static readonly Lazy<Movie[]> movies = new(GetMovies);
static Movie[] Movies => movies.Value;

void Main()
{
	/*var m = Movies.Single(e => e.Id == 22010428);
	m.Genres.Dump();
	m.HasGenres(Genre.Action).Dump();
	m.HasGenres(Genre.Documentary).Dump();
	m.HasGenres(Genre.Music).Dump();
	return;*/
	/*Movies.Where(e =>
		e.Name.ToLowerInvariant().Contains("survive") &&
		e.Name.ToLowerInvariant().Contains("style")
	).Dump();
	return;*/
	
	/*var list = (
		from movie in Movies
		where !exclude.Contains(movie.Id)
		//where movie.IsNotCountries("India", "Pakistan", "Iran", "Serbia", "Egypt", "Saudi Arabia")
		//where movie.Countries.Length <= 2
		//where !movie.HasGenres(Genre.Documentary, Genre.Biography)

		//where movie.IsCountries("France")
		where movie.TotalReviewCount >= 60
		where movie.GetUserScore() >= 30
		where movie.Rating >= 65
		//where movie.Reviews.Count(e => e.Score >= 90) >= 3
		//where movie.HasGenres(Genre.Action, Genre.Adventure, Genre.Crime, Genre.Thriller, Genre.Fantasy)
		where movie.HasGenres(Genre.SciFi)
		where movie.IsCountries("India")
		where movie.ReleaseDate >= new DateTime(1990, 1, 1)// && movie.ReleaseDate <= new DateTime(2015, 1, 1)
		
		select movie
	)
		//.OrderByDescending(e => e.ReleaseDate.Year).ThenByDescending(e => e.Rating).ThenByDescending(e => e.Reviews.Length)
		.OrderByDescending(e => e.Rating)
		.ToArray();

	list
		.Take(600)
		.Dump($"total:{list.Length}");*/
		
	(
		from movie in Movies
		select movie
	)
		.OrderByDescending(e => e.ReleaseDate)
		.Take(100)
		.Dump();
}


static class MovieUtils
{
	public static void Show(this IEnumerable<Movie> movies)
	{
		
	}
	
	
}


private static HashSet<int> exclude = new()
{
	6751668,8613070,2762506,7286456,4154796,4633694,11657662,8574252,10530176,
	4154756,8772262,5501104,8367814,7979580,9426210
};

/*

ToWatch
=======
https://www.imdb.com/title/tt4028464		The Innocents
https://www.imdb.com/title/tt12298506		Black Box
https://www.imdb.com/title/tt3228774		Cruella
https://www.imdb.com/title/tt6708668		Black Crab
https://www.imdb.com/title/tt4364194		The Peanut Butter Falcon
https://www.imdb.com/title/tt6613878		Clara


Good ones I've seen
===================
https://www.imdb.com/title/tt12474056		New Order
https://www.imdb.com/title/tt9484998		Palm Springs
https://www.imdb.com/title/tt8781414		Freaks
https://www.imdb.com/title/tt11252440		Psycho Goreman
https://www.imdb.com/title/tt4357394		Tau
*/

static Movie[] GetMovies()
{
	using var imdb = new ImdbScraper(opt =>
	{
		opt.DbgUseSmallDatasets = false;
	});
	imdb.Init().Wait();
	return imdb.Movies.ToArray();
}

public static object ToDump(object o) => o switch
{
	Movie e => new
	{
		Link = Util.VerticalRun(
			new Hyperlinq(ImdbUrlUtils.MakeTitleUrl(e.Id), e.Name),
			e.MakePlotDiv(),
			new Hyperlink("Copy", _ => Clipboard.SetText($"{ImdbUrlUtils.MakeTitleUrl(e.Id)}\t\t{e.Name}"))
		),
		Rating = $"{e.Rating / 10.0:F1}",
		Date = $"{e.ReleaseDate:yyyy-MM-dd}",
		Genres = e.Genres,
		ReviewCnt = e.TotalReviewCount,
		Country = e.MakeCountriesDiv(),
		Reviews = e.MakeReviewsCtrl(),
	},
	_ => o
};

static class DivUtils
{
	public static Div MakePlotDiv(this Movie e) =>
		$"{e.Id}\n{e.Plot}"
			.MakeDiv();
	
	public static Div MakeCountriesDiv(this Movie e) =>
		e.Countries
			.JoinLines()
			.MakeDiv();

	public static object MakeReviewsCtrl(this Movie e)
	{
		const int showReviewCount = 5;
		var lines = e.Reviews
			.Shuffle(null)
			.Select(f => $"{f.Score.FmtScore()} {f.Title}")
			.ToArray();
		return (lines.Length <= 5) switch
		{
			true => lines.JoinLines().MakeDiv(),
			false => Util.VerticalRun(
				lines.Take(showReviewCount).JoinLines().MakeDiv(),
				new Lazy<Div>(lines.Skip(showReviewCount).JoinLines().MakeDiv())
			)
		};
	}


	public static Div MakeReviewsDiv(this Movie e) =>
		e.Reviews
			.Shuffle(null)
			.Take(10)
			.Select(f => $"{f.Score.FmtScore()} {f.Title}")
			.JoinLines()
			.MakeDiv();

	private static Div MakeDiv(this string str)
	{
		var div = new Div(new Span(str));
		div.Styles["font-family"] = "Consolas";
		div.Styles["font-size"] = "12px";
		div.Styles["background-color"] = "#030526";
		div.Styles["color"] = "#32FEC4";
		return div;
	}
	
	private static string JoinLines(this IEnumerable<string> source) => string.Join(Environment.NewLine, source);
}


public static class MovieExt
{
	public static double GetUserScore(this Movie movie) => movie.Reviews.Average(e => e.Score);
	
	public static bool HasGenres(this Movie movie, params Genre[] genres) => genres.Any(genre => movie.Genres.HasFlag(genre));
	
	public static bool IsNotCountries(this Movie movie, params string[] countries) => countries.All(e => !movie.Countries.Contains(e));
	public static bool IsCountries(this Movie movie, params string[] countries) => !movie.IsNotCountries(countries);
	
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
	public static string FmtScore(this byte score) => score switch
	{
		< 100 => $"[ {score/10.0:F1}]",
		_ => $"[{score/10.0:F1}]",
	};
}