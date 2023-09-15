<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeLib\bin\Debug\net7.0\DynaServeLib.dll</Reference>
  <Reference>C:\Dev_Nuget\Libs\ImdbLib\ImdbLib\bin\Debug\net7.0\ImdbLib.dll</Reference>
  <Namespace>DynaServeLib</Namespace>
  <Namespace>static DynaServeLib.Nodes.Ctrls</Namespace>
  <Namespace>DynaServeLib.Nodes</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>ImdbLib.Structs</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
</Query>

#load "..\..\..\browse-libs\base-ui"
#load "..\..\..\browse-libs\user-data"
#load "..\..\..\browse-libs\movie-data"
#load "..\..\1_layout\layout"

void Main()
{
	var userData = new UserDataVars(testData: true).D(D);
	var movieData = new MovieVars(userData.ComboFilters, movieOpt =>
	{
		movieOpt.UseTestData = true;
		movieOpt.ShowOnlyOneMovie = true;
	}).D(D);
	Serv.Start(
		MkOpt(
			"1_layout",
			"1_movies-thumbs"
		),

		UI_Layout.Make(
			headerNodes: UI_MoviesThumbs.MakeHeader(movieData),
			mainNodes: UI_MoviesThumbs.MakeThumbs(movieData.Movies)
		)
	);
}




public static class UI_MoviesThumbs
{
	public static HtmlNode[] MakeHeader(MovieVars v) => new[]
	{
		Div().Wrap(
			Observable.Merge(v.FilteredMoviesCount.ToUnit(), v.Movies.ToUnit()),
			() => new [] { Div().Txt(v.GetMovieNfo()) }
		),
		Div("pager").Wrap(
			Observable.Merge(v.PageIndex.ToUnit(), v.PageCount.ToUnit()),
			() => new []
			{
				Div().Txt($"page:{v.PageIndex.V + 1}/{v.PageCount.V}"),
				Ctrls.RangeSlider(v.PageIndex, 0, v.PageCount.V - 1).Cls("pager-range"),
				IconBtn("fa-solid fa-caret-left", () => v.PageIndex.V--).EnableWhen(v.CanDecPage()),
				IconBtn("fa-solid fa-caret-right", () => v.PageIndex.V++).EnableWhen(v.CanIncPage())
			}
		)
	};

	private static string GetMovieNfo(this MovieVars v) => $"(movies all:{v.AllMovies.Length} filtered:{v.FilteredMoviesCount.V} paged:{v.Movies.V.Length})";
	
	public static HtmlNode MakeThumbs(IRoVar<Movie[]> movies) =>
		Div("moviesthumbs").Wrap(
			Var.Expr(() => movies.V.Select(movie =>
				MakeThumb(movie)
			))
		);
	
	private static HtmlNode MakeThumb(Movie movie) =>
		Div("moviethumb").Wrap(Div("moviethumb-inner").Wrap(
		
			Div("moviethumb-top").Wrap(
				Div("moviethumb-img-wrap").Wrap(
					Img(movie.ImgUrl),
					Div("moviethumb-img-stars").Wrap(
						Img("image/star.svg"),
						HtmlNode.MkTxt(movie.Rating.FmtScore())
					)
				),
				Div("moviethumb-reviews").Wrap(
					movie.Reviews.Select(review =>
						Div("moviethumb-review").Txt($"{review.Score.FmtScore()} {review.Title}")
					)
				)
			),
			Div("moviethumb-bottom").Wrap(
				Div("moviethumb-name").Txt(movie.Name),
				Div("moviethumb-description").Txt(movie.Plot),
				Div("moviethumb-keyval-wrap").Wrap(
					KeyVal("Director", movie.Director),
					KeyVal("Stars", movie.Stars.JoinText(",")),
					KeyVal("Reviews", movie.Reviews.Length == 0 ? "_" : $"{movie.Reviews.Average(e => e.Score) / 10.0:F1} ({movie.TotalReviewCount})")
				)
			)
			
		));
	
	private static HtmlNode KeyVal(string key, string val) =>
		Div("moviethumb-keyval").Wrap(
			Div("moviethumb-key").Txt(key),
			Div("moviethumb-val").Txt(val)
		);
		
}