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
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>static System.Windows.Forms.VisualStyles.VisualStyleElement</Namespace>
  <Namespace>System.Runtime.CompilerServices</Namespace>
</Query>

#load "..\..\..\browse-libs\base-ui"
#load "..\..\1_layout\layout"
#load "..\..\10_ctrls\1_dlg-input\dlg-input"

void Main()
{
	var arr = new EditListRec[]
	{
		new("Vlad", false),
		new("Milou", false),
		new("Erik", false),
		new("Goncalo", false),
		new("Marek", false),
	};
	var list = Var.Make(arr).D(D);
	var editor = new EditList<EditListRec>(
		list,
		"Items",
		DlgRec,
		opt =>
		{
			opt.SelectMode = EditListSelectMode.Multiple;
			opt.Width = 300;
		}
	).D(D);

	Serv.Start(
		opt =>
		{
			MkOpt(
				"1_dlg-input",
				"2_edit-list"
			)(opt);
			opt.Port = 7001;
		},
		
		editor.UI
	);
}



private static async Task<Maybe<EditListRec>> DlgRec(Maybe<EditListRec> prev)
{
	const string keyName = "name";

	EditListRec Mk(IDlgReader r) => new(r.GetString(keyName), prev.Select(e => e.Enabled).FailWith(true));
	
	var mayRead = await DlgInput.Make(
		prev.IsSome() ? "Edit Rec" : "Add Rec",
		dlg =>
		{
			dlg.ValidFun = r => !string.IsNullOrWhiteSpace(Mk(r).Name);
			dlg.EditString(keyName, "Name", prev.Select(e => e.Name).FailWith(""));
		}
	);
	
	return mayRead.Select(Mk);
}

record EditListRec(
	string Name,
	bool Enabled
)
{
	public override string ToString() => Name;
}








public enum EditListSelectMode
{
	Single,
	Multiple,
};

public enum ItemSelStatus
{
	None,
	Sel,
	SelLast
}

public static class EditListCls
{
	public static string DefaultItemCls<T>(ItemNfo<T> Nfo) => Nfo.SelStatus switch
	{
		ItemSelStatus.None => "editlist-item",
		ItemSelStatus.Sel => "editlist-item editlist-item-selmulti",
		ItemSelStatus.SelLast => "editlist-item editlist-item-selmulti editlist-item-selsingle",
		_ => throw new ArgumentException()
	};
}

public record ItemNfo<T>(T Item, ItemSelStatus SelStatus);





public class EditListOpt<T>
{
	public EditListSelectMode SelectMode { get; set; } = EditListSelectMode.Single;
	public int? Width { get; set; }
	public Func<ItemNfo<T>, HtmlNode>? ItemDispFun { get; set; }
	public IObservable<Unit>? ItemDispWhenChange { get; set; }
	
	private EditListOpt() {}
	internal static EditListOpt<T> Build(Action<EditListOpt<T>>? optFun)
	{
		var opt = new EditListOpt<T>();
		optFun?.Invoke(opt);
		return opt;
	}
}





public class EditList<T> : IDisposable
{
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();

	private static readonly Func<ItemNfo<T>, HtmlNode> defaultItemDispFun = nfo =>
		Div(EditListCls.DefaultItemCls(nfo)).Txt($"{nfo.Item}");
	
	private readonly IRwVar<T[]> list;
	private readonly EditListOpt<T> opt;
	private readonly Func<ItemNfo<T>, HtmlNode> itemDispFun;
	private readonly IRwVar<Maybe<T>> selItem;
	private readonly IRwVar<T[]> selItems;
	private IRoVar<int> SelItemIndex { get; }
	
	public HtmlNode UI { get; }
	public IRoVar<Maybe<T>> SelItem => selItem.ToReadOnly();
	public IRoVar<T[]> SelItems => selItems.ToReadOnly();
	public IObservable<Unit> WhenChanged => Observable.Merge(
		list.ToUnit(),
		SelItem.ToUnit(),
		SelItems.ToUnit(),
		opt.ItemDispWhenChange ?? Observable.Never<Unit>()
	).Throttle(TimeSpan.Zero).Prepend(Unit.Default);
	
	public EditList(
		IRwVar<T[]> list,
		string title,
		Func<Maybe<T>, Task<Maybe<T>>> createFun,
		Action<EditListOpt<T>>? optFun = null
	)
	{
		this.list = list;
		opt = EditListOpt<T>.Build(optFun);
		itemDispFun = opt.ItemDispFun ?? defaultItemDispFun;
		selItem = Var.Make(May.None<T>()).D(d);
		selItems = Var.Make(Array.Empty<T>()).D(d);
		// TODO: not sure why this line fails !
		//SelItemIndex = Var.Expr(() => ComputeSelItemIndex(SelItem.V));
		SelItemIndex = Var.Expr(() => ComputeSelItemIndex(selItem.V), list.ToUnit());
		
		
		UI =
			Div("editlist").SetWidthOpt(opt.Width).Wrap(
			
				Div("editlist-titlelist").OnClick(OnNoItemClicked).Wrap(
				
					TH3().Txt(title),
					
					Div("editlist-list").Wrap(
						WhenChanged,
						() => list.V
							.Select(ComputeItemDispNfo)
							.Select(nfo =>
								itemDispFun(nfo)
									.OnClick(() =>
									{
										OnItemClicked(nfo.Item);
									}, true)
							)
					)
				),
				
				
				Div("editlist-btnrow").Wrap(
				
						IconBtn("fa-solid fa-up-long",async  () =>
						{
							MoveSelItemUp();
						}).EnableWhen(SelItemIndex.Select(idx => idx != -1 && idx > 0)),
						
						IconBtn("fa-solid fa-down-long", () =>
						{
							MoveSelItemDown();
						}).EnableWhen(SelItemIndex.Select(idx => idx != -1 && idx < list.V.Length - 1)),
						
						IconBtn("fa-solid fa-pen-to-square", async () =>
						{
							if ((await createFun(May.Some(SelItem.V.Ensure()))).IsNone(out var item)) return;
							ReplaceSelItem(item);
						}).EnableWhen(SelItem.Select(e => e.IsSome())),

						IconBtn("fa-solid fa-plus", async () =>
						{
							if ((await createFun(May.None<T>())).IsNone(out var item)) return;
							AddItem(item);
						}),

						IconBtn("fa-solid fa-trash", () =>
						{
							DelSelItem();
						}).EnableWhen(SelItem.Select(e => e.IsSome()))
				)
			);
	}
	
	
	public void TransformSelItemEnsure(Func<T, T> transformFun) => ReplaceSelItem(transformFun(SelItem.V.Ensure()));
	
	

	private void OnNoItemClicked()
	{
		selItem.V = May.None<T>();
		selItems.V = Array.Empty<T>();
	}
	
	private void OnItemClicked(T item)
	{
		switch (opt.SelectMode)
		{
			case EditListSelectMode.Single:
				selItem.V = May.Some(item);
				selItems.V = new [] { item };
				break;
			case EditListSelectMode.Multiple:
				var isMultiSel = selItems.V.Contains(item);
				if (isMultiSel)
				{
					selItem.V = May.None<T>();
					selItems.V = selItems.V.ArrDel(item);
				}
				else
				{
					selItem.V = May.Some(item);
					selItems.V = selItems.V.ArrAdd(item);
				}
				break;
			default:
				throw new ArgumentException();
		}
	}
	
	private void MoveSelItemUp()
	{
		var idx = SelItemIndex.V;
		if (idx == -1 || idx <= 0) throw new ArgumentException($"Err in MoveSelItemUp idx={idx} (list.length={list.V.Length})");
		var elt = SelItem.V.Ensure();
		var l = list.V.ToList();
		
		l.RemoveAt(idx);
		l.Insert(idx - 1, elt);
		list.V = l.ToArray();
		
		selItems.V = new [] { elt };
	}

	private void MoveSelItemDown()
	{
		var idx = SelItemIndex.V;
		if (idx == -1 || idx >= list.V.Length - 1) throw new ArgumentException($"Err in MoveSelItemDown idx={idx} list.length={list.V.Length}");
		var elt = SelItem.V.Ensure();
		var l = list.V.ToList();
		
		l.RemoveAt(idx);
		l.Insert(idx + 1, elt);
		list.V = l.ToArray();

		selItems.V = new [] { elt };
	}
	private void ReplaceSelItem(T itemNext)
	{
		var itemPrev = SelItem.V.Ensure();
		
		list.V = list.V.ArrRepl(itemPrev, itemNext);
		selItem.V = May.Some(itemNext);
		selItems.V = new [] { itemNext };
	}
	
	private void AddItem(T item)
	{
		list.V = list.V.ArrAdd(item);
		selItem.V = May.Some(item);
		selItems.V = new [] { item };
	}
	
	private void DelSelItem()
	{
		var item = SelItem.V.Ensure();
		list.V = list.V.ArrDel(item);
		selItem.V = May.None<T>();
		var isMultiSel = selItems.V.Contains(item);
		if (isMultiSel)
			selItems.V = selItems.V.ArrDel(item);
	}
	
	private int ComputeSelItemIndex(Maybe<T> selItemMayVal) => selItemMayVal.IsSome(out var selItemVal) switch
	{
		true => list.V.ToList().IndexOf(selItemVal),
		false => -1
	};
	
	private ItemNfo<T> ComputeItemDispNfo(T item)
	{
		var selStatus = SelItems.V.Contains(item) switch
		{
			false => ItemSelStatus.None,
			true => SelItem.V.IsSomeAndEqualTo(item) switch
			{
				false => ItemSelStatus.Sel,
				true => ItemSelStatus.SelLast,
			}
		};
		return new ItemNfo<T>(item, selStatus);
	}
}




static class EditListNodeExt
{
	public static HtmlNode SetWidthOpt(this HtmlNode node, int? width) => width switch
	{
		null => node,
		int.MaxValue => node.Attr("style", $"width: 100%"),
		not null => node.Attr("style", $"width: {width}px"),
	};

	public static void Inspect<T>(this IObservable<T> obs, [CallerArgumentExpression(nameof(obs))] string? message = null) => obs.Subscribe(e => $"{message}: {e}".Dump()).D(D);
}
