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
  <Namespace>DynaServeExtrasLib.Utils</Namespace>
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
	public IRoVar<ComboFiltersArr> ComboFilters { get; }
	
	public UserDataVars(bool testData)
	{
		var data = testData switch
		{
			false => UserDataFileApi.Load(),
			true => UserData.Test,
		};
		Combos = Var.Make(data.Combos).D(d);
		Filters = Var.Make(data.Filters).D(d);
		SelCombo = Var.Make(data.SelCombo.ToMaybe().Select(comboName => Combos.V.Single(e => e.Name == comboName))).D(d);
		//ComboFilters = Var.Make(data.C
		
		// TODO: no idea why this line fails !!
		// ComboFilters = Var.Expr(() => MkComboFilterSt(SelCombo.V, Combos.V, Filters.V));
		ComboFilters = Var.Expr(() => MkComboFilterSt(SelCombo.V, Combos.V, Filters.V));

		
		EnforceConstraints();
		SetupAutoSaving(testData);
	}
	
	private static ComboFiltersArr MkComboFilterSt(Maybe<Combo> selCombo, Combo[] combos, Filter[] filters) =>
		selCombo.IsSome(out var selComboVal) switch
			{
				true => new ComboFiltersArr(
					(
						from filterName in combos.Single(e => e.Name == selComboVal.Name).Filters
						let filter = filters.Single(e => e.Name == filterName)
						from propFilter in filter.PropFilters
						select propFilter
					).ToArray()
				),
				false => ComboFiltersArr.Empty,
			};

	
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
