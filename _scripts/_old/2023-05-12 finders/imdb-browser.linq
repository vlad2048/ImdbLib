<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\ImdbLib\ImdbLib\bin\Debug\net7.0\ImdbLib.dll</Reference>
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <Namespace>Img = LINQPad.Controls.Image</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>LINQPadExtras.PageServing</Namespace>
  <Namespace>ImdbLib.Structs</Namespace>
  <Namespace>ImdbLib</Namespace>
  <Namespace>LINQPadExtras.Utils.Exts</Namespace>
  <Namespace>PowBasics.ColorCode</Namespace>
  <Namespace>LINQPadExtras.PageServing.Utils.HtmlUtils</Namespace>
  <Namespace>LINQPadExtras.Utils</Namespace>
</Query>

void Main1() { Util.ReadLine(); Main(); }

void Main()
{
	Css.Init();
	var items = Enumerable.Range(0, 30).SelectToArray(e => $"item_{e}");
	
	var c = new CtrlMaker();
	
	c.DivVert().Root(
		c.DivHoriz().SetBackColor(Cst.ColHeader).WithChildren(
			new Span("header1"),
			new Span("header2")
		),
		
		c.MovieScroller(
			Movies
				//.Take(10)
				.Select(e => c.MoviePanel(
					c.DivHoriz().Set("align-items", "center").WithChildren(
						new Img(PathUtils.GetImage("star.svg")),
						new Span($"{e.Rating.FmtScore()}").Fnt(2, true).PadRight(5),
						new Span(e.Name).Fnt(2, true)
							.Set("flex", "1 1 auto")
							.Set("text-align", "center")
					),
					new Img(e.ImgUrl).Set("overflow", "hidden").Set("object-fit", "contain"),
					new Div(new Span(e.Plot)).Set("font-size", "8px")
				))
		)
	).Dump();
	
	//HtmlDocUtils.SavePage(@"C:\tmp\inspect");
}

internal static class Cst
{
	public const string ColBackground = Col_Gray0;
	public const string ColHeader = Col_Gray1;
	public const string ColMovieBackground = Col_Gray1;
	
	public const string Col_Gray0 = "#001";
	public const string Col_Gray1 = "#223";
	
	private static readonly string[] fontSizes = {
		"12px",
		"14px",
		"18px",
	};

	public static C Fnt<C>(this C ctrl, int sz, bool bold = false) where C : Control => ctrl
		.Set("font-size", fontSizes[sz])
		.SetIf(bold, "font-weight", "bold");
	
}

static class PathUtils
{
	public static string GetImage(string name) =>
		Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, "art", name);
}

static class ImdbStyleExt
{
	public static string FmtScore(this byte score) => $"{score / 10.0:F1}";
	public static C MargRight<C>(this C ctrl, int val) where C : Control => ctrl.Set("margin-right", $"{val}px");
	public static C PadRight<C>(this C ctrl, int val) where C : Control => ctrl.Set("padding-right", $"{val}px");
	public static Div MovieScroller(this CtrlMaker c, IEnumerable<Control> children) => c.MovieScroller(children.ToArray());
	public static Div MovieScroller(this CtrlMaker c, params Control[] children) => c.Div()
		.SetCss("div-scrollwrap", $"""
			flex: 1 1 auto;
			display: grid;
			grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
			grid-auto-rows: min-content;
			overflow-y: auto;
			padding: {c.Opt.Gap}px;
			gap: {c.Opt.Gap}px;
		""")
		.WithChildren(children);

	public static Div MoviePanel(this CtrlMaker c, params Control[] children) =>
		new Div().SetCss("imdb-movie", $"""
			background-color: {CssVar.Get(Cst.ColMovieBackground)};
			//border: 1px solid red;
			aspect-ratio: 2 / 2;
			padding: {c.Opt.Gap}px;
			overflow: hidden;
			display: flex;
			flex-flow: column;
		""").WithChildren(
			new Div().SetCss("imdb-movie-inner", """
				flex: 1 1 auto;
				display: flex;
				flex-direction: column;
				align-items: flex-start;
				overflow: hidden;
			""").WithChildren(children)
		);
	
	public static Div StyleVert(this Div div) => div
		.Set("display", "flex")
		.Set("flex-flow", "column")
		.Set("align-items", "stretch");
	
	public static C Push<C>(this C ctrl) where C : Control => ctrl.Set("margin-right", "auto !important");
}

static class StyleExt
{
	public static C RelBase<C>(this C div) where C : Control => div.Set("position", "relative");
	
	public static C RelPos<C>(this C div, int x, int y) where C : Control => div
		.Set("position", "relative")
		.Set("left", $"{x}px")
		.Set("top", $"{y}px");
}



