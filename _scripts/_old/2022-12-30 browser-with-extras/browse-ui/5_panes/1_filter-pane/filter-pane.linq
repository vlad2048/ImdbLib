<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeLib\bin\Debug\net7.0\DynaServeLib.dll</Reference>
  <Namespace>static DynaServeLib.Nodes.Ctrls</Namespace>
  <Namespace>DynaServeLib</Namespace>
  <Namespace>DynaServeLib.Nodes</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive</Namespace>
</Query>

#load "..\..\..\browse-libs\base-ui"
#load "..\..\..\browse-libs\user-data"
#load "..\..\..\browse-libs\user-data-structs"
#load "..\..\1_layout\layout"
#load "..\..\10_ctrls\1_dlg-input\dlg-input"
#load "..\..\10_ctrls\2_edit-list\edit-list"


void Main()
{
	var userData = new UserDataVars(testData: true).D(D);
	var filterVars = new FilterVars(userData).D(D);
	var isVisible = Var.Make(true).D(D);
	
	Serv.Start(
		MkOptDbg(
			"1_layout",
			"1_filter-pane",
			"1_dlg-input",
			"2_edit-list"
		),
		
		UI_Layout.Make(
			mainOverlayNodes: UI_FilterPane.Make(filterVars, isVisible)
		)
	);
}


public class FilterVars : IDisposable
{
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();

	private readonly IFullRwBndVar<PropFilter[]> displayedPropFilters;

	public UserDataVars UserData { get; }
	// Outer	User adds/removes/edits the DisplayedPropFilters
	// Inner	User changes the currently selected Maybe<Filter>
	public IRwVar<PropFilter[]> DisplayedPropFilters => displayedPropFilters.ToRwBndVar();

	public FilterVars(UserDataVars userData)
	{
		UserData = userData;
		displayedPropFilters = Var.MakeBnd(Array.Empty<PropFilter>()).D(d);
	}

	public void Update_DisplayedPropFilters_When_SelFilter_Changes(IRoVar<Maybe<Filter>> maySelFilterVar) =>
		maySelFilterVar
			.SelectVar(maySelFilter =>
				maySelFilter
					.Select(selFilter => selFilter.PropFilters)
					.FailWith(Array.Empty<PropFilter>())
			)
			.PipeToInner(displayedPropFilters);

	public void Update_SelFilter_When_DisplayedPropFilters_Change(Action<Func<Filter, Filter>> replaceSelFilterFun) =>
		displayedPropFilters
			.WhenOuter
			.Subscribe(propFilters => replaceSelFilterFun(filter => filter with { PropFilters = propFilters })).D(d);
}



public static class UI_FilterPane
{
	public static HtmlNode Make(FilterVars filterVars, IRoVar<bool> isVisible)
	{
		var comboList = new EditList<Combo>(
			filterVars.UserData.Combos,
			"Combos",
			async prev => await DlgCombo(prev, combo => filterVars.UserData.Combos.V.All(e => e.Name != combo.Name) || prev.Select(e => e.Name).IsSomeAndEqualTo(combo.Name), filterVars.UserData.Filters.V),
			opt =>
			{
				opt.Width = 300;
			}
		).D(D);
		
		var filterList = new EditList<Filter>(
			filterVars.UserData.Filters,
			"Filters",
			async prev => await DlgFilter(prev, filter => filterVars.UserData.Filters.V.All(e => e.Name != filter.Name) || prev.Select(e => e.Name).IsSomeAndEqualTo(filter.Name)),
			opt =>
			{
				opt.Width = 300;
				opt.ItemDispWhenChange = comboList.SelItem.ToUnit();
				opt.ItemDispFun = nfo =>
					Div(EditListCls.DefaultItemCls(nfo).AddCls("filterpane-filter-item")).Wrap(
						Div("filterpane-filter-item-inner").Txt($"{nfo.Item}")
						.AddNodeIf(
							Div("filterpane-filter-item-inner-linked"),
							comboList.SelItem.V.IsSomeAndVerifies(combo => combo.Filters.Any(e => e == nfo.Item.Name))
						)
					);
			}
		).D(D);

		filterVars.Update_DisplayedPropFilters_When_SelFilter_Changes(filterList.SelItem);
		filterVars.Update_SelFilter_When_DisplayedPropFilters_Change(filterList.TransformSelItemEnsure);
		
		var propFilterList = new EditList<PropFilter>(
			filterVars.DisplayedPropFilters,
			"PropFilters",
			async _ => await DlgPropFilter(),
			opt =>
			{
				opt.Width = int.MaxValue;
				opt.ItemDispFun = nfo => nfo.Item.MkEditUI();
			}
		).D(D);
		
		return TSection("filterpane").VisibleWhen(isVisible).Wrap(
			propFilterList.UI,
			filterList.UI,
			comboList.UI
		);
	}
	
	
	
	
	private static async Task<Maybe<Combo>> DlgCombo(Maybe<Combo> prev, Func<Combo, bool> validFun, Filter[] filters)
	{
		const string keyName = "name";
		const string keyFilters = "filters";
		
		Combo Mk(IDlgReader r) => new(r.GetString(keyName), r.GetMultipleChoices(keyFilters));
		
		var mayRead = await DlgInput.Make(
			prev.IsSome() ? "Edit Combo" : "Add Combo",
			dlg =>
			{
				dlg.ValidFun = r => validFun(Mk(r));
				dlg.EditString(keyName, "Name", prev.Select(e => e.Name).FailWith(""));
				dlg.EditMultipleChoices(keyFilters, "Filters", prev.Select(e => e.Filters).FailWith(Array.Empty<string>()), filters.SelectToArray(e => e.Name));
			}
		);
		
		return mayRead.Select(Mk);
	}
	
