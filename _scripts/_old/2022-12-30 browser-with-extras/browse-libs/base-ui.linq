<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeLib\bin\Debug\net7.0\DynaServeLib.dll</Reference>
  <Namespace>DynaServeLib</Namespace>
  <Namespace>DynaServeLib.Logging</Namespace>
  <Namespace>DynaServeLib.Nodes</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>static DynaServeLib.Nodes.Ctrls</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

void Main()
{
	
}


private static readonly SerialDisp<Disp> serD = new();
public static Disp D = null!;

void OnStart()
{
	serD.Value = null;
	serD.Value = D = new Disp();
}


public static Action<ServOpt> MkOptDbg(params string[] cssFolders) =>
	opt =>
	{
		MkOpt(cssFolders)(opt);
		opt.ShowDynaServLibVersion = true;
	};


public static Action<ServOpt> MkOpt(params string[] cssFolders) =>
	opt =>
	{
		opt.Logr = new NullLogr();
		opt.PlaceWebSocketsHtmlManually = true;

		var fontawesomeFolder = Fold.FindFolder("fontawesome");
		var manifestFolder = Fold.FindFolder("manifest");
		
		// Common CSS
		opt.AddCss(Fold.FindFolder("0_css-common"));
		// Base Css
		opt.AddCss(Fold.FindFolder("browse-libs"));
		// Icons
		opt.AddImg(Fold.FindFolder("icons"));

		// FontAwesome
		opt.AddCss(Path.Combine(fontawesomeFolder, "css"));
		opt.AddFonts(Path.Combine(fontawesomeFolder, "webfonts"));
		
		// Manifest
		opt.AddImg(manifestFolder);
		opt.SetManifest(Path.Combine(manifestFolder, "manifest.json"));
		
		// Extra CSS
		opt.AddCss(cssFolders.SelectToArray(Fold.FindFolder));
	};


public static T[] Arr<T>(params T[] arr) => arr;
public static T[] ArrOpt<T>(params T?[] arr) where T : class => arr.Where(e => e != null).SelectToArray(e => e!);
public static HtmlNode TBtn(string? cls = null) => new HtmlNode("button").Cls(cls);
public static HtmlNode THeader(string? cls = null) => new HtmlNode("header").Cls(cls);
public static HtmlNode TMain(string? cls = null) => new HtmlNode("main").Cls(cls);
public static HtmlNode TFooter(string? cls = null) => new HtmlNode("footer").Cls(cls);
public static HtmlNode TSection(string? cls = null) => new HtmlNode("section").Cls(cls);
public static HtmlNode TAside(string? cls = null) => new HtmlNode("aside").Cls(cls);
public static HtmlNode TH1(string? cls = null) => new HtmlNode("h1").Cls(cls);
public static HtmlNode TH2(string? cls = null) => new HtmlNode("h2").Cls(cls);
public static HtmlNode TH3(string? cls = null) => new HtmlNode("h3").Cls(cls);
public static HtmlNode TH4(string? cls = null) => new HtmlNode("h4").Cls(cls);
public static HtmlNode TH5(string? cls = null) => new HtmlNode("h5").Cls(cls);
public static HtmlNode TH6(string? cls = null) => new HtmlNode("h6").Cls(cls);


public static HtmlNode IconToggle(IRwVar<bool> isOn, string cls) =>
	new HtmlNode("i")
		.Cls(isOn.SelectVar(on => on switch
		{
			true => $"{cls} base-icontoggle base-icontoggle-on",
			false => $"{cls} base-icontoggle base-icontoggle-off",
		}))
		.OnClick(() => isOn.V = !isOn.V);


public static HtmlNode IconBtn(string cls, Action action) => IconBtn(cls, async () => action());

public static HtmlNode IconBtn(string cls, Func<Task> action) =>
	Btn("", action).Cls("base-iconbtn").Wrap(
		new HtmlNode("i").Cls(cls)
	);

