namespace ImdbLib.Logic.Scraping.StructsScrape;

record ScrapeTitle(
	byte Rating,
	string ImgUrl,
	string Plot,
	string Director,
	string[] Stars,
	DateTime ReleaseDate,
	string[] Countries,
	string[] Languages
);