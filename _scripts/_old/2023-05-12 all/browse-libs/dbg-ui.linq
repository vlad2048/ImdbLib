<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeExtrasLib\bin\Debug\net7.0\DynaServeExtrasLib.dll</Reference>
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeLib\bin\Debug\net7.0\DynaServeLib.dll</Reference>
  <NuGetReference>AngleSharp.Diffing</NuGetReference>
  <Namespace>DynaServeLib</Namespace>
  <Namespace>DynaServeLib.Nodes</Namespace>
  <Namespace>DynaServeLib.Serving.Debugging.Structs</Namespace>
  <Namespace>DynaServeLib.Utils.Exts</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>static DynaServeExtrasLib.Utils.HtmlNodeExtraMakers</Namespace>
  <Namespace>static DynaServeLib.Nodes.Ctrls</Namespace>
  <Namespace>AngleSharp.Diffing</Namespace>
  <Namespace>AngleSharp.Html.Dom</Namespace>
  <Namespace>AngleSharp.Dom</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

#load ".\base-ui"



void Main()
{
	var isOn = Var.Make(false).D(D);
	var WhenOn = isOn.AsObservable();
	
	Serv.Start(
		opt =>
		{
			opt.AddScriptCss("test", """
				.row {
					display: flex;
				}
				"""
			);
		},
		Div("row").Wrap(
			TBtn().Txt("Toggle").OnClick(() =>
			{
				//var newVal = !lastVal;
				//whenOn.OnNext(newVal);
				isOn.V = !isOn.V;
				//lastVal = newVal;
			}),

			TBtn().EnableWhen(WhenOn).Txt("Dummy").OnClick(() =>
			{
				"DummyClk".Dump();
			})
		)
	).D(D);
}



public static class DbgUI
{
	private const string OutFolder = @"C:\tmp\dynaserv-diffs";
	private static string ServFile => Path.Combine(OutFolder, "server.html");
	private static string ClientFile => Path.Combine(OutFolder, "client.html");
	
	private static string servTxt = "";
	private static string clientTxt = "";
	
	public static object Make()
	{
		var dc = new DumpContainer();
		Util.InvokeScript(false, "eval", "document.body.style = 'font-family: consolas'");
		var btnDbg = new Button("Dbg", async _ =>
		{
			var snap = await Serv.Dbg.GetSnap();
			servTxt = snap.ServerDom.Fmt();
			clientTxt = snap.ClientDom.Fmt();
			DbgLogic.Run(snap, dc);
		});
		
		var btnSave = new Button("Save", _ =>
		{
			File.WriteAllText(ServFile, servTxt);
			File.WriteAllText(ClientFile, clientTxt);
		});
		
		return Util.VerticalRun(
			Util.HorizontalRun(true,
				btnDbg,
				btnSave
			),
			dc
		);
	}
}



static class DbgLogic
{
	public static void Run(DbgSnap snap, DumpContainer dc)
	{
		dc.UpdateContent($"{DateTime.Now}");
		var isStep_1_OK = Step_1_Diff_ServerVsClient_Doms(snap, dc);
		var isStep_2_OK = Step_2_CheckRefreshers_Vs_ServerAndClientDoms(snap, dc);
		Step_3_Print_RefresherBreakdown(snap, dc);
		if (isStep_1_OK && isStep_2_OK)
		{
			dc.AppendContent("              => ALL OK");
		}
	}
	
	private static bool Step_1_Diff_ServerVsClient_Doms(DbgSnap snap, DumpContainer dc)
	{
		var diffs = DiffBuilder
		    .Compare(snap.ServerDom.Body!.Fmt())
			.WithTest(snap.ClientDom.Body!.Fmt())
			.WithOptions(opt =>
			{
				opt.AddDefaultOptions();
			})
			.Build()
			.ToArray();
		
		var sb = new StringBuilder("Snap received");
		if (diffs.Length == 0)
			sb.Append(" -> no diffs found");
		else
			sb.Append($"  -> {diffs.Length} diffs found");
		dc.AppendContent(sb.ToString());
		return diffs.Length == 0;
	}
	
	
	private static bool Step_2_CheckRefreshers_Vs_ServerAndClientDoms(DbgSnap snap, DumpContainer dc)
	{
		var serverDom = snap.ServerDom;
		var clientDom = snap.ClientDom;
		var (servIds, servNoIdCnt) = serverDom.Body!.GetAllChildrenIds(dc);
		var (clientIds, clientNoIdCnt) = clientDom.Body!.GetAllChildrenIds(dc);
		var servStats = ComputeRefreshStats(servIds, snap.RefreshMap, servNoIdCnt, dc);
		var clientStats = ComputeRefreshStats(clientIds, snap.RefreshMap, clientNoIdCnt, dc);
		
		dc.AppendContent($"              -> Refreshers vs Serv   => {servStats}");
		dc.AppendContent($"                 Refreshers vs Client => {clientStats}");
		
		return servStats.IsOK && clientStats.IsOK;
	}
	
	private static void Step_3_Print_RefresherBreakdown(DbgSnap snap, DumpContainer dc)
	{
		static string MultStr(int n) => n switch
		{
			1 => "",
			> 1 => $"x{n}",
			_ => throw new ArgumentException()
		};
		
		var str = snap.RefreshMap.Values
			.SelectMany(e => e)
			.GroupBy(e => e)
			.OrderByDescending(grp => grp.Count())
			.Select(grp => $"{grp.Key}{MultStr(grp.Count())}")
			.JoinText(", ");

		dc.AppendContent($"              -> Breakdown => {str}");
	}
	
	
	
	
	
	private record RefreshStats(
		int TotalNodeCount,
		int TotalRefresherCount,
		int ForgottenRefreshers,
		int MissingRefreshers,
		int DupIds,
		int NoIdCnt
	)
	{
		public override string ToString() => IsOK switch
		{
			true => $"ok    (nodes:{TotalNodeCount} refreshers:{TotalRefresherCount})",
			false => $"error (nodes:{TotalNodeCount} refreshers:{TotalRefresherCount})  forgotten:{ForgottenRefreshers}  missing:{MissingRefreshers}  dupIds:{DupIds}  noidCnt:{NoIdCnt}",
		};
		public bool IsOK => ForgottenRefreshers == 0 && MissingRefreshers == 0 && DupIds == 0 && NoIdCnt == 0;
	}

	private static RefreshStats ComputeRefreshStats(string[] ids, Dictionary<string, string[]> map, int noidCnt, DumpContainer dc)
	{
		var totalNodeCount = ids.Length;
		var totalRefresherCount = map.Sum(kv => kv.Value.Length);
		var refreshIds = map.Keys.ToArray();
		var forgotten = refreshIds.WhereNotToArray(ids.Contains).Length;
		var missing = ids.WhereNotToArray(refreshIds.Contains).Length;
		if (missing > 0)
			dc.AppendContent($"Missing refreshers for Ids: {ids.WhereNotToArray(refreshIds.Contains).JoinText(", ")}");
		var dupIds = ids.Length - ids.Distinct().ToArray().Length;
		return new RefreshStats(totalNodeCount, totalRefresherCount, forgotten, missing, dupIds, noidCnt);
	}
	
	public static (string[], int) GetAllChildrenIds(this IElement elt, DumpContainer dc)
	{
		var list = new List<string>();
		var noidCnt = 0;
		void Recurse(IElement e)
		{
			if (e.Id != null)
				list.Add(e.Id);
			else
			{
				if (e.NodeName != "BODY" && e.ClassName != "DynaServVerCls")
				{
					dc.AppendContent($"NoId for {e.NodeName} cls:{e.ClassName}");
					noidCnt++;
				}
			}
			foreach (var ec in e.Children)
				Recurse(ec);
		}
		Recurse(elt);
		return (list.ToArray(), noidCnt);
	}
}