	private static async Task<Maybe<Filter>> DlgFilter(Maybe<Filter> prev, Func<Filter, bool> validFun)
	{
		const string keyName = "name";

		Filter Mk(IDlgReader r) => new(r.GetString(keyName), prev.Select(e => e.PropFilters).FailWith(Array.Empty<PropFilter>()));
		
		var mayRead = await DlgInput.Make(
			prev.IsSome() ? "Edit Filter" : "Add Filter",
			dlg =>
			{
				dlg.ValidFun = r => validFun(Mk(r));
				dlg.EditString(keyName, "Name", prev.Select(e => e.Name).FailWith(""));
			}
		);
		
		return mayRead.Select(Mk);
	}
	
	private static async Task<Maybe<PropFilter>> DlgPropFilter()
	{
		const string typeName = "type";

		PropFilter Mk(IDlgReader r) => PropFilter.Make(Enum.Parse<PropFilterType>(r.GetString(typeName)));
		
		var mayRead = await DlgInput.Make(
			"Add PropFilter",
			dlg =>
			{
				dlg.EditSingleChoice(typeName, "Type", May.None<string>(), Enum.GetValues<PropFilterType>().SelectToArray(e => $"{e}"));
			}
		);
		
		return mayRead.Select(Mk);
	}
	
	
	
