using PowMaybeErr;

namespace ImdbLib.Logic.Scraping.Structs.Enums;

enum MovieStatus
{
	OK = 0,

	// these two come with an error message
	DownloadError = 1,
	UnknownError = 2,

	RateLimitDefense = 10,
	NotReleased,
	InDevelopment,
	RatingNotFound,
	ZeroReviews,
	ReleaseDateNotFound,
	DirectorNotFound,
	ImageNotFound,
	PageNotFoundOnServer,
}

record StatusErr(MovieStatus Status, string? Err);

static class MovieStatusUtils
{
	public static (MovieStatus, string?) ExtractStatus<T>(MaybeErr<T> may) => may.IsSome(out _, out var err) switch
	{
		true => (MovieStatus.OK, null),
		false => GetError(err!)
	};

	private static (MovieStatus, string?) GetError(string err)
	{
		var statuses = Enum.GetValues<MovieStatus>().Skip(2);
		foreach (var status in statuses)
			if (err == $"{status}")
				return (status, err);
		return (MovieStatus.UnknownError, err);
	}
}