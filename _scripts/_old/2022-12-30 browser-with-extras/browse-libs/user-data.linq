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
  <Namespace>System.Text.Json.Serialization</Namespace>
</Query>

#load ".\base-ui"
#load ".\user-data-structs"

void Main()
{
	//var userData = new UserDataVars();
	
	var data = UserDataFileApi.Load();
	data.Dump();
}



public class UserDataVars : IDisposable
{
	private static readonly TimeSpan SaveThrottleTime = TimeSpan.FromSeconds(1);
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();
	
	public IRwVar<Combo[]> Combos { get; }
	public IRwVar<Filter[]> Filters { get; }
	public IRwVar<Maybe<Combo>> SelCombo { get; }
	
	public UserDataVars(bool testData)
	{
		var data = testData switch
		{
			false => UserDataFileApi.Load(),
			true => UserData.Empty,
		};
		Combos = Var.Make(data.Combos).D(d);
		Filters = Var.Make(data.Filters).D(d);
		SelCombo = Var.Make(data.SelCombo.ToMaybe().Select(comboName => Combos.V.Single(e => e.Name == comboName))).D(d);
		
		EnforceConstraints();
		SetupAutoSaving(testData);
	}
	
	private void EnforceConstraints()
	{
		// when Filter is Deleted -> remove any reference to it in Combos
		// ==============================================================
		Filters.Subscribe(filters =>
		{
			var filterNames = filters.Select(e => e.Name).ToHashSet();
			var anyChanged = false;
			var list = new List<Combo>();
			foreach (var combo in Combos.V)
			{
				if (combo.Filters.Any(e => !filterNames.Contains(e)))
				{
					anyChanged = true;
					list.Add(combo with { Filters = combo.Filters.WhereToArray(filterNames.Contains) });
				}
				else
				{
					list.Add(combo);
				}
			}
			if (anyChanged)
			{
				Combos.V = list.ToArray();
			}
		}).D(d);
	}
	
	private void SetupAutoSaving(bool testData)
	{
		if (testData) return;
		Observable
			.Merge(Combos.ToUnit(), Filters.ToUnit())
			.Throttle(SaveThrottleTime)
			.Subscribe(_ =>
			{
				var data = new UserData(
					Filters.V,
					Combos.V,
					SelCombo.V.Select(e => e.Name).ToNullable()
				);
				UserDataFileApi.Save(data);
			}).D(d);
	}
}



/*public class UserDataVars : IDisposable
{
	private static readonly TimeSpan SaveThrottleTime = TimeSpan.FromSeconds(1);
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();

	private readonly IRwVar<Filter[]> filters;
	private readonly IRwVar<Combo[]> combos;
	private readonly IRwVar<Maybe<string>> selCombo;
	private readonly IRwVar<Maybe<string>> selPropFilter;
	private readonly IRwVar<string[]> selFilters;
	private readonly ISubject<Unit> whenChanged;

	// Serialized
	public IRoVar<Filter[]> Filters => filters.ToReadOnly();
	public IRoVar<Combo[]> Combos => combos.ToReadOnly();
	public IRoVar<Maybe<string>> SelCombo => selCombo.ToReadOnly();
	
	// In memory only
	public IRoVar<Maybe<string>> SelPropFilter => selPropFilter.ToReadOnly();
	public IRoVar<string[]> SelFilters => selFilters.ToReadOnly();
	public IObservable<Unit> WhenChanged => whenChanged.AsObservable().Prepend(Unit.Default);

	public IRoVar<ComboFiltersArr> ComboFilters { get; }


	public UserDataVars(bool testData)
	{
		var data = testData switch
		{
			false => UserDataFileApi.Load(),
			true => UserData.Empty,
		};
		filters = Var.Make(data.Filters).D(d);
		combos = Var.Make(data.Combos).D(d);
		selCombo = Var.Make(data.SelCombo.ToMaybe()).D(d);
		
		selPropFilter = Var.Make(May.None<string>()).D(d);
		selFilters = Var.Make(Array.Empty<string>()).D(d);
		whenChanged = new Subject<Unit>().D(d);
		
		if (!testData)
			WhenChanged.Throttle(SaveThrottleTime).Subscribe(_ =>
			{
				data = new UserData(Filters.V, Combos.V, SelCombo.V.ToNullable());
				UserDataFileApi.Save(data);
			}).D(d);
		
		SelCombo.WhenSome().Subscribe(selCombo => selFilters.V = Combo_GetSel().Filters).D(d);
		
		// TODO: no idea why this line fails !!
		// ComboFilters = Var.Expr(() => MkComboFilterSt(SelCombo.V, Combos.V, Filters.V));
		ComboFilters = Var.Expr(() => MkComboFilterSt(selCombo.V, combos.V, filters.V));
	}

	private static ComboFiltersArr MkComboFilterSt(Maybe<string> mayComboName, Combo[] combos, Filter[] filters) =>
		mayComboName.IsSome(out var comboName) switch
			{
				true => new ComboFiltersArr(
					(
						from filterName in combos.Single(e => e.Name == comboName).Filters
						let filter = filters.Single(e => e.Name == filterName)
						from propFilter in filter.PropFilters
						select propFilter
					).ToArray()
				),
				false => ComboFiltersArr.Empty,
			};
*/