	/*private static async Task<Maybe<PropFilterType>> AskFilterType()
	{
		var mayType = await DlgInput.ReadStringChoice("Filter name?", Enum.GetValues<PropFilterType>().SelectToArray(e => $"{e}"));
		if (mayType.IsNone(out var type)) return May.None<PropFilterType>();
		return May.Some(Enum.Parse<PropFilterType>(type));
	}*/
	
	
	// ****************
	// * Prop Filters *
	// ****************
	/*
	private static HtmlNode Make_Props(UserDataVars userData) =>
		Div("filterpane-props-wrap filterpane-subpanel").Wrap(
			userData.WhenChanged,
			() =>
			{
				if (userData.SelFilters.V.Length != 1) return Array.Empty<HtmlNode>();
				var selFilterName = userData.SelFilters.V[0];
				var selFilter = userData.Filter_Get(selFilterName);
				return Arr(
					Div("filterpane-props").Wrap(
						Div("filterpane-titlelist")
							//.OnClick(() => userData.PropFilter_SetSel(May.None<PropFilter>()))
							.Wrap(
							Div("filterpane-titlerow").Wrap(
								TH3().Txt($"Editing {selFilter.Name}")
							),
							Div("filterpane-props-list").Wrap(
								selFilter.PropFilters.SelectToArray(propFilter =>
									propFilter.MkEditUI(userData)
								)
							)
						),
						Btn("Add", async () => {
							if ((await AskFilterType()).IsNone(out var type)) return;
							userData.Filter_SingleSel_PropFilter_Add(PropFilter.Make(type));
						}).Cls("filterpane-props-list-add"),
						
						Div("filterpane-props-btnrow").Wrap(
							IconBtn("fa-solid fa-trash", () =>
							{
								userData.Filter_SingleSel_PropFilter_Del();
							}).EnableWhen(userData.SelPropFilter.Select(e => e.IsSome())),
							IconBtn("fa-solid fa-up-long", () =>
							{
								userData.PropFilter_MoveUp();
							}).EnableWhen(userData.WhenChanged.Select(_ => userData.PropFilter_CanSelBeMovedUp())),
							IconBtn("fa-solid fa-down-long", () =>
							{
								userData.PropFilter_MoveDown();
							}).EnableWhen(userData.WhenChanged.Select(_ => userData.PropFilter_CanSelBeMovedDown()))
						)
					)
				);
			}
		);
	

	// ***********
	// * Filters *
	// ***********
	private static HtmlNode Make_Filters(UserDataVars userData) =>
		Div("filterpane-list-wrap filterpane-subpanel").Wrap(
			Div("filterpane-titlelist").Wrap(
				Div("filterpane-titlerow").Wrap(
					TH3().Txt("Filters"),
					IconBtn("fa-solid fa-plus", async () =>
					{
						if ((await AskFilterName(userData, "")).IsNone(out var name)) return;
						userData.Filter_Add(name);
					})
				),
				Div("filterpane-list").Wrap(
					userData.WhenChanged,
					() => userData.Filters.V.SelectToArray(filter =>
						Div()
							.ClsOn("filterpane-list-item", userData.Filter_IsSel(filter.Name))
							.Txt(filter.Name)
							.OnClick(() => userData.Filter_ToggleSel(filter.Name))
					)
				)
			),
			Div("filterpane-edit").Wrap(
				userData.WhenChanged,
				() =>
				{
					if (userData.SelFilters.V.Length != 1) return Array.Empty<HtmlNode>();
					var selFilterName = userData.SelFilters.V[0];
					var selFilter = userData.Filter_Get(selFilterName);
					return new HtmlNode[]
					{
						Div("filterpane-edit-row").Wrap(
							HtmlNode.MkTxt("Change name"),
							IconBtn("fa-solid fa-pen", async () => {
								if ((await AskFilterName(userData, selFilter.Name)).IsNone(out var name)) return;
								if (name == selFilter.Name) return;
								userData.Filter_SingleSel_ChangeName(name);
							})
						),
						Div("filterpane-edit-row").Wrap(
							HtmlNode.MkTxt("Delete"),
							IconBtn("fa-solid fa-trash", () => userData.Filter_SingleSel_Del())
						)
					};
				}
			)
		);
	

	// **********
	// * Combos *
	// **********
	private static HtmlNode Make_Combos(UserDataVars userData) =>
		Div("filterpane-list-wrap filterpane-subpanel").Wrap(
			Div("filterpane-titlelist").OnClick(() => userData.Combo_SetSel(May.None<string>())).Wrap(
				Div("filterpane-titlerow").Wrap(
					TH3().Txt("Combos"),
					IconBtn("fa-solid fa-plus", async () =>
					{
						if ((await AskComboName(userData, "")).IsNone(out var name)) return;
						userData.Combo_Add(name);
					})
				),
				Div("filterpane-list").Wrap(
					userData.WhenChanged,
					() => userData.Combos.V.SelectToArray(combo =>
						Div()
							.ClsOn("filterpane-list-item", userData.Combo_IsSelEqualTo(combo.Name))
							.Txt(combo.Name)
							.OnClick(() => userData.Combo_SetSel(May.Some(combo.Name)), true)
					)
				)
			),
			Div("filterpane-edit").Wrap(
				userData.WhenChanged,
				() =>
				{
					if (userData.SelCombo.V.IsNone(out var selComboName)) return Array.Empty<HtmlNode>();
					var selCombo = userData.Combo_Get(selComboName);
					return new HtmlNode[]
					{
						Div("filterpane-edit-row").Wrap(
							HtmlNode.MkTxt("Change name"),
							IconBtn("fa-solid fa-pen", async () => {
								if ((await AskComboName(userData, selCombo.Name)).IsNone(out var name)) return;
								if (name == selCombo.Name) return;
								userData.Combo_Sel_ChangeName(name);
							})
						),
						Div("filterpane-edit-row").Wrap(
							HtmlNode.MkTxt("Set filters"),
							IconBtn("fa-solid fa-clipboard-list", () => userData.Combo_Sel_SetFilters())
						),
						Div("filterpane-edit-row").Wrap(
							HtmlNode.MkTxt("Delete"),
							IconBtn("fa-solid fa-trash", () => userData.Combo_Sel_Del())
						)
					};
				}
			)
		);
	
	private static async Task<Maybe<string>> AskFilterName(UserDataVars userData, string namePrev)
	{
		var mayName = await DlgInput.ReadString("Filter name?", namePrev);
		if (mayName.IsNone(out var name)) return May.None<string>();
		if (userData.Filters.V.Any(filter => filter.Name == name)) return May.None<string>();
		return May.Some(name);
	}
	
	private static async Task<Maybe<PropFilterType>> AskFilterType()
	{
		var mayType = await DlgInput.ReadStringChoice("Filter name?", Enum.GetValues<PropFilterType>().SelectToArray(e => $"{e}"));
		if (mayType.IsNone(out var type)) return May.None<PropFilterType>();
		return May.Some(Enum.Parse<PropFilterType>(type));
	}
	
	private static async Task<Maybe<string>> AskComboName(UserDataVars userData, string namePrev)
	{
		var mayName = await DlgInput.ReadString("Combo name?", namePrev);
		if (mayName.IsNone(out var name)) return May.None<string>();
		if (userData.Combos.V.Any(combo => combo.Name == name)) return May.None<string>();
		return May.Some(name);
	}
	*/
}




