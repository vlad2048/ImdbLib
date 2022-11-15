namespace ImdbLib.Logic.Scraping.Utils;

static class ImdbUrlUtils
{
	public static string MakeTitleUrl(int id) => $"https://www.imdb.com/title/{id.FmtId()}";
	public static string MakeReviewsUrl(int id) => $"https://www.imdb.com/title/{id.FmtId()}/reviews";
	public static string MakeMoreReviewsUrl(int id, string dataKey) => $"https://www.imdb.com/title/{id.FmtId()}/reviews/_ajax?paginationKey={dataKey}";
	public static string FmtId(this int id) => $"tt{id:D7}";
}