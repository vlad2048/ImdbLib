namespace ImdbLib.Logic.Scraping.StructsScrape;

record ScrapeReviews(
	int TotalReviewCount,
	ScrapeReview[] Reviews
);