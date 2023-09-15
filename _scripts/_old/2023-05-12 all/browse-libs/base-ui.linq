<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeExtrasLib\bin\Debug\net7.0\DynaServeExtrasLib.dll</Reference>
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeLib\bin\Debug\net7.0\DynaServeLib.dll</Reference>
  <Namespace>DynaServeLib</Namespace>
  <Namespace>DynaServeLib.Logging</Namespace>
  <Namespace>DynaServeLib.Nodes</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>static DynaServeLib.Nodes.Ctrls</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>DynaServeExtrasLib.Components.EditListLogic</Namespace>
  <Namespace>DynaServeLib.Serving.FileServing.StructsEnum</Namespace>
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

public static class CfgUtils
{
	public static void BaseConfig(this ServOpt opt)
	{
		opt.RegisterEditList();
		//opt.Serve(FCat.Manifest, "manifest");
		//opt.Serve(FCat.Image, "manifest");
		//opt.Serve(FCat.Image, "icons");
	}
}





/*public static class HtmlNodeExtraExt
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
}*/



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