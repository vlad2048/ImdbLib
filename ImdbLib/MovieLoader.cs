using ImdbLib.Structs;
using ImdbLib.Utils;
using ImdbLib.Utils.Exts;

namespace ImdbLib;

public class MovieLoaderOpt
{
	public string DataFolder { get; set; } = Consts.DefaultDataFolder;
	public bool DbgUseSmallDatasets { get; set; } = false;

	internal MovieLoaderOpt() { }

	internal static MovieLoaderOpt Build(Action<MovieLoaderOpt>? optFun)
	{
		var opt = new MovieLoaderOpt();
		optFun?.Invoke(opt);
		return opt;
	}
}

public static class MovieLoader
{
	public static Movie[] Load(Action<MovieLoaderOpt>? optFun = null)
	{
		var opt = MovieLoaderOpt.Build(optFun);
		var fileApi = new FileApi(opt.DataFolder, opt.DbgUseSmallDatasets);
		return (
				from file in fileApi.GetScrapeFiles()
				from movie in file.LoadJson<Movie[]>()
				select movie
			)
			.ToArray();
	}
}