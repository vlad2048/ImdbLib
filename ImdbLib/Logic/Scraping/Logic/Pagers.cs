using ImdbLib.Logic.Scraping.Utils;
using ImdbLib.Utils;
using PowMaybeErr;

namespace ImdbLib.Logic.Scraping.Logic;

public interface IPager
{
	Task<MaybeErr<string>> GetTitle(int id, CancellationToken cancelToken);
	Task<MaybeErr<string>> GetReviews(int id, CancellationToken cancelToken);
	Task<MaybeErr<string>> GetMoreReviews(int id, string dataKey, CancellationToken cancelToken);
}

public interface IPagerWithSave : IPager
{
	void SaveTitle(int id, string str);
	void SaveReviews(int id, string str);
	void SaveMoreReviews(int id, string str);
}

class HtmlPager : IPager
{
	public async Task<MaybeErr<string>> GetTitle(int id, CancellationToken cancelToken) => await HtmlUtils.GetPage(ImdbUrlUtils.MakeTitleUrl(id), cancelToken);
	public async Task<MaybeErr<string>> GetReviews(int id, CancellationToken cancelToken) => await HtmlUtils.GetPage(ImdbUrlUtils.MakeReviewsUrl(id), cancelToken);
	public async Task<MaybeErr<string>> GetMoreReviews(int id, string dataKey, CancellationToken cancelToken) => await HtmlUtils.GetPage(ImdbUrlUtils.MakeMoreReviewsUrl(id, dataKey), cancelToken);
}

class FilePager : IPagerWithSave
{
	private readonly string folder;

	public FilePager(string folder) => this.folder = folder;

	public Task<MaybeErr<string>> GetTitle(int id, CancellationToken cancelToken) => Task.FromResult(WrapEx(() => File.ReadAllText(MkTitleFile(id))));
	public Task<MaybeErr<string>> GetReviews(int id, CancellationToken cancelToken) => Task.FromResult(WrapEx(() => File.ReadAllText(MkReviewsFile(id))));
	public Task<MaybeErr<string>> GetMoreReviews(int id, string dataKey, CancellationToken cancelToken) => Task.FromResult(WrapEx(() => File.ReadAllText(MkMoreReviewsFile(id))));
	public void SaveTitle(int id, string str) => File.WriteAllText(MkTitleFile(id), str);
	public void SaveReviews(int id, string str) => File.WriteAllText(MkReviewsFile(id), str);
	public void SaveMoreReviews(int id, string str) => File.WriteAllText(MkMoreReviewsFile(id), str);

	private string MkTitleFile(int id) => Path.Combine(folder, $"{id.FmtId()}-title.html");
	private string MkReviewsFile(int id) => Path.Combine(folder, $"{id.FmtId()}-reviews.html");
	private string MkMoreReviewsFile(int id) => Path.Combine(folder, $"{id.FmtId()}-reviews-more.html");
}

public static class Pagers
{
	public static readonly IPager Html = new HtmlPager();
	public static IPagerWithSave File(string folder) => new FilePager(folder);
}