using ImdbLib.Logic.Scraping.Structs.Enums;

namespace ImdbLib.Logic.Scraping.Structs;

record TitleState(
	MovieStatus Status,
	string? Err,
	DateTime LastUpdate
);