/*public static class UI_FilterPane
{
	public static HtmlNode Make(UserDataVars userData, IRoVar<bool> isVisible) =>
		TSection("filterpane").VisibleWhen(isVisible).Wrap(
			Make_Section_Edit(userData),
			Make_Section_List(userData)
		);
		
		
	private static HtmlNode Make_Section_Edit(UserDataVars userData) =>
		Div("filterpane-section-edit").Wrap(
			userData.WhenEvt.ToUnit(),
			() =>
			{
				if (userData.ActiveFilter.V.IsNone(out var activeFilterName)) return Array.Empty<HtmlNode>();
				var activeFilter = userData.GetFilterByName(activeFilterName);
				return new HtmlNode[] {
						Div("filterpane-section-edit-inner").Wrap(
						TH3().Txt(activeFilter.Name),
						Div("filterpane-section-edit-filts").Wrap(
							activeFilter.Filts.Select(filt =>
								Make_Filt_Editor(userData, activeFilter, filt)
							)
						),
						TBtn("filterpane-section-edit-add").Txt("Add").OnClick(() =>
						{
							var filt = new YearFilt(MinOrMax.Min);
							userData.Filt_Add(activeFilterName, filt);
						})
					)
				};
			}
		);
	
	private static HtmlNode Make_Filt_Editor(UserDataVars userData, Filter filter, IFilt filt) =>
		Div("filterpane-section-edit-filteditor").Wrap(
			filt.MkEditUI(userData, filter)
		);
	
	
	private static HtmlNode Make_Section_List(UserDataVars userData) =>
		Div("filterpane-section-list").Wrap(
			Div("filterpane-section-listwithtitle").OnClick(() => userData.SetActiveFilter(May.None<string>())).Wrap(
				TH3().Txt("Saved"),
				Div("filterpane-list").Wrap(
					userData.WhenEvt.ToUnit().Prepend(Unit.Default),
					() => userData.Filters.V.Select(filter =>
						Div()
							.Cls(userData.ActiveFilter.V.IsSome(out var activeFilter) && activeFilter == filter.Name ? "filterpane-list-item filterpane-list-item-active" : "filterpane-list-item")
							.OnClick(() => userData.SetActiveFilter(May.Some(filter.Name)), true)
							.Wrap(
								Div().Txt(filter.Name)
							)
					)
				)
			),
			Div("filterpane-list-edit-wrap").Wrap(
				userData.ActiveFilter.ToUnit(),
				() => userData.ActiveFilter.V.IsSome(out var activeFilter) switch
				{
					true => Div("filterpane-list-edit").Wrap(
						TH3().Txt(activeFilter),
						Div("filterpane-list-edit-line").Wrap(
							HtmlNode.MkTxt("Change name"),
							IconBtn("fa-solid fa-pen", async () => {
								if ((await AskFilterName(userData, activeFilter)).IsNone(out var name)) return;
								if (name == activeFilter) return;
								userData.RenameFilter(activeFilter, name);
							})
						),
						Div("filterpane-list-edit-line").Wrap(
							HtmlNode.MkTxt("Delete"),
							IconBtn("fa-solid fa-trash", () => userData.DeleteFilter(activeFilter))
						)
					),
					false => Array.Empty<HtmlNode>()
				}
			),
			Div("filterpane-list-btns").Wrap(
				IconBtn("fa-solid fa-floppy-disk", () =>
				{
					userData.Save();
				}).EnableWhen(userData.WhenEvt.ToUnit().Select(_ => userData.NeedsSaving)),
				IconBtn("fa-solid fa-plus", async () =>
				{
					if ((await AskFilterName(userData, "")).IsNone(out var name)) return;
					userData.CreateFilter(name);
				})
			)
		);
	
	private static async Task<Maybe<string>> AskFilterName(UserDataVars userData, string namePrev)
	{
		var mayName = await DlgInput.ReadString("Name?", namePrev);
		if (mayName.IsNone(out var name)) return May.None<string>();
		if (userData.IsNameTaken(name)) return May.None<string>();
		return May.Some(name);
	}
}*/