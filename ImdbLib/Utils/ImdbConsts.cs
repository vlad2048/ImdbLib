namespace ImdbLib.Utils;

static class ImdbConsts
{
	public static string GetDatasetUrl(DatasetKind kind) => kind switch
	{
		DatasetKind.TitleBasics => "https://datasets.imdbws.com/title.basics.tsv.gz",
		DatasetKind.TitleAkas => "https://datasets.imdbws.com/title.akas.tsv.gz",
		_ => throw new ArgumentException()
	};

	public const int DevDatasetTitleCount = 1000;
}