/*class RelationTracker : IDisposable
{
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();
	
	public void EnforceRefMultiple<TMaster, TSlave, TSlaveKey>(ItemBox<TMaster> master, ItemBox<TSlave> slave, Func<TSlave, TSlaveKey> slaveKeyFun, Expression<Func<TMaster, TSlaveKey[]>> fun)
	{
		
	}
	public void EnforceOwnMultiple<TMaster, TSlave>(ItemBox<TMaster> master, Expression<Func<TMaster, TSlave[]>> fun)
	{
		
	}
}

class ItemBox<T> : IDisposable
{
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();

	public IRwVar<T[]> RxArr { get; }
	
	public ItemBox()
	{
		RxArr = Var.Make(Array.Empty<T>()).D(d);
	}
}






static class ExprTreeUtils
{
	public static Func<TObj, TProp> CompileGetter<TObj, TProp>(Expression<Func<TObj, TProp>> expr) =>
		expr.Compile();

	public static Action<TObj, TProp> CompileSetter<TObj, TProp>(Expression<Func<TObj, TProp>> expr)
	{
		if (expr.Body is not MemberExpression memberExpr) throw new ArgumentException();
		var propName = memberExpr.Member.Name;		
		var paramObj = Expression.Parameter(typeof(TObj));
	    var paramProp = Expression.Parameter(typeof(TProp), propName);
	    var propGetExpr = Expression.Property(paramObj, propName);
	    return Expression.Lambda<Action<TObj, TProp>>
	    (
	        Expression.Assign(propGetExpr, paramProp), paramObj, paramProp
	    ).Compile();
	}
}
*/








