<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeExtrasLib\bin\Debug\net7.0\DynaServeExtrasLib.dll</Reference>
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeLib\bin\Debug\net7.0\DynaServeLib.dll</Reference>
  <Reference>C:\Dev_Nuget\Libs\ImdbLib\ImdbLib\bin\Debug\net7.0\ImdbLib.dll</Reference>
  <Namespace>DynaServeExtrasLib.Utils</Namespace>
  <Namespace>DynaServeLib.Nodes</Namespace>
  <Namespace>ImdbLib.Structs</Namespace>
  <Namespace>ImdbLib.Structs.Enums</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>static DynaServeLib.Nodes.Ctrls</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>DynaServeExtrasLib.Components.EditListLogic.Structs</Namespace>
  <Namespace>DynaServeExtrasLib.Components.EditListLogic.Utils</Namespace>
</Query>

#load ".\base-ui"

void Main()
{
	
}


public static class UserDataFileApi
{
	public static UserData Load() => SafeJsonUtils.Load<UserData>(DataUtils.UserDataFile, () => UserData.Empty, IntegrityCheck);
	public static void Save(UserData e) => SafeJsonUtils.Save(DataUtils.UserDataFile, e);
	
	private static bool IntegrityCheck(UserData e)
	{
		// Combos cannot reference non existing filters
		var filterNames = e.Filters.Select(e => e.Name).ToHashSet();
		if (e.Combos.Any(combo => combo.Filters.Any(filterName => !filterNames.Contains(filterName)))) return false;
		// SelCombo cannot reference a non existing combo
		if (e.SelCombo != null && e.Combos.All(combo => combo.Name != e.SelCombo)) return false;
		return true;
	}
}


public record UserData(
	Filter[] Filters,
	Combo[] Combos,
	string? SelCombo
)
{
	public static readonly UserData Empty = new(
		Array.Empty<Filter>(),
		Array.Empty<Combo>(),
		null
	);
	public static readonly UserData Test = new(
		new Filter[]
		{
			new("Main", new[] {
				PropFilter.Make(PropFilterType.YearMin),
				PropFilter.Make(PropFilterType.GenreInc),
				PropFilter.Make(PropFilterType.ImdbRatingMin),
				PropFilter.Make(PropFilterType.TitlePlotSearch),
				PropFilter.Make(PropFilterType.YearMin),
				PropFilter.Make(PropFilterType.GenreInc),
				PropFilter.Make(PropFilterType.ImdbRatingMin),
				PropFilter.Make(PropFilterType.TitlePlotSearch),
				PropFilter.Make(PropFilterType.ImdbRatingMin),
				PropFilter.Make(PropFilterType.TitlePlotSearch),
			}),
			new("Plus", new PropFilter[] {
				PropFilter.Make(PropFilterType.ReviewCountMax),
				PropFilter.Make(PropFilterType.YearMax),
				PropFilter.Make(PropFilterType.ActorSearch),
			}),
			new("Other", new PropFilter[] {
				PropFilter.Make(PropFilterType.ReviewCountMin),
				PropFilter.Make(PropFilterType.ReviewRatingMax),
				PropFilter.Make(PropFilterType.DirectorSearch),
			}),
		},
		new Combo[]
		{
			new("Good", new[] { "Main", "Plus" }),
			new("Bad", new[] { "Plus", "Other" }),
			new("Very Bad", new string[] { }),
		},
		"Bad"
	);
}


public record Combo(
	string Name,
	string[] Filters
)
{
	public override string ToString() => Name;
}


public record Filter(
	string Name,
	PropFilter[] PropFilters
)
{
	public override string ToString() => Name;
}



public enum PropFilterType
{
	YearMin,
	YearMax,
	GenreInc,
	GenreExc,
	ImdbRatingMin,
	ImdbRatingMax,
	ReviewRatingMin,
	ReviewRatingMax,
	ReviewCountMin,
	ReviewCountMax,
	TitleSearch,
	TitlePlotSearch,
	DirectorSearch,
	ActorSearch,
}


