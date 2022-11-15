using PowMaybe;

namespace ImdbLib.Logic.Scraping.Logic.Structs;

record Pages(
	string TitleStr,
	string ReviewsStr,
	Maybe<string> MoreReviewsStr
);