/*public class UserDataVars : IDisposable
{
	private static readonly TimeSpan SaveThrottleTime = TimeSpan.FromSeconds(1);
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();

	private readonly IRwVar<Filter[]> filters;
	private readonly IRwVar<Combo[]> combos;
	private readonly IRwVar<Maybe<string>> selCombo;
	private readonly IRwVar<Maybe<string>> selPropFilter;
	private readonly IRwVar<string[]> selFilters;
	private readonly ISubject<Unit> whenChanged;

	// Serialized
	public IRoVar<Filter[]> Filters => filters.ToReadOnly();
	public IRoVar<Combo[]> Combos => combos.ToReadOnly();
	public IRoVar<Maybe<string>> SelCombo => selCombo.ToReadOnly();
	
	// In memory only
	public IRoVar<Maybe<string>> SelPropFilter => selPropFilter.ToReadOnly();
	public IRoVar<string[]> SelFilters => selFilters.ToReadOnly();
	public IObservable<Unit> WhenChanged => whenChanged.AsObservable().Prepend(Unit.Default);

	public IRoVar<ComboFiltersArr> ComboFilters { get; }


	public UserDataVars(bool testData)
	{
		var data = testData switch
		{
			false => UserDataFileApi.Load(),
			true => UserData.Empty,
		};
		filters = Var.Make(data.Filters).D(d);
		combos = Var.Make(data.Combos).D(d);
		selCombo = Var.Make(data.SelCombo.ToMaybe()).D(d);
		
		selPropFilter = Var.Make(May.None<string>()).D(d);
		selFilters = Var.Make(Array.Empty<string>()).D(d);
		whenChanged = new Subject<Unit>().D(d);
		
		if (!testData)
			WhenChanged.Throttle(SaveThrottleTime).Subscribe(_ =>
			{
				data = new UserData(Filters.V, Combos.V, SelCombo.V.ToNullable());
				UserDataFileApi.Save(data);
			}).D(d);
		
		SelCombo.WhenSome().Subscribe(selCombo => selFilters.V = Combo_GetSel().Filters).D(d);
		
		// TODO: no idea why this line fails !!
		// ComboFilters = Var.Expr(() => MkComboFilterSt(SelCombo.V, Combos.V, Filters.V));
		ComboFilters = Var.Expr(() => MkComboFilterSt(selCombo.V, combos.V, filters.V));
	}

	private static ComboFiltersArr MkComboFilterSt(Maybe<string> mayComboName, Combo[] combos, Filter[] filters) =>
		mayComboName.IsSome(out var comboName) switch
			{
				true => new ComboFiltersArr(
					(
						from filterName in combos.Single(e => e.Name == comboName).Filters
						let filter = filters.Single(e => e.Name == filterName)
						from propFilter in filter.PropFilters
						select propFilter
					).ToArray()
				),
				false => ComboFiltersArr.Empty,
			};
		
	
	// *******************************
	// * Toggle PropFilter Selection *
	// *******************************
	public void PropFilter_SetSel(Maybe<PropFilter> sel)
	{
		selPropFilter.V = sel.Select(e => e.Name);
		SignalChange();
	}
	public bool PropFilter_IsSel(PropFilter propFilter) => SelPropFilter.V.IsSome(out var val) && val == propFilter.Name;
	public bool PropFilter_CanSelBeMovedUp()
	{
		if (SelPropFilter.V.IsNone(out var propFilterName) || SelFilters.V.Length != 1) return false;
		var selFilter = Filter_Get(SelFilters.V[0]);
		var idx = selFilter.PropFilters.IndexOf(e => e.Name == propFilterName);
		return idx > 0;
	}
	public bool PropFilter_CanSelBeMovedDown()
	{
		if (SelPropFilter.V.IsNone(out var propFilterName) || SelFilters.V.Length != 1) return false;
		var selFilter = Filter_Get(SelFilters.V[0]);
		var idx = selFilter.PropFilters.IndexOf(e => e.Name == propFilterName);
		return idx < selFilter.PropFilters.Length - 1;
	}
	public void PropFilter_MoveUp()
	{
		if (!PropFilter_CanSelBeMovedUp()) return;
		if (SelPropFilter.V.IsNone(out var propFilterName) || SelFilters.V.Length != 1) return;
		var selFilterPrev = Filter_Get(SelFilters.V[0]);
		var propFiltersPrev = selFilterPrev.PropFilters;
		var list = propFiltersPrev.ToList();
		var idx = list.IndexOf(e => e.Name == propFilterName);
		var propFilter = list[idx];
		list.RemoveAt(idx);
		list.Insert(idx - 1, propFilter);
		var propFiltersNext = list.ToArray();
		var selFilterNext = selFilterPrev with { PropFilters = propFiltersNext };
		filters.V = filters.V.ArrRepl(selFilterPrev, selFilterNext);
		SignalChange();
	}
	public void PropFilter_MoveDown()
	{
		if (!PropFilter_CanSelBeMovedDown()) return;
		if (SelPropFilter.V.IsNone(out var propFilterName) || SelFilters.V.Length != 1) return;
		var selFilterPrev = Filter_Get(SelFilters.V[0]);
		var propFiltersPrev = selFilterPrev.PropFilters;
		var list = propFiltersPrev.ToList();
		var idx = list.IndexOf(e => e.Name == propFilterName);
		var propFilter = list[idx];
		list.RemoveAt(idx);
		list.Insert(idx + 1, propFilter);
		var propFiltersNext = list.ToArray();
		var selFilterNext = selFilterPrev with { PropFilters = propFiltersNext };
		filters.V = filters.V.ArrRepl(selFilterPrev, selFilterNext);
		SignalChange();
	}

	
	// ***************************
	// * Toggle Filter Selection *
	// ***************************
	public void Filter_ToggleSel(string name)
	{
		var isIn = SelFilters.V.Contains(name);
		if (isIn)
			selFilters.V = selFilters.V.ArrDel(name);
		else
			selFilters.V = selFilters.V.ArrAdd(name);
		SignalChange();
	}
	public bool Filter_IsSel(string name) => SelFilters.V.Contains(name);
	
	// **************************
	// * Change Combo Selection *
	// **************************
	public void Combo_SetSel(Maybe<string> selComboVal)
	{
		selCombo.V = selComboVal;
		SignalChange();
	}
	public bool Combo_IsSelEqualTo(string comboName) => SelCombo.V.IsSome(out var val) && val == comboName;
	
	// ********************
	// * Get / Add Filter *
	// ********************
	public Filter Filter_Get(string name) {
		var filter = Filters.V.FirstOrDefault(e => e.Name == name);
		if (filter == null) throw new ArgumentException("cannot find element");
		return filter;
	}
	public void Filter_Add(string name) {
		filters.V = filters.V.ArrAdd(new Filter(name, Array.Empty<PropFilter>()));
		SignalChange();
	}
	
	// ***********************************************************************************
	// * Change the selected Filter (only works if there is exactly one Filter selected) *
	// ***********************************************************************************
	public void Filter_SingleSel_Del() {
		var filter = Filter_GetSingleSel();
		filters.V = filters.V.ArrDel(filter);
		SignalChange();
	}
	public void Filter_SingleSel_ChangeName(string name) {
		var filterPrev = Filter_GetSingleSel();
		var filterNext = filterPrev with { Name = name };
		filters.V = filters.V.ArrRepl(filterPrev, filterNext);
		selFilters.V = new [] { name };
		SignalChange();
	}
	public void Filter_SingleSel_PropFilter_Add(PropFilter propFilter) {
		var filterPrev = Filter_GetSingleSel();
		var filterNext = filterPrev with { PropFilters = filterPrev.PropFilters.ArrAdd(propFilter) };
		filters.V = filters.V.ArrRepl(filterPrev, filterNext);
		selPropFilter.V = May.Some(propFilter.Name);
		SignalChange();
	}
	public void Filter_SingleSel_PropFilter_Del() {
		var propFilterName = SelPropFilter.V.Ensure();
		var filterPrev = Filter_GetSingleSel();
		var propFilter = filterPrev.PropFilters.Single(e => e.Name == propFilterName);
		var filterNext = filterPrev with { PropFilters = filterPrev.PropFilters.ArrDel(propFilter) };
		filters.V = filters.V.ArrRepl(filterPrev, filterNext);
		selPropFilter.V = May.None<string>();		
		SignalChange();
	}
	public void Filter_SingleSel_PropFilter_Change(PropFilter propFilterPrev, PropFilter propFilterNext) {
		var filterPrev = Filter_GetSingleSel();
		var filterNext = filterPrev with { PropFilters = filterPrev.PropFilters.ArrRepl(propFilterPrev, propFilterNext) };
		filters.V = filters.V.ArrRepl(filterPrev, filterNext);
		selPropFilter.V = May.Some(propFilterNext.Name);
		SignalChange();
	}
	
	// ********************
	// * Get / Add Combos *
	// ********************
	public Combo Combo_Get(string name)
	{
		var combo = Combos.V.FirstOrDefault(e => e.Name == name);
		if (combo == null) throw new ArgumentException("Cannot find combo");
		return combo;
	}
	public void Combo_Add(string name)
	{
		combos.V = combos.V.ArrAdd(new Combo(name, SelFilters.V));
		SignalChange();
	}
	
	// *********************************************************************
	// * Edit the selected Combo (only works if there is a selected Combo) *
	// *********************************************************************
	public void Combo_Sel_Del()
	{
		var combo = Combo_GetSel();
		combos.V = combos.V.ArrDel(combo);
		selCombo.V = May.None<string>();
		SignalChange();
	}
	public void Combo_Sel_ChangeName(string name)
	{
		var comboPrev = Combo_GetSel();
		var comboNext = comboPrev with { Name = name };
		combos.V = combos.V.ArrRepl(comboPrev, comboNext);
		selCombo.V = May.Some(name);
		SignalChange();
	}
	public void Combo_Sel_SetFilters()
	{
		var comboPrev = Combo_GetSel();
		var comboNext = comboPrev with { Filters = SelFilters.V };
		combos.V = combos.V.ArrRepl(comboPrev, comboNext);
		SignalChange();
	}
	
	private Filter Filter_GetSingleSel() => Filter_Get(SelFilters.V.Single());
	private Combo Combo_GetSel() => Combo_Get(SelCombo.V.Ensure());
	private void SignalChange()
	{
		FixSels();
		whenChanged.OnNext(Unit.Default);
	}

	private void FixSels()
	{
		if (SelCombo.V.IsSome(out var selComboVal) && Combos.V.All(e => e.Name != selComboVal))
			selCombo.V = May.None<string>();
		selFilters.V = SelFilters.V.WhereToArray(e => Filters.V.Any(f => f.Name == e));
		if (SelPropFilter.V.IsSome(out var selPropFilterVal) && Filters.V.All(filter => filter.PropFilters.All(propFilter => propFilter.Name != selPropFilterVal)))
			selPropFilter.V = May.None<string>();
	}
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
	
	
	public HtmlNode MkEditUI(UserDataVars userData)
	{
		return Div().ClsOn("base-propfilter", userData.PropFilter_IsSel(this)).Wrap(
			//userData.WhenChanged,
			//() =>
			new List<HtmlNode>()
			{
				Div("base-propfilter-name")
					.Txt(Name)
					.OnClick(() => userData.PropFilter_SetSel(May.Some(this)), true)
					,
				new HtmlNode("input")
					.Cls("base-propfilter-enabled")
					.Attr("type", "checkbox")
					.Attr("checked", Enabled ? "" : null)
					.HookArg("change", valStr => {
						userData.Filter_SingleSel_PropFilter_Change(this, this with { Enabled = bool.Parse(valStr) });
					}, "this.checked")
					.OnClick(() => {}, true),
			}.AppendRange(
				MkEditUIType(userData)
			)
		);
	}
	
	private HtmlNode[] MkEditUIType(UserDataVars userData) => Type switch
	{
		PropFilterType.YearMin or PropFilterType.YearMax => Arr(
			new HtmlNode("input").Cls("base-propfilter-main")
				.Attr("type", "range")
				.Attr("min", "1980")
				.Attr("max", $"{DateTime.Now.Year}")
				.Attr("value", $"{YearVal}")
				.HookArg("change", valStr => userData.Filter_SingleSel_PropFilter_Change(this, this with { YearVal = int.Parse(valStr) }), "this.value"),
			Div().Txt($"{YearVal}")
		),
		
		PropFilterType.GenreInc or PropFilterType.GenreExc => 
			Div("base-propfilter-main base-propfilter-main-genre").Wrap(
				Enum.GetValues<Genre>().SelectToArray(genre =>
					Btn($"{genre}", () =>
					{
						var isOn = GenreVal!.Value.HasFlag(genre);
						var genresNext = isOn switch
						{
							true => (Genre)(GenreVal!.Value & ~genre),
							false => (Genre)(GenreVal!.Value | genre)
						};
						userData.Filter_SingleSel_PropFilter_Change(this, this with { GenreVal = genresNext });
					}).ClsOn("base-propfilter-main-genre-single", GenreVal!.Value.HasFlag(genre))
				)
			),
			
		PropFilterType.ImdbRatingMin or PropFilterType.ImdbRatingMax => Arr(
			new HtmlNode("input").Cls("base-propfilter-main")
				.Attr("type", "range")
				.Attr("min", "0")
				.Attr("max", "100")
				.Attr("value", $"{ImdbRatingVal}")
				.HookArg("change", valStr => userData.Filter_SingleSel_PropFilter_Change(this, this with { ImdbRatingVal = int.Parse(valStr) }), "this.value"),
			Div().Txt($"{ImdbRatingVal}")
		),
			
		PropFilterType.ReviewRatingMin or PropFilterType.ReviewRatingMax => Arr(
			new HtmlNode("input").Cls("base-propfilter-main")
				.Attr("type", "range")
				.Attr("min", "0")
				.Attr("max", "100")
				.Attr("value", $"{ReviewRatingVal}")
				.HookArg("change", valStr => userData.Filter_SingleSel_PropFilter_Change(this, this with { ReviewRatingVal = int.Parse(valStr) }), "this.value"),
			Div().Txt($"{ReviewRatingVal}")
		),
			
		PropFilterType.ReviewCountMin or PropFilterType.ReviewCountMax => Arr(
			new HtmlNode("input").Cls("base-propfilter-main")
				.Attr("type", "range")
				.Attr("min", "0")
				.Attr("max", "2000")
				.Attr("value", $"{ReviewCountVal}")
				.HookArg("change", valStr => userData.Filter_SingleSel_PropFilter_Change(this, this with { ReviewCountVal = int.Parse(valStr) }), "this.value"),
			Div().Txt($"{ReviewCountVal}")
		),
			
		PropFilterType.TitleSearch => Arr(
			new HtmlNode("input").Cls("base-propfilter-main")
				.Attr("type", "text")
				.Attr("value", $"{TitleSearch}")
				.HookArg("input", valStr => userData.Filter_SingleSel_PropFilter_Change(this, this with { TitleSearch = valStr }), "this.value")
		),
			
		PropFilterType.TitlePlotSearch => Arr(
			new HtmlNode("input").Cls("base-propfilter-main")
				.Attr("type", "text")
				.Attr("value", $"{TitlePlotSearch}")
				.HookArg("input", valStr => userData.Filter_SingleSel_PropFilter_Change(this, this with { TitlePlotSearch = valStr }), "this.value")
		),
			
		PropFilterType.DirectorSearch => Arr(
			new HtmlNode("input").Cls("base-propfilter-main")
				.Attr("type", "text")
				.Attr("value", $"{DirectorSearch}")
				.HookArg("input", valStr => userData.Filter_SingleSel_PropFilter_Change(this, this with { DirectorSearch = valStr }), "this.value")
		),
			
		PropFilterType.ActorSearch => Arr(
			new HtmlNode("input").Cls("base-propfilter-main")
				.Attr("type", "text")
				.Attr("value", $"{ActorSearch}")
				.HookArg("input", valStr => userData.Filter_SingleSel_PropFilter_Change(this, this with { ActorSearch = valStr }), "this.value")
		),
		
		_ => throw new ArgumentException(),
	};
}

static class MovieUtils
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

public record Filter(
	string Name,
	PropFilter[] PropFilters
);

public record Combo(
	string Name,
	string[] Filters
);

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









static class UserDataFileApi
{
	public static UserData Load() => JsonUtils.Load<UserData>(DataUtils.UserDataFile, () => UserData.Empty);
	public static void Save(UserData e) => JsonUtils.Save(DataUtils.UserDataFile, e);
}

*/



