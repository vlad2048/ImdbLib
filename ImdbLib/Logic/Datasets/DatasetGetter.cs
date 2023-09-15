using System.Diagnostics;
using System.IO.Compression;
using ImdbLib.Logic.Datasets.Structs;
using ImdbLib.Logic.Datasets.TsvLoading;
using ImdbLib.Logic.Datasets.TsvLoading.Structs;
using ImdbLib.Logic.Datasets.TsvLoading.Utils;
using ImdbLib.Utils;
using ImdbLib.Utils.Exts;
using RestSharp;

namespace ImdbLib.Logic.Datasets;

static class DatasetGetter
{
	private static readonly DatasetKind[] activeKinds =
	{
		DatasetKind.TitleBasics,
		//DatasetKind.TitleAkas,
	};

	// **********
	// * Public *
	// **********
	public static async Task Init(
		FileApi fileApi,
		TimeSpan refreshPeriod,
		Func<TitleBasicsRec, bool> titleFilter,
		bool refreshTitleFilter
	)
	{
		var wasDatasetRefreshed = await RefreshDatasetsIFN(fileApi, refreshPeriod, titleFilter);
		var recompileTitles = refreshTitleFilter | wasDatasetRefreshed || !File.Exists(fileApi.GetTitlesFile());
		if (recompileTitles)
			RecompileTitles(fileApi, titleFilter);
	}

	public static TitleNfo[] LoadTitles(FileApi fileApi) => fileApi.GetTitlesFile().LoadJson<TitleNfo[]>();

	public static void AnalyzeDatasets(
		FileApi fileApi
	)
	{
		foreach (var kind in activeKinds)
		{
			var cols = kind switch
			{
				DatasetKind.TitleBasics => TitleBasicsRec.Cols,
				DatasetKind.TitleAkas => TitleAkasRec.Cols,
				_ => throw new ArgumentException()
			};
			AnalyzeDataset(fileApi, kind, cols);
		}
	}



	// ***********
	// * Loaders *
	// ***********
	private static TitleBasicsRec[] LoadTitleBasicsProd(FileApi fileApi) =>
		TsvLoader.Load(
			fileApi.GetDataSetFileForceProd(DatasetKind.TitleBasics, true),
			TitleBasicsRec.Cols,
			TitleBasicsRec.MakeFromVals
		);

	private static TitleBasicsRec[] LoadTitleBasics(FileApi fileApi) =>
		TsvLoader.Load(
			fileApi.GetDataSetFile(DatasetKind.TitleBasics, true),
			TitleBasicsRec.Cols,
			TitleBasicsRec.MakeFromVals
		);

	private static TitleAkasRec[] LoadTitleAkas(FileApi fileApi, HashSet<int> ids) =>
		TsvLoader.Load(
			fileApi.GetDataSetFile(DatasetKind.TitleAkas, true),
			TitleAkasRec.Cols,
			TitleAkasRec.MakeFromVals,
			e => ids.Contains(e.Id)
		);
	


	// ********************
	// * Refresh Datasets *
	// ********************
	private static async Task<bool> RefreshDatasetsIFN(
		FileApi fileApi,
		TimeSpan refreshPeriod,
		Func<TitleBasicsRec, bool> titleFilter
	)
	{
		var anyRefresh = false;
		foreach (var kind in activeKinds)
			anyRefresh |= await RefreshProdDatasetIFN(fileApi, kind, refreshPeriod);

		if (fileApi.IsDev)
			await MakeDevDatasets(fileApi, titleFilter);

		return anyRefresh;
	}

	private static async Task<bool> RefreshProdDatasetIFN(
		FileApi fileApi,
		DatasetKind kind,
		TimeSpan refreshPeriod
	)
	{
		var file = fileApi.GetDataSetFileForceProd(kind, false);
		var fileDecomp = fileApi.GetDataSetFileForceProd(kind, true);
		var url = ImdbConsts.GetDatasetUrl(kind);
		if (!DoesDatasetNeedDownloading(refreshPeriod, fileDecomp)) return false;

		L($"Downloading {kind} dataset", EstDepOnKind(kind, 20, 36));
		var client = new RestClient(url);
		var bytes = await client.DownloadDataAsync(new RestRequest(string.Empty));
		await File.WriteAllBytesAsync(file, bytes!);
		LDone();

		L($"Decompressing {kind} dataset", EstDepOnKind(kind, 6, 35));
		await Decompress(file, fileDecomp);
		LDone();

		File.Delete(file);
		return true;
	}

