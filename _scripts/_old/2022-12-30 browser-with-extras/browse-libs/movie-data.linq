<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeLib\bin\Debug\net7.0\DynaServeLib.dll</Reference>
  <Reference>C:\Dev_Nuget\Libs\ImdbLib\ImdbLib\bin\Debug\net7.0\ImdbLib.dll</Reference>
  <Namespace>ImdbLib.Structs</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>static DynaServeLib.Nodes.Ctrls</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>DynaServeLib.Nodes</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>ImdbLib.Structs.Enums</Namespace>
  <Namespace>ImdbLib</Namespace>
</Query>

#load ".\base-ui"
#load ".\user-data"


void Main()
{
	var comboFilters = Var.Make(ComboFiltersArr.Empty).D(D);
	var movieVars = new MovieVars(comboFilters, opt =>
	{
		opt.UseTestData = true;
	}).D(D);
	movieVars.AllMovies.Length.Dump();
}

public class MovieVarsOpt {

	public bool UseTestData { get; set; }
	public bool ShowOnlyOneMovie { get; set; }
	public int MoviesPerPage { get; set; } = 64;
	
	private MovieVarsOpt() {}
	internal static MovieVarsOpt Build(Action<MovieVarsOpt>? optFun)
	{
		var opt = new MovieVarsOpt();
		optFun?.Invoke(opt);
		return opt;
	}
}

public class MovieVars : IDisposable
{
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();
	
	private readonly int moviesPerPage;
	private readonly IRwVar<int> pageIndex;
	private readonly IRoVar<Movie[]> filteredMovies;
	
	public Movie[] AllMovies { get; }
	public IRoVar<Movie[]> Movies { get; }
	public IRwVar<int> PageIndex => pageIndex;	
	public IRoVar<int> PageCount { get; }
	public IRoVar<int> FilteredMoviesCount { get; }
	
	public MovieVars(IRoVar<ComboFiltersArr> comboFilters, Action<MovieVarsOpt>? optFun)
	{
		var opt = MovieVarsOpt.Build(optFun);
		moviesPerPage = opt.MoviesPerPage;
		pageIndex = Var.Make(0).D(d);
		
		AllMovies = MovieUtils.LoadMovies(opt.UseTestData);
		if (opt.ShowOnlyOneMovie) AllMovies = AllMovies.Take(1).ToArray();
		filteredMovies = Var.Expr(() => comboFilters.V.Apply(AllMovies).ToArray());
		FilteredMoviesCount = Var.Expr(() => filteredMovies.V.Length);
		Movies = Var.Expr(() => filteredMovies.V.Skip(pageIndex.V * moviesPerPage).Take(moviesPerPage).ToArray());
		
		PageCount = Var.Expr(() => GetPageCnt(filteredMovies.V.Length, moviesPerPage));
		
		Observable.Merge(
			filteredMovies.ToUnit(),
			PageIndex.ToUnit()
		)
			.Where(_ => PageIndex.V >= PageCount.V)
			.Subscribe(_ =>
			{
				pageIndex.V = Math.Max(0, PageCount.V - 1);
			}).D(d);
	}

	public IRoVar<bool> CanDecPage() => Var.Expr(() => PageIndex.V > 0);
	public IRoVar<bool> CanIncPage() => Var.Expr(() => PageIndex.V < filteredMovies.V.Length - 1);
	//public void DecPage() { if (!CanDecPage().V) throw new ArgumentException(); pageIndex.V--; }
	//public void IncPage() { if (!CanIncPage().V) throw new ArgumentException(); pageIndex.V++; }
	
	private static int GetPageCnt(int filteredMovieCount, int moviesPerPage) =>
		filteredMovieCount switch
		{
			0 => 1,
			_ => Math.Max(
				1,
				((filteredMovieCount - 1) / moviesPerPage) + 1
			)
		};
}




static class MovieUtils
{
	public static Movie[] LoadMovies(bool useTestData) => useTestData switch
	{
		false => LoadProdMovies(),
		true => LoadTestMovies()
	};
	
	private static Movie[] LoadProdMovies()
	{
		using var imdb = new ImdbScraper(opt =>
		{
			opt.DbgUseSmallDatasets = false;
			opt.DbgLimitTodoCount = null;
			opt.ScrapeParallelism = 4;
			opt.ScrapeBatchSize = 64;
			opt.LoadOnly = true;
		});
		imdb.Init().Wait();
		return imdb.Movies.ToArray();
	}
	
	private static Movie[] LoadTestMovies() => JsonUtils.Load<Movie[]>(DataUtils.TestMoviesFile, GenTestMovies);
	
	private static Movie[] GenTestMovies()
	{
		var movies = LoadProdMovies();
		var list = new List<Movie>();
		var yearMin = movies.Min(e => e.Year);
		var yearMax = movies.Max(e => e.Year);
		for (var year = yearMin; year <= yearMax; year++)
		{
			var yearMovies = movies.Where(e => e.Year == year).ToArray().Shuffle().Take(100).ToArray();
			list.AddRange(yearMovies);
		}
		return list.ToArray();
	}
}