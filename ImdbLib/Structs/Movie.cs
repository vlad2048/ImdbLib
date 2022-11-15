using ImdbLib.Logic.Datasets.Structs;
using ImdbLib.Logic.Scraping.StructsScrape;
using ImdbLib.Structs.Enums;
using ImdbLib.Utils.Exts;

namespace ImdbLib.Structs;

public record Movie(
	// TitleNfo
	int Id,
	string Name,
	string? OriginalName,
	int Year,
	int? Runtime,
	Genre Genres,

	// ScrapeTitle
	byte Rating,
	string ImgUrl,
	string Plot,
	string Director,
	string[] Stars,
	DateTime ReleaseDate,
	string[] Countries,
	string[] Languages,

	// ScrapeReviews
	int TotalReviewCount,
	MovieReview[] Reviews
)
{
	internal static Movie Make(TitleNfo nfo, ScrapeTitle title, ScrapeReviews reviews) => new(
		nfo.Id,
		nfo.Name,
		nfo.OriginalName,
		nfo.Year,
		nfo.Runtime,
		nfo.Genres,

		title.Rating,
		title.ImgUrl,
		title.Plot,
		title.Director,
		title.Stars,
		title.ReleaseDate,
		title.Countries,
		title.Languages,

		reviews.TotalReviewCount,
		reviews.Reviews.SelectToArray(e => e.ToMovieReview())
	);
}

public record MovieReview(
	byte Score,
	string Title
);