internal class CtrlMakerOpt
{
	public bool UseBg { get; set; } = false;
	public int Gap { get; set; } = 5;
	internal static CtrlMakerOpt Build(Action<CtrlMakerOpt>? optFun = null) { var opt = new CtrlMakerOpt(); optFun?.Invoke(opt); return opt; }
}

internal class CtrlMaker
{
	public CtrlMakerOpt Opt { get; }
	
	public CtrlMaker(Action<CtrlMakerOpt>? optFun = null)
	{
		Opt = CtrlMakerOpt.Build(optFun);
	}

	public Div Div() => new Div().Bg(Opt.UseBg);
	
	public Div DivVert(params Control[] children) => Div()
		.SetCss("div-vert", $"""
			display: flex;
			flex-flow: column;
			padding: {Opt.Gap}px;
			gap: {Opt.Gap}px;
		""")
		.WithChildren(children);
	
	public Div DivHoriz(params Control[] children) => Div()
		.SetCss("div-horiz", $"""
			display: flex;
			flex-flow: row;
			padding: {Opt.Gap}px;
			gap: {Opt.Gap}px;
		""")
		.WithChildren(children);
	
	public Div DivScrollableWrapPanel(IEnumerable<Control> children) => DivScrollableWrapPanel(children.ToArray());
	public Div DivScrollableWrapPanel(params Control[] children) => Div()
		.SetCss("div-scrollwrap", $"""
			flex: 1 1 auto;
			display: grid;
			grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
			grid-template-rows: 40px 40px 40px;
			overflow-y: auto;
			padding: {Opt.Gap}px;
			row-gap: 50px;
			//grid-gap: {Opt.Gap}px;
			
		""")
		.WithChildren(children);
}

static class CtrlMakerExt
{
	public static Div Root(this Div ctrl, params Control[] children)
	{
		ctrl.SetCss("div-root", """
			height: 100%;
		""");
		ctrl.Children.AddRange(children);
		return ctrl;
	}
	
	public static Div WithChildren(this Div ctrl, params Control[] children)
	{
		ctrl.Children.AddRange(children);
		return ctrl;
	}
	
	public static Div WrapWith<C>(this C ctrl, Func<Div, Div> fun) where C : Control =>
		fun(new Div()).WithChildren(ctrl);
	
	public static Div FlexGrow(this Div ctrl) => ctrl.Set("flex", "1 1 auto");
	
	public static C SetGap<C>(this C ctrl, int gap) where C : Control =>
		ctrl.Set("padding", $"{gap}px").Set("gap", $"{gap}px");

	public static Div SetCss(this Div div, string clsName, string css)
	{
		var clsPrev = div.CssClass;
		div.CssClass = clsPrev switch
		{
			null => clsName,
			not null => $"{clsPrev} {clsName}"
		};
		Util.HtmlHead.AddStyles($$"""
			.{{clsName}} {
			{{css}}
			}
			"""
		);
		return div;
	}
}


static class Styler
{
	private static readonly string[] colors = ColorUtils.MakePalette(11, 7).SelectToArray(ToHexCol);
	private static int colorIdx;
	private static string GetCol() { var col = colors[colorIdx++]; colorIdx = colorIdx % colors.Length; return col; }
	private static string ToHexCol(System.Drawing.Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
	//private static void AddStyle(string css) => Util.HtmlHead.AddStyles(css);

	public static C Bg<C>(this C ctrl, bool apply) where C : Control => apply switch
	{
		true => ctrl.Set("background-color", GetCol()),
		false => ctrl
	};
}


static class Css
{
	public static void Init()
	{
		Util.HtmlHead.AddStyles($$"""
			*, *::before, *::after {
				box-sizing: border-box;
			}
			* {
				margin: 0 !important;
				padding: 0;
				border: 0;
			}
			html, body {
				height: 100%;
			}
			body > div {
				height: 100%;
			}
			body {
				background-color: {{Cst.ColBackground}};
			}
			""");
		
		LINQPadServer.AddLINQPadOnlyCss("""
			body {
				padding: 2px;
			}
			""");
	}
}


static Movie[] Movies => ImdbUtils.movies.Value;


static class ImdbUtils
{
	public static readonly Lazy<Movie[]> movies = new(GetMovies);
	private static Movie[] GetMovies()
	{
		using var imdb = new ImdbScraper(opt =>
		{
			opt.LoadOnly = true;
			opt.DbgUseSmallDatasets = true;
			opt.DisableLogging = true;
		});
		imdb.Init().Wait();
		return imdb.Movies.ToArray();
	}
}