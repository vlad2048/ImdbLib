<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeLib\bin\Debug\net7.0\DynaServeLib.dll</Reference>
  <Namespace>DynaServeLib</Namespace>
  <Namespace>static DynaServeLib.Nodes.Ctrls</Namespace>
  <Namespace>DynaServeLib.Nodes</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

#load "..\..\browse-libs\base-ui"
#load "..\1_layout\layout"

void Main()
{
	var panesVisibleVars = new PanesVisibleVars().D(D);
	
	Serv.Start(
		MkOpt(
			"1_layout",
			"4_pane-toggler"
		),
		
		UI_Layout.Make(
			mainOverlayNodes: Div().Txt("Filter Pane (main overlay)").VisibleWhen(panesVisibleVars.FltOpen),
			mainNodes: Enumerable.Range(0, 30).Select(e => Div().Txt($"item_{e}")).ToArray(),
			asideNodes: UI_PaneToggler.Make(panesVisibleVars)
		)
	);
}


public class PanesVisibleVars : IDisposable
{
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();
	
	public IRwVar<bool> FltOpen { get; }
	public IRwVar<bool> TagOpen { get; }
	public IRwVar<bool> OptOpen { get; }
	public IRwVar<bool> LogOpen { get; }
	
	public PanesVisibleVars()
	{
		FltOpen = Var.Make(false).D(d);
		TagOpen = Var.Make(false).D(d);
		OptOpen = Var.Make(false).D(d);
		LogOpen = Var.Make(false).D(d);
		ToggleVarUtils.KeepOnlyOneOn(FltOpen, TagOpen, OptOpen).D(d);
	}
}


public static class UI_PaneToggler
{
	public static HtmlNode Make(PanesVisibleVars vars) =>
		Div("pane-toggler").Wrap(
			IconToggle(vars.FltOpen, "fa-solid fa-filter"),
			IconToggle(vars.TagOpen, "fa-solid fa-folder-tree"),
			IconToggle(vars.OptOpen, "fa-solid fa-gear"),
			IconToggle(vars.LogOpen, "fa-solid fa-file-lines")
		);
}


static class ToggleVarUtils
{
	public static IDisposable KeepOnlyOneOn(params IRwVar<bool>[] rxVars)
	{
		var d = new Disp();
		foreach (var rxVar in rxVars)
		{
			rxVar.Where(e => e).Subscribe(_ =>
			{
				foreach (var rxVarOther in rxVars.Where(e => e != rxVar))
					rxVarOther.V = false;				
			}).D(d);
		}
		return d;
	}
}