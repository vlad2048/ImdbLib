using ImdbLib.Utils.Exts;

namespace ImdbLib.Utils;

enum DatasetKind
{
	TitleBasics,
	TitleAkas
}

class FileApi
{
	private readonly string datasetsProdFolder;
	private readonly string datasetsFolder;
	private readonly string scrapingFolder;
	private readonly bool dev;

	public bool IsDev => dev;

	public string GetDataSetFileForceProd(DatasetKind kind, bool decomp) => GetDataSetFile(kind, decomp, datasetsProdFolder);
	public string GetDataSetFile(DatasetKind kind, bool decomp) => GetDataSetFile(kind, decomp, datasetsFolder);
	private string GetDataSetFile(DatasetKind kind, bool decomp, string folder)
	{
		var url = ImdbConsts.GetDatasetUrl(kind);
		var file = Path.Combine(folder, Path.GetFileName(url));
		return file.RemoveExtensionIf(decomp);
	}

	public string GetTitlesFile() => Path.Combine(datasetsFolder, "titles.json");

	public string GetTitleStatesFile() => Path.Combine(scrapingFolder, "title-states.json");

	public string[] GetScrapeFiles() => Directory.GetFiles(scrapingFolder, "*.json").WhereToArray(e => Path.GetFileName(e) != "title-states.json");
	public string GetScrapeFile(int year) => Path.Combine(scrapingFolder, $"{year}.json");

	public FileApi(string dataFolder, bool dev)
	{
		this.dev = dev;
		datasetsProdFolder = Path.Combine(dataFolder, "datasets").CreateFolderIFN();
		datasetsFolder = Path.Combine(dataFolder, "datasets".AddSuffixIf(dev, "-dev")).CreateFolderIFN();
		scrapingFolder = Path.Combine(dataFolder, "scraping".AddSuffixIf(dev, "-dev")).CreateFolderIFN();
	}
}