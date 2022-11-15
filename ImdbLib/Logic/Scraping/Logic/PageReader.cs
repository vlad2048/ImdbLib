using ImdbLib.Logic.Scraping.Logic.Structs;
using ImdbLib.Utils;
using PowMaybe;
using PowMaybeErr;

namespace ImdbLib.Logic.Scraping.Logic;

static class PageReader
{
	public static async Task<MaybeErr<Pages>?> ReadPages(int id, IPager pager, CancellationToken cancelToken)
	{
		var getTitleTask = pager.GetTitle(id, cancelToken);
		var getReviewsTask = pager.GetReviews(id, cancelToken);
		await Task.WhenAll(getTitleTask, getReviewsTask);
		if (cancelToken.IsCancellationRequested) return null;
		var mayTitleStr = getTitleTask.Result;
		var mayReviewsStr = getReviewsTask.Result;
		if (mayTitleStr.IsNone(out var titleStr, out var titleStrErr)) return MayErr.None<Pages>(titleStrErr);
		if (mayReviewsStr.IsNone(out var reviewsStr, out var reviewsStrErr)) return MayErr.None<Pages>(reviewsStrErr);

		static MaybeErr<string> ReadDataKey(string s) =>
			from root in Html.LoadFromString(s)
			from _dataKey in root.GetAttr("data-key", "//div[@class='load-more-data']")
			select _dataKey;

		var mayDataKey = ReadDataKey(reviewsStr);
		var mayMoreReviewsStr = May.None<string>();
		if (mayDataKey.IsSome(out var dataKey))
		{
			var mayMore = await pager.GetMoreReviews(id, dataKey, cancelToken);
			if (cancelToken.IsCancellationRequested) return null;
			if (mayMore.IsSome(out var more))
				mayMoreReviewsStr = May.Some(more);
		}

		return MayErr.Some(new Pages(
			titleStr,
			reviewsStr,
			mayMoreReviewsStr
		));
	}
}