public static class HtmlNodeExtraExt
{
	public static IEnumerable<T> AppendRange<T>(this IEnumerable<T> source, IEnumerable<T> others)
	{
		var list = source.ToList();
		list.AddRange(others);
		return list;
	}
	
	public static HtmlNode ClsOn(this HtmlNode node, string clsBase, bool isOn) => isOn switch
	{
		false => node.Cls(clsBase),
		true => node.Cls($"{clsBase} {clsBase}-on"),
	};

	/*public static HtmlNode WrapMay<T>(this HtmlNode node, IRoVar<Maybe<T>> rxMayVar, Func<T, HtmlNode> makeFun) =>
		node.Wrap(
			rxMayVar.ToUnit(),
			() => rxMayVar.V.IsSome(out var val) switch
			{
				true => makeFun(val),
				false => Array.Empty<HtmlNode>()
			}
		);*/
}



public static class DataUtils
{
	public static readonly string UserDataFolder = FindUserDataFolder();
	
	public static readonly string UserDataFile = Path.Combine(UserDataFolder, "user-data.json");	
	public static readonly string TestMoviesFile = Path.Combine(UserDataFolder, "test-movies.json");	
	
	private static string FindUserDataFolder()
	{
		var fold = Path.GetDirectoryName(Util.CurrentQueryPath)!;
		while (Path.GetFileName(fold) != "_scripts" && Path.GetFileName(fold) != "imdb")
			fold = Path.GetDirectoryName(fold)!;
		return Path.Combine(fold, "browse-user-data");
	}	

}


public static class Fmt
{
	public static string FmtScore(this byte score) => $"{(int)(score / 10.0):F1}";
}

public static class ImdbMaybeExt
{
	public static bool IsSomeAndEqualTo<T>(this Maybe<T> may, T val) => may.IsSome(out var mayVal) switch
	{
		true => mayVal.Equals(val),
		false => false
	};
	public static bool IsSomeAndVerifies<T>(this Maybe<T> may, Func<T, bool> predicate) => may.Select(predicate).FailWith(false);
}

public static class ClsExt
{
	public static string AddCls(this string cls, string clsExtra) => $"{cls} {clsExtra}";
	public static string AddClsIf(this string cls, string clsExtra, bool cond) => cond switch
	{
		false => cls,
		true => cls.AddCls(clsExtra)
	};
}


public static class ArrExt
{
	public static HtmlNode[] AddNodeIf(this HtmlNode node, HtmlNode nodeExtra, bool cond) => new [] { node }.AddNodeIf(nodeExtra, cond);
	
	public static HtmlNode[] AddNodeIf(this HtmlNode[] nodes, HtmlNode nodeExtra, bool cond) => cond switch
	{
		false => nodes,
		true => nodes.Append(nodeExtra).ToArray()
	};
	
	public static T[] ArrAdd<T>(this T[] arr, T elt) { var list = arr.ToList(); list.Add(elt); return list.ToArray(); }
	public static T[] ArrDel<T>(this T[] arr, T elt) { var list = arr.ToList(); var idx = list.IndexOf(elt); if (idx == -1) throw new ArgumentException("element not found"); list.RemoveAt(idx); return list.ToArray(); }
	public static T[] ArrRepl<T>(this T[] arr, T eltPrev, T eltNext) { var list = arr.ToList(); var idx = list.IndexOf(eltPrev); if (idx == -1) throw new ArgumentException("element not found"); list.RemoveAt(idx); list.Insert(idx, eltNext); return list.ToArray(); }
	public static T[] ArrToggle<T>(this T[] arr, T elt) => arr.Contains(elt) switch
	{
		false => arr.ArrAdd(elt),
		true => arr.ArrDel(elt)
	};
	