	private static bool DoesDatasetNeedDownloading(
		TimeSpan refreshPeriod,
		string file
	)
	{
		if (!File.Exists(file)) return true;
		var fileTime = new FileInfo(file).LastWriteTime;
		var fileAge = DateTime.Now - fileTime;
		return fileAge >= refreshPeriod;
	}

	private static async Task Decompress(string fileSrc, string fileDst)
	{
		await using var fsSrc = File.OpenRead(fileSrc);
		await using var fsSrcDecomp = new GZipStream(fsSrc, CompressionMode.Decompress);
		await using var fsDst = File.Create(fileDst);
		await fsSrcDecomp.CopyToAsync(fsDst);
	}



	// *********************
	// * Make Dev Datasets *
	// *********************
	private static async Task MakeDevDatasets(FileApi fileApi, Func<TitleBasicsRec, bool> titleFilter)
	{
		var titleIds = new Lazy<HashSet<int>>(() =>
			LoadTitleBasicsProd(fileApi)
				.Where(titleFilter)
				.OrderBy(e => e.StartYear)
				.Select(e => e.Id)
				.Take(ImdbConsts.DevDatasetTitleCount)
				.ToHashSet()
		);
		foreach (var kind in activeKinds)
			await MakeDevDataset(fileApi, kind, titleIds);
	}

	private static async Task MakeDevDataset(FileApi fileApi, DatasetKind kind, Lazy<HashSet<int>> titleIdsLazy)
	{
		var fileSrc = fileApi.GetDataSetFileForceProd(kind, true);
		var fileDst = fileApi.GetDataSetFile(kind, true);
		if (File.Exists(fileDst)) return;

		L("Getting TitleIds for dev datasets", 122);
		var titleIds = titleIdsLazy.Value;
		LDone();

		L($"Making {kind} dev dataset", EstDepOnKind(kind, 86, 243));
		using var fsSrc = File.OpenText(fileSrc);
		await using var fsDst = File.CreateText(fileDst);
		await fsDst.WriteLineAsync(await fsSrc.ReadLineAsync());

		while (!fsSrc.EndOfStream)
		{
			var line = await fsSrc.ReadLineAsync();
			var id = int.Parse(line!.Split('\t')[0][2..]);
			if (titleIds.Contains(id))
				await fsDst.WriteLineAsync(line);
		}
		LDone();
	}



	// ******************
	// * Refresh Titles *
	// ******************
	private static void RecompileTitles(FileApi fileApi, Func<TitleBasicsRec, bool> titleFilter)
	{
		L("Recompile titles", 86);
		var titles = LoadTitleBasics(fileApi)
			.Where(titleFilter)
			.SelectToArray(TitleNfo.MakeFromTitleBasics);

		//var ids = titleBasics.ToHashSet(e => e.Id);
		//var titleAkas = LoadTitleAkas(fileApi, dev, ids);

		fileApi.GetTitlesFile().SaveJson(titles);
		LDone();
	}



	// ********************
	// * Analyze Datasets *
	// ********************
	private static void AnalyzeDataset(
		FileApi fileApi,
		DatasetKind kind,
		ICol[] cols
	)
	{
		var file = fileApi.GetDataSetFile(kind, true);
		L($"Loading {kind} dataset to analyze", EstDepOnKind(kind, 99, 0));
		var data = TsvLoader.Load(file, cols, e => e);
		LDone();
		ColUtils.AnalyzeData(data, cols, $"Analyzing {kind} dataset");
	}



	// ***********
	// * Logging *
	// ***********
	private static Stopwatch? watch;
	private static void L(string op, int estSecs)
	{
		if (watch != null) throw new ArgumentException();
		watch = Stopwatch.StartNew();
		var estTime = TimeSpan.FromSeconds(estSecs);
		var prefix = $@"[{DateTime.Now:HH:mm:ss} - est:{estTime:mm\:ss}]";
		Console.Write($"{prefix} {op.TruncPad(40)} ... ");
	}

	private static void LDone()
	{
		if (watch == null) throw new ArgumentException();
		var time = $@"{watch.Elapsed:mm\:ss}";
		watch = null;
		Console.WriteLine($"done ({time})");
	}

	private static int EstDepOnKind(DatasetKind kind, params int[] ests) => ests[(int)kind];
}