public record PropFilter
{
	public bool Enabled { get; set; } = true;
	public PropFilterType Type { get; set; }
	public int? YearVal { get; set; }
	public Genre? GenreVal { get; set; }
	public int? ImdbRatingVal { get; set; }
	public int? ReviewRatingVal { get; set; }
	public int? ReviewCountVal { get; set; }
	public string? TitleSearch { get; set; }
	public string? TitlePlotSearch { get; set; }
	public string? DirectorSearch { get; set; }
	public string? ActorSearch { get; set; }
	
	public string Name => Type switch
	{
		PropFilterType.YearMin => "Year (min)",
		PropFilterType.YearMax => "Year (max)",
		PropFilterType.GenreInc => "Genre (inc)",
		PropFilterType.GenreExc => "Genre (exc)",
		PropFilterType.ImdbRatingMin => "ImdbRating (min)",
		PropFilterType.ImdbRatingMax => "ImdbRating (max)",
		PropFilterType.ReviewRatingMin => "ReviewRating (min)",
		PropFilterType.ReviewRatingMax => "ReviewRating (max)",
		PropFilterType.ReviewCountMin => "ReviewCount (min)",
		PropFilterType.ReviewCountMax => "ReviewCount (max)",
		PropFilterType.TitleSearch => "TitleSearch",
		PropFilterType.TitlePlotSearch => "TitlePlotSearch",
		PropFilterType.DirectorSearch => "DirectorSearch",
		PropFilterType.ActorSearch => "ActorSearch",
		_ => throw new ArgumentException(),
	};
	
	public static PropFilter Make(PropFilterType type) => type switch
	{
		PropFilterType.YearMin => new() {
			Type = type,
			YearVal = 1980,
		},
		PropFilterType.YearMax => new() {
			Type = type,
			YearVal = DateTime.Now.Year,
		},
		PropFilterType.GenreInc => new()
		{
			Type = type,
			GenreVal = 0,
		},
		PropFilterType.GenreExc => new()
		{
			Type = type,
			GenreVal = 0,
		},
		PropFilterType.ImdbRatingMin => new()
		{
			Type = type,
			ImdbRatingVal = 0,
		},
		PropFilterType.ImdbRatingMax => new()
		{
			Type = type,
			ImdbRatingVal = 10,
		},
		PropFilterType.ReviewRatingMin => new()
		{
			Type = type,
			ReviewRatingVal = 0,
		},
		PropFilterType.ReviewRatingMax => new()
		{
			Type = type,
			ReviewRatingVal = 10,
		},
		PropFilterType.ReviewCountMin => new()
		{
			Type = type,
			ReviewCountVal = 0,
		},
		PropFilterType.ReviewCountMax => new()
		{
			Type = type,
			ReviewCountVal = 1000,
		},
		PropFilterType.TitleSearch => new()
		{
			Type = type,
			TitleSearch = "",
		},
		PropFilterType.TitlePlotSearch => new()
		{
			Type = type,
			TitlePlotSearch = "",
		},
		PropFilterType.DirectorSearch => new()
		{
			Type = type,
			DirectorSearch = "",
		},
		PropFilterType.ActorSearch => new()
		{
			Type = type,
			ActorSearch = "",
		},
		_ => throw new ArgumentException(),
	};
	
	public IEnumerable<Movie> Filter(IEnumerable<Movie> source) => Enabled switch
	{
		false => source,
		true => Type switch
		{
			PropFilterType.YearMin => source.Where(e => e.Year >= YearVal!.Value),
			PropFilterType.YearMax => source.Where(e => e.Year <= YearVal!.Value),
			PropFilterType.GenreInc => source.Where(e => (e.Genres & GenreVal) != 0),
			PropFilterType.GenreExc => source.Where(e => (e.Genres & GenreVal) == 0),
			PropFilterType.ImdbRatingMin => source.Where(e => e.Rating >= ImdbRatingVal!.Value),
			PropFilterType.ImdbRatingMax => source.Where(e => e.Rating <= ImdbRatingVal!.Value),
			PropFilterType.ReviewRatingMin => source.Where(e => e.GetReviewRating() >= ReviewRatingVal!.Value),
			PropFilterType.ReviewRatingMax => source.Where(e => e.GetReviewRating() <= ReviewRatingVal!.Value),
			PropFilterType.ReviewCountMin => source.Where(e => e.Reviews.Length >= ReviewCountVal!.Value),
			PropFilterType.ReviewCountMax => source.Where(e => e.Reviews.Length <= ReviewCountVal!.Value),
			PropFilterType.TitleSearch => source.SearchText(e => e.Name, TitleSearch!),
			PropFilterType.TitlePlotSearch => source.SearchText(e => $"{e.Name} {e.Plot}", TitlePlotSearch!),
			PropFilterType.DirectorSearch => source.SearchText(e => e.Director, DirectorSearch!),
			PropFilterType.ActorSearch => source.SearchText(e => e.Stars.JoinText(" "), ActorSearch!),
			_ => throw new ArgumentException(),
		}
	};
	
	public HtmlNode MkEditUI(ItemNfo<PropFilter> nfo)
	{
		//return Div().ClsOnIf("propfilter", isPropFilterSelFun()).Wrap(
		return Div(EditListCls.DefaultItemCls(nfo).AddCls("propfilter")).Wrap(
			new List<HtmlNode>()
			{
				new HtmlNode("input")
					.Cls("propfilter-enabled")
					.Attr("type", "checkbox")
					.Attr("checked", Enabled ? "" : null)
					.HookArg("change", valStr => {
						nfo.Mutate(nfo.Item with { Enabled = bool.Parse(valStr) });
					}, "this.checked"),
				Div("propfilter-name")
					.Txt(Name),
			}.Concat(
				MkEditUIType(nfo)
			)
		);
	}
	
	private HtmlNode[] MkEditUIType(ItemNfo<PropFilter> nfo) => Type switch
	{
		PropFilterType.YearMin or PropFilterType.YearMax => new[] {
			new HtmlNode("input").Cls("propfilter-main")
				.Attr("type", "range")
				.Attr("min", "1980")
				.Attr("max", $"{DateTime.Now.Year}")
				.Attr("value", $"{YearVal}")
				.HookArg("change", valStr =>
				{
					nfo.Mutate(nfo.Item with { YearVal = int.Parse(valStr) });
				}, "this.value"),
			Div().Txt($"{YearVal}")
		},
		
		PropFilterType.GenreInc or PropFilterType.GenreExc => 
			Div("propfilter-main propfilter-main-genre").Wrap(
				Enum.GetValues<Genre>().SelectToArray(genre =>
					Btn($"{genre}", () =>
					{
						var isOn = GenreVal!.Value.HasFlag(genre);
						var genresNext = isOn switch
						{
							true => (Genre)(GenreVal!.Value & ~genre),
							false => (Genre)(GenreVal!.Value | genre)
						};
						nfo.Mutate(nfo.Item with { GenreVal = genresNext });
					}).ClsOnIf("propfilter-main-genre-single", GenreVal!.Value.HasFlag(genre))
				)
			),

		PropFilterType.ImdbRatingMin or PropFilterType.ImdbRatingMax => new[] {
			new HtmlNode("input").Cls("propfilter-main")
				.Attr("type", "range")
				.Attr("min", "0")
				.Attr("max", "100")
				.Attr("value", $"{ImdbRatingVal}")
				.HookArg("change", valStr =>
				{
					nfo.Mutate(nfo.Item with { ImdbRatingVal = int.Parse(valStr) });
				}, "this.value"),
			Div().Txt($"{ImdbRatingVal}")
		},

		PropFilterType.ReviewRatingMin or PropFilterType.ReviewRatingMax => new[] {
			new HtmlNode("input").Cls("propfilter-main")
				.Attr("type", "range")
				.Attr("min", "0")
				.Attr("max", "100")
				.Attr("value", $"{ReviewRatingVal}")
				.HookArg("change", valStr =>
				{
					nfo.Mutate(nfo.Item with { ReviewRatingVal = int.Parse(valStr) });
				}, "this.value"),
			Div().Txt($"{ReviewRatingVal}")
		},

		PropFilterType.ReviewCountMin or PropFilterType.ReviewCountMax => new[] {
			new HtmlNode("input").Cls("propfilter-main")
				.Attr("type", "range")
				.Attr("min", "0")
				.Attr("max", "2000")
				.Attr("value", $"{ReviewCountVal}")
				.HookArg("change", valStr =>
				{
					nfo.Mutate(nfo.Item with { ReviewCountVal = int.Parse(valStr) });
				}, "this.value"),
			Div().Txt($"{ReviewCountVal}")
		},

		PropFilterType.TitleSearch => new[] {
			new HtmlNode("input").Cls("propfilter-main")
				.Attr("type", "text")
				.Attr("value", $"{TitleSearch}")
				.HookArg("input", valStr =>
				{
					nfo.Mutate(nfo.Item with { TitleSearch = valStr });
				}, "this.value")
		},

		PropFilterType.TitlePlotSearch => new[] {
			new HtmlNode("input").Cls("propfilter-main")
				.Attr("type", "text")
				.Attr("value", $"{TitlePlotSearch}")
				.HookArg("input", valStr =>
				{
					nfo.Mutate(nfo.Item with { TitlePlotSearch = valStr });
				}, "this.value")
		},

		PropFilterType.DirectorSearch => new[] {
			new HtmlNode("input").Cls("propfilter-main")
				.Attr("type", "text")
				.Attr("value", $"{DirectorSearch}")
				.HookArg("input", valStr =>
				{
					nfo.Mutate(nfo.Item with { DirectorSearch = valStr });
				}, "this.value")
		},

		PropFilterType.ActorSearch => new[] {
			new HtmlNode("input").Cls("propfilter-main")
				.Attr("type", "text")
				.Attr("value", $"{ActorSearch}")
				.HookArg("input", valStr =>
				{
					nfo.Mutate(nfo.Item with { ActorSearch = valStr });
				}, "this.value")
		},
		
		_ => throw new ArgumentException(),
	};
}



public class ComboFiltersArr : IEquatable<ComboFiltersArr>
{
	public PropFilter[] Arr { get; }
	
	public static readonly ComboFiltersArr Empty = new(Array.Empty<PropFilter>());
	
	public IEnumerable<Movie> Apply(IEnumerable<Movie> source)
	{
		var res = source;
		foreach (var elt in Arr)
			res = elt.Filter(res);
		return res;
	}

	public ComboFiltersArr(PropFilter[] arr)
	{
		Arr = arr;
	}

	public bool Equals(ComboFiltersArr? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return AreArrsEqual(Arr, other.Arr);
	}
	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((ComboFiltersArr)obj);
	}
	public override int GetHashCode() => Arr.GetHashCodeAggregate();
	public static bool operator ==(ComboFiltersArr? left, ComboFiltersArr? right) => Equals(left, right);
	public static bool operator !=(ComboFiltersArr? left, ComboFiltersArr? right) => !Equals(left, right);
	private static bool AreArrsEqual(PropFilter[] a, PropFilter[] b) =>
		a.Length == b.Length &&
		a.Zip(b).All(t => t.First == t.Second);
}




static class FilterMovieExt
{
	public static int GetReviewRating(this Movie m) => (m.Reviews.Length == 0) switch
	{
		true => 0,
		false => (int)m.Reviews.Average(e => e.Score * 10)
	};
	
	
	public static IEnumerable<Movie> SearchText(this IEnumerable<Movie> source, Func<Movie, string> strFun, string search)
	{
		var parts = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(e => e.ToLowerInvariant()).ToArray();
		return source
			.Where(movie =>
			{
				var movieStr = strFun(movie).ToLowerInvariant();
				return parts.All(movieStr.Contains);
			});
	}
}
