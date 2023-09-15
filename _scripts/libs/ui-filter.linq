<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\ImdbLib\ImdbLib\bin\Debug\net7.0\ImdbLib.dll</Reference>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>ImdbLib.Structs</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>LINQPadHero.UICode</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Disposables</Namespace>
</Query>

#load ".\ui"

void Main()
{
}


public record FieldFilter(
	Control UI,
	Func<Movie, bool> Predicate,
	IObservable<Unit> WhenChanged,
	Action Reset
);

public static class FilterMaker
{
	public static FieldFilter Combine(this FieldFilter[] arr) => new(
		new Div(arr.Select(e => e.UI)).BigSpace(),
		e => arr.All(f => f.Predicate(e)),
		arr.Select(e => e.WhenChanged).Merge(),
		() => { foreach (var elt in arr) elt.Reset(); }
	);
	
	public static (FieldFilter, IDisposable) Text(string name, Func<Movie, string> selFun)
	{
		var d = new Disp();
		var rxV = Var.Make("").D(d);
		TextBox ctrl;
		var ui = new Span(
			new Span($"{name}: "),
			ctrl = new TextBox(rxV.V, onTextInput: c => rxV.V = c.Text).WithCss("max-width", "150px")
		);
		return new FieldFilter(
			ui,
			e => StringSearchUtils.IsMatch(selFun(e), rxV.V),
			rxV.ToUnit(),
			() =>
			{
				rxV.V = "";
				ctrl.Text = "";
			}
		).WithD(d);
	}
	
	public static (FieldFilter, IDisposable) TextIncExc(string name, Func<Movie, string> selFun)
	{
		var d = new Disp();
		var rxIncV = Var.Make("").D(d);
		var rxExcV = Var.Make("").D(d);
		TextBox ctrlInc, ctrlExc;
		var ui = new Span(
			new Span($"{name}: "),
			new Span(
				ctrlInc = new TextBox(rxIncV.V, onTextInput: c => rxIncV.V = c.Text).WithCss("max-width", "150px"),
				ctrlExc = new TextBox(rxExcV.V, onTextInput: c => rxExcV.V = c.Text).WithCss("max-width", "150px")
			).Vert()
		);
		return new FieldFilter(
			ui,
			e =>
			{
				var str = selFun(e).ToLowerInvariant();
				var incs = rxIncV.V.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(e => e.Trim().ToLowerInvariant()).ToArray();
				var excs = rxExcV.V.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(e => e.Trim().ToLowerInvariant()).ToArray();
				return
					incs.All(inc => str.Contains(inc, StringComparison.InvariantCultureIgnoreCase)) &&
					excs.All(exc => !str.Contains(exc, StringComparison.InvariantCultureIgnoreCase));
			},
			Observable.Merge(rxIncV, rxExcV).ToUnit(),
			() =>
			{
				rxIncV.V = "";
				rxExcV.V = "";
				ctrlInc.Text = "";
				ctrlExc.Text = "";
			}
		).WithD(d);
	}
	
	public static (FieldFilter, IDisposable) Range(string name, Func<Movie, int> selFun, Func<int, string> dispFun, int min, int max)
	{
		var d = new Disp();
		var rxMin = Var.Make(min).D(d);
		var rxMax = Var.Make(max).D(d);
		var whenChanged = Observable.Merge(rxMin, rxMax).ToUnit();
		RangeControl ctrlMin, ctrlMax;
		var ui = new Span(
			new Span($"{name}: "),
			new Span(
				ctrlMin = new RangeControl(min, max, rxMin.V).WhenChange(e => rxMin.V = e, d),
				ctrlMax = new RangeControl(min, max, rxMax.V).WhenChange(e => rxMax.V = e, d)
			).Vert(),
			new Span().React(whenChanged, (s, _) => s.Text = $"{dispFun(rxMin.V)} - {dispFun(rxMax.V)}").D(d)
		).Space();
		return new FieldFilter(
			ui,
			e =>
				(rxMin.V == min || selFun(e) >= rxMin.V) &&
				(rxMax.V == max || selFun(e) <= rxMax.V),
			whenChanged,
			() => {
				rxMin.V = min;
				rxMax.V = max;
				ctrlMin.Value = min;
				ctrlMax.Value = max;
			}
		).WithD(d);
	}
	
	private static RangeControl WhenChange(this RangeControl ctrl, Action<int> fun, IRoDispBase d)
	{
		void Evt(object? o, EventArgs e)
		{
			fun(ctrl.Value);
		}
		ctrl.ValueInput += Evt;
		Disposable.Create(() => ctrl.ValueInput -= Evt).D(d);
		return ctrl;
	}
	
	private static (T, IDisposable) WithD<T>(this T obj, IDisposable d) => (obj, d);
}





public static class StringSearchUtils
{
	public static bool IsMatch(string itemStr, string searchStr) =>
		searchStr
			.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.All(part => itemStr.Contains(part, StringComparison.InvariantCultureIgnoreCase));
}