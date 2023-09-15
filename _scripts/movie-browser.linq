<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\ImdbLib\ImdbLib\bin\Debug\net7.0\ImdbLib.dll</Reference>
  <Namespace>ImdbLib</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>LINQPadHero.Controls.ListCtrl</Namespace>
  <Namespace>ImdbLib.Structs</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>LINQPadHero.UICode</Namespace>
  <Namespace>HtmlAgilityPack</Namespace>
  <Namespace>LINQPad.Controls.Core</Namespace>
  <Namespace>ImdbLib.Structs.Enums</Namespace>
</Query>

#load ".\libs\ui"
#load ".\libs\ui-filter"

public static DumpContainer reviewsDC = null!;

void Main()
{
	var useDev = true;
	var movies = MovieLoader.Load(opt => opt.DbgUseSmallDatasets = useDev);
	reviewsDC = new DumpContainer();
	reviewsDC.Style = "display:none";
	
	//MovieDisplay(movies[0]).Dump();return;
	
	var filter = new FieldFilter[]
	{
		FilterMaker.Text("Name", e => e.Name).D(D),
		FilterMaker.Text("Director", e => e.Director).D(D),
		FilterMaker.Range("Year", e => e.Year, e => $"{e}", movies.Min(e => e.Year), movies.Max(e => e.Year)).D(D),
		FilterMaker.Range("Rating", e => e.Rating, e => $"{((byte)e).Fmt()}", 0, 100).D(D),
		FilterMaker.Range("Reviews", e => e.TotalReviewCount, e => $"{e}", 0, 200).D(D),
		FilterMaker.TextIncExc("Country", e => e.Countries.JoinText()).D(D),
		FilterMaker.TextIncExc("Genre", e => GenreUtils.GetGenresDescription(e.Genres)).D(D),
	}
		.Combine();
	
	
	Util.VerticalRun(
		Util.HorizontalRun(true,
			new Button("Reset", _ => filter.Reset()),
			filter.WhenChanged.Select(_ =>
			{
				var cnt = movies.Where(filter.Predicate).ToArray().Length;
				return $"{cnt} / {movies.Length}";
			})
			.ToDC(D)			
		),
		filter.UI,
		new Span(
			filter.WhenChanged
				.Throttle(TimeSpan.FromMilliseconds(100))
				.Select(_ => movies.Where(filter.Predicate).ToArray())
				.DisplayList(MovieDisplay, D)
				.WithOrdering(e => e.Rating, e => e.TotalReviewCount, e => e.Name)
				.WithPaging(C.PageSize)
				.Build("grid"),
			reviewsDC
		).Horiz()
	).Dump();
}


public static class C
{
	/*
	public const bool ShowReviews = true;
	public const int PageSize = 4 * 2;
	public const int ThumbWidth = 350;
	public const int ThumbHeight = 250;
	public const int MovieImageWidth = 180;
	public static int MovieImageHeight => (int)(MovieImageWidth * 281.0 / 190.0);
	public const string FontDefault = "font-size: 12px;";
	public static C FontBig<C>(this C ctrl) where C : Control => ctrl.FontSize(20).FontBold();
	public static C FontBigBold<C>(this C ctrl) where C : Control => ctrl.FontSize(20);
	public static C FontMedium<C>(this C ctrl) where C : Control => ctrl.FontSize(14);
	public const int KeyWidth = 50;
	*/

	public const bool ShowReviews = true;
	public const int PageSize = 7 * 3;
	public const int ThumbWidth = 180;
	public const int ThumbHeight = 220;
	public const int MovieImageWidth = 80;
	public static int MovieImageHeight => (int)(MovieImageWidth * 281.0 / 190.0);
	public const string FontDefault = "font-size: 8px;";
	public static C FontBig<C>(this C ctrl) where C : Control => ctrl.FontSize(10).FontBold();
	public static C FontBigBold<C>(this C ctrl) where C : Control => ctrl.FontSize(10);
	public static C FontMedium<C>(this C ctrl) where C : Control => ctrl.FontSize(8);
	public const int KeyWidth = 35;
	

	public const string ColSoft = "#686868";
	public const string ColDirector = "#B3B82C";
	public const string ColRelease = "#B462B7";
	public const string ColStars = "#25A4A8";
	public const string ColDescription = "#9B9B9B";
	
	public const string ColReview = "#999";

}



void OnStart()
{
	Util.HtmlHead.AddCssLink("https://fonts.googleapis.com/css?family=Roboto");
	Util.HtmlHead.AddStyles($$"""
		:root {
			--pad: 5px;
		}
		body {
			font-family: 'Roboto';
			{{C.FontDefault}}
		}
		.grid {
			display: grid;
			flex: 1 1 auto;
			grid-template-columns: repeat(auto-fill, minmax({{C.ThumbWidth}}px, 1fr));
			grid-auto-rows: min-content;
			overflow-y: auto;
			padding: var(--pad);
			gap: var(--pad);
		}
		.thumb {
			background-color: #2B2C2E;
			border: 1px solid #707070;
			border-radius: 10px;
			color: white;
			/*aspect-ratio: {C.AspectRatio};*/
			height: {{C.ThumbHeight}}px;
			overflow: hidden;
			display: flex;
			flex-flow: column;
			row-gap: 5px;
			padding: 2px 5px;
		}
	""");
}
static Control MovieDisplay(Movie m)
{
	var movieImg = new Image(m.ImgUrl);
	movieImg.Width = C.MovieImageWidth;
	//movieImg.Styles["width"] = "50%";
	
	var div = new Div(
	
		new Span(
			new Span(
				new Span(Img("star.svg"), new Span(m.Rating.Fmt())).FontBigBold(),
				//new Span(m.Name).FontBigBold()
				new Hyperlink(m.Name, m.Id.MakeTitleUrl()).FontBigBold()
			).Space(),
			new Span($"({m.Year})").FontBig()
		).SpaceBetween(),
		
		new Span(GenreUtils.GetGenresDescription(m.Genres)),

		new Span(
			movieImg,
			C.ShowReviews switch
			{
				false => new Div(),
				true => new Div(
					m.Reviews.Select(r => (Control)new Div(
						new Span($"[{r.Score.Fmt()}]").WithCss("color", "#E8DF60"),
						new Span(r.Title).WithCol(C.ColReview)
					).Space())
					.Prepend(
						new Span(
							new Span("Reviews").WhenClick(() =>
							{
								reviewsDC.Style = "display:visible";
								reviewsDC.Content = Util.VerticalRun(
									new Button("Hide", _ =>
									{
										reviewsDC.ClearContent();
										reviewsDC.Style = "display:none";
									}),
									m.Reviews.Select(r => (Control)new Div(
										new Span($"[{r.Score.Fmt()}]").WithCss("color", "#E8DF60"),
										new Span(r.Title).WithCol(C.ColReview)
									).Space())
								);
							}),
							new Span($"({m.TotalReviewCount})")
						)
					).ToArray()
				)
				.WithCss("max-height", $"{C.MovieImageHeight}px")
				.WithCss("display", "flex")
				.WithCss("flex-direction", "column")
				.WithCss("row-gap", "3px")
				.WithCss("overflow", "hidden")
			}
		).SideBySide(),
		
		new Span(
			KeyVal("Director", m.Director, C.ColDirector),
			KeyVal("Release", $"{m.ReleaseDate:yyyy-MM-dd}", C.ColRelease)
		).SpaceBetween(),		
		KeyVal("Stars", m.Stars.JoinText(), C.ColStars),
		KeyVal("Country", $"{m.Countries.JoinText()} ({m.Languages.JoinText()})", C.ColSoft),
		
		new Span(m.Plot).FontMedium().WithCol(C.ColDescription)
		
	).WithCssClass("thumb");
	return div;
}

private static Span KeyVal(string key, string val, string valCol) => new Span(new Span(key).WithCol(C.ColSoft).WithWidth(C.KeyWidth), new Span(val).WithCol(valCol)).WithCss("display", "flex");

public static Control Img(string src)
{
	var file = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, "libs", src);
	var img = new Image(file);
	return img;
}
// => new HtmlNode("img").Attr("src", src);

public static class FmtExt
{
	public static string MakeTitleUrl(this int id) => $"https://www.imdb.com/title/{id.FmtId()}";
	private static string FmtId(this int id) => $"tt{id:D7}";
	public static string MakeReviewsUrl(int id) => $"https://www.imdb.com/title/{id.FmtId()}/reviews";
	public static string MakeMoreReviewsUrl(int id, string dataKey) => $"https://www.imdb.com/title/{id.FmtId()}/reviews/_ajax?paginationKey={dataKey}";

	public static string Fmt(this byte v) => $"{v/10.0:F1}";
	public static C FontSize<C>(this C ctrl, int size) where C : Control => ctrl.WithCss("font-size", $"{size}px");
	public static C FontBold<C>(this C ctrl) where C : Control => ctrl.WithCss("font-weight", "bold");
	public static C WithCol<C>(this C ctrl, string col) where C : Control => ctrl.WithCss("color", col);
	public static C WithWidth<C>(this C ctrl, int width) where C : Control => ctrl.WithCss("width", $"{width}px");
	
	public static string JoinText(this IEnumerable<string> source, string sep = ",") => string.Join(sep, source);
}














