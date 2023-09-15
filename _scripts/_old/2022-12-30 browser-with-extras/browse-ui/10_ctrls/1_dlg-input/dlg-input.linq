<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeLib\bin\Debug\net7.0\DynaServeLib.dll</Reference>
  <Namespace>DynaServeLib</Namespace>
  <Namespace>DynaServeLib.Nodes</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>static DynaServeLib.Nodes.Ctrls</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Reactive</Namespace>
</Query>

#load "..\..\..\browse-libs\base-ui"
#load "..\..\1_layout\layout"


record DlgInputRec(string Name, string[] Signs);

void Main()
{
	typeof(Serv).Assembly.GetManifestResourceNames().Dump();
	return;
	
	var rec = May.None<DlgInputRec>();
	
	Serv.Start(
		MkOpt(
			"1_layout",
			"1_dlg-input"
		),
		
		UI_Layout.Make(
			asideNodes: new HtmlNode("button").Txt("Ask").OnClick(async () =>
			{
				var mayReader = await DlgInput.Make(
					rec.IsSome() ? "Edit Rec" : "Create Rec",
					dlg =>
					{
						dlg.EditString("name", "Name", rec.Select(e => e.Name).FailWith(""));
						dlg.EditMultipleChoices("sign", "Sign", rec.Select(e => e.Signs).FailWith(Array.Empty<string>()), new [] { "Capricorn", "Leo", "Fish", "Gemini", "Sagitarius" });
						dlg.ValidFun = r => r.HasNonEmptyString("name");
					}
				);
				if (mayReader.IsSome(out var reader))
					rec = May.Some(new DlgInputRec(
						reader.GetString("name"),
						reader.GetMultipleChoices("sign")
					));
			})
		)
	);
}


public interface IDlgSetup
{
	Func<IDlgReader, bool>? ValidFun { get; set; }
	void EditString(string key, string label, string prevVal);
	void EditSingleChoice(string key, string label, Maybe<string> prevVal, string[] choices);
	void EditMultipleChoices(string key, string label, string[] prevVal, string[] choices);
}

public interface IDlgReader
{
	IReadOnlyDictionary<string, object> GetMap();
	string GetString(string key);
	string[] GetMultipleChoices(string key);
}

public static class DlgReaderExt
{
	public static bool HasNonEmptyString(this IDlgReader reader, string key) => reader.GetMap().TryGetValue(key, out var val) switch
	{
		false => false,
		true => val switch
		{
			string s => !string.IsNullOrWhiteSpace(s),
			_ => false
		}
	};
}


public static class DlgInput
{
	private static HtmlNode Dlg(string? cls = null) => new HtmlNode("dialog").Cls(cls);
	
	
	public static async Task<Maybe<IDlgReader>> Make(string title, Action<IDlgSetup> setupFun)
	{
		var d = new Disp();
		var slim = new SemaphoreSlim(0).D(d);
		using var dlgSetup = new DlgSetup();
		setupFun(dlgSetup);
		var edits = dlgSetup.Edits;
		var valMap = new ValMap(edits).D(d);
		var resultReader = May.None<IDlgReader>();
		var reader = new DlgReader(valMap);
		
		void ExecFinish()
		{
			slim.Release();
			d.Dispose();
		}
		
		void OnClick_Cancel()
		{
			resultReader = May.None<IDlgReader>();
			ExecFinish();
		}
		
		
		void OnClick_OK()
		{
			if (dlgSetup.ValidFun != null && !dlgSetup.ValidFun(reader)) throw new ArgumentException();
			resultReader = May.Some<IDlgReader>(reader);
			ExecFinish();
		}
		
		
		
		Serv.AddNodeToBody(
			Dlg("dlginput")
				.Attr("onkeyup", "{ if (event.keyCode === 13) { const elt = document.querySelector('.dlginput-btn-ok'); if (!!elt) elt.click(); } } ")
				.Wrap(Div().Wrap(
			
				THeader().Txt(title),
				
				TMain().Wrap(edits.Select((edit, editIdx) =>
					Div("dlginput-edit").Wrap(
						Div("dlginput-edit-label").Txt(edit.Label),
						edit.MakeUI(val => valMap.SetVal(edit.Key, val), editIdx == 0)
					)
				)),
				
				TFooter().Wrap(
					Div().Wrap(
						Btn("Cancel", OnClick_Cancel).Cls("dlginput-btn-cancel")
					),
					Div().Wrap(
						Btn("OK", OnClick_OK).Cls("dlginput-btn-ok")
							.EnableWhen(valMap.WhenChanged.Select(_ => dlgSetup.ValidFun == null || dlgSetup.ValidFun(reader)))
					)
				)
			))
		).D(d);
		
		await slim.WaitAsync();
		return resultReader;
	}
	
	private class ValMap : IDisposable
	{
		private readonly Disp d = new();
		public void Dispose() => d.Dispose();
		
		private readonly IReadOnlyList<IEdit> edits;
		private readonly Dictionary<string, object> map = new();
		private readonly ISubject<Unit> whenChanged;
		
		public IReadOnlyDictionary<string, object> Map => map.AsReadOnly();
		public IObservable<Unit> WhenChanged => whenChanged.AsObservable().Prepend(Unit.Default);
		
		public ValMap(IReadOnlyList<IEdit> edits)
		{
			this.edits = edits;
			whenChanged = new Subject<Unit>().D(d);
		}
		public void SetVal(string key, object val)
		{
			var actType = val.GetType();
			var expType = edits.Single(e => e.Key == key).ValType;
			if (actType != expType) throw new ArgumentException($"An IEdit value was set with the wrong type: {actType} (expected: {expType})");
			map[key] = val;
			whenChanged.OnNext(Unit.Default);
		}
	}
	
	
	private interface IEdit : IDisposable
	{
		string Key { get; }
		string Label { get; }
		Type ValType { get; }
		HtmlNode MakeUI(Action<object> setFun, bool isFirst);
	}
	
	private abstract record EditBase(
		string Key,
		string Label,
		Type ValType
	) : IEdit
	{
		protected Disp D = new();
		public void Dispose() => D.Dispose();
		
		public abstract HtmlNode MakeUI(Action<object> setFun, bool isFirst);
	}
	
	private record StringEdit(string Key, string Label, string InitVal) : EditBase(Key, Label, typeof(string))
	{
		public override HtmlNode MakeUI(Action<object> setFun, bool isFirst)
		{
			setFun(InitVal);
			return new HtmlNode("input")
				.Attr("type", "text")
				.Attr("value", InitVal)
				.AutofocusIfFirst(isFirst)
				.HookArg("input", v => setFun(v), "this.value");
		}
	}
	
	private record SingleChoiceEdit(string Key, string Label, Maybe<string> InitVal, string[] Choices) : EditBase(Key, Label, typeof(string))
	{
		public override HtmlNode MakeUI(Action<object> setFun, bool isFirst)
		{
			if (InitVal.IsSome(out var initValVal)) setFun(initValVal);
			var mayVar = Var.Make(InitVal).D(D);
			mayVar
				.WhenSome()
				.Subscribe(val => setFun(val)).D(D);
			return
				Div("dlginput-multiplechoices").Wrap(
					mayVar.ToUnit(),
					() => Choices.Select(choice =>
						Div().Txt(choice).Cls(AddClsOnIf("dlginput-multiplechoices-item", mayVar.V.IsSomeAndEqualTo(choice)))
							.OnClick(() => mayVar.V = May.Some(choice))
					)
				);
		}
	}
	
	private record MultipleChoicesEdit(string Key, string Label, string[] InitVal, string[] Choices) : EditBase(Key, Label, typeof(string[]))
	{
		public override HtmlNode MakeUI(Action<object> setFun, bool isFirst)
		{
			setFun(InitVal);
			var arrVar = Var.Make(InitVal).D(D);
			arrVar.Subscribe(arr => setFun(arr)).D(D);
			return
				Div("dlginput-multiplechoices").Wrap(
					arrVar.ToUnit(),
					() => Choices.Select(choice =>
						Div().Txt(choice).Cls(AddClsOnIf("dlginput-multiplechoices-item", arrVar.V.Contains(choice)))
							.OnClick(() => arrVar.V = arrVar.V.ArrToggle(choice))
					)
				);
		}
	}
	
	private static HtmlNode AutofocusIfFirst(this HtmlNode node, bool isFirst) => isFirst switch
	{
		true => node.Attr("autofocus", ""),
		false => node
	};
	
	private class DlgSetup : IDlgSetup, IDisposable
	{
		private readonly Disp d = new();
		public void Dispose() => d.Dispose();
		
		private readonly List<IEdit> edits = new();
		
		public IReadOnlyList<IEdit> Edits => edits.AsReadOnly();		
		
		public Func<IDlgReader, bool>? ValidFun { get; set; }
		public void EditString(string key, string label, string prevVal) => AddEdit(new StringEdit(key, label, prevVal));
		public void EditSingleChoice(string key, string label, Maybe<string> prevVal, string[] choices) => AddEdit(new SingleChoiceEdit(key, label, prevVal, choices));
		public void EditMultipleChoices(string key, string label, string[] prevVal, string[] choices) => AddEdit(new MultipleChoicesEdit(key, label, prevVal, choices));
		
		private void AddEdit(IEdit edit)
		{
			if (Edits.Any(e => e.Key == edit.Key)) throw new ArgumentException($"Same key added multiple times: '{edit.Key}'");
			edits.Add(edit.D(d));
		}
	}
	
	
	private class DlgReader : IDlgReader
	{
		private readonly ValMap valMap;
		
		public DlgReader(ValMap valMap) => this.valMap = valMap;
		
		public IReadOnlyDictionary<string, object> GetMap() => valMap.Map;
		public string GetString(string key) => (string)valMap.Map[key];
		public string[] GetMultipleChoices(string key) => (string[])valMap.Map[key];
	}
	
	
	private static string AddClsOnIf(string cls, bool cond) => cond switch
	{
		false => cls,
		true => $"{cls} {cls}-on"
	};
}