	public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		var idx = 0;
		foreach (var elt in source)
		{
			if (predicate(elt)) return idx;
			idx++;
		}
		throw new ArgumentException("Cannot find element");
	}
	
	private static readonly Random random = new();

    public static T[] Shuffle<T>(this T[] array)
    {
        var n = array.Length;
        for (var i = 0; i < n; i++)
        {
            var r = i + random.Next(n - i);
            var t = array[r];
            array[r] = array[i];
            array[i] = t;
        }
        return array;
    }
}


public static class JsonUtils
{
	private static readonly JsonSerializerOptions jsonOpt = new()
	{
		WriteIndented = true
	};
	static JsonUtils()
	{
		jsonOpt.Converters.Add(new MaybeSerializer<string>());
	}
	
	public static T Load<T>(string file, Func<T> makeEmptyFun, Func<T, bool>? integrityCheck = null)
	{
		Maybe<T> LoadInner()
		{
			if (!File.Exists(file))
			{
				$"File missing '{file}'".Dump();
				return May.None<T>();
			}
			try
			{
				var str = File.ReadAllText(file);
				var obj = JsonSerializer.Deserialize<T>(str, jsonOpt);
				if (obj == null)
				{
					"Deserializing to null".Dump();
					return May.None<T>();
				}
				if (integrityCheck != null && !integrityCheck(obj))
				{
					"Data deserialized OK but failed the integrity test".Dump();
					return May.None<T>();
				}
				return May.Some(obj);
			}
			catch (Exception ex)
			{
				$"Exception deserializing '{file}': {ex.Message}".Dump();
				return May.None<T>();
			}
		}
		
		if (LoadInner().IsNone(out var obj))
		{
			Save(file, makeEmptyFun());
		}
		
		return LoadInner().Ensure();
	}
	
	public static void Save<T>(string file, T obj)
	{
		var str = JsonSerializer.Serialize(obj, jsonOpt);
		File.WriteAllText(file, str);
	}
	
	
	
	public class MaybeSerializer<T> : JsonConverter<Maybe<T>> where T : class
	{
		public override Maybe<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			using var doc = JsonDocument.ParseValue(ref reader);
			var str = doc.Deserialize<T?>()!;
			return str.ToMaybe();
		}

		public override void Write(Utf8JsonWriter writer, Maybe<T> value, JsonSerializerOptions options)
		{
			var str = value.ToNullable();
			JsonSerializer.Serialize(writer, str);
		}
	}
}




// ***********
// * Private *
// ***********
static class Fold
{
	private const string UIFolderName = "browse-ui";
	
	public static string[] SearchFolders => new []
	{
		Path.GetDirectoryName(GetUIFolder())!,
	};
	
	public static string FindFolder(string name)
	{
		string? Recurse(string folder)
		{
			if (Path.GetFileName(folder) == name) return folder;
			var subDirs = Directory.GetDirectories(folder);
			foreach (var subDir in subDirs)
			{
				var subDirRes = Recurse(subDir);
				if (subDirRes != null)
					return subDirRes;
			}
			return null;
		}
		foreach (var searchFolder in SearchFolders)
		{
			var searchFolderRes = Recurse(searchFolder);
			if (searchFolderRes != null)
				return searchFolderRes;
		}
		throw new ArgumentException($"Failed to find folder '{name}'");
	}
	
	
	private static string GetUIFolder()
	{
		static string? Check(string folder)
		{
			var fullFolder = Path.GetFullPath(folder);
			return (Directory.Exists(fullFolder) && Path.GetFileName(fullFolder) == UIFolderName) switch
			{
				true => fullFolder,
				false => null
			};
		}
		
		var queryFolder = Path.GetDirectoryName(Util.CurrentQueryPath)!;
		
		return
			Check(Path.Combine(queryFolder, UIFolderName)) ??
			Check(Path.Combine(queryFolder, "..")) ??
			Check(Path.Combine(queryFolder, "..", "..")) ??
			throw new ArgumentException($"Failed to find '{UIFolderName}' folder");
	}
}