namespace ImdbLib.Logic.Scraping.Structs;

public record ScrapeState(
	bool Running,
	int CntTotal,
	int CntValid,
	int CntInvalid,
	double Flow
)
{
	public string GetConsoleMessage() => $"{Flow:F2} movies/min. val:{CntValid} inval:{CntInvalid} total:{CntTotal}. progress:{GetProgressStr()}";

	private string GetProgressStr() => CntTotal switch
	{
		0 => "_",
		_ => $"{(CntValid + CntInvalid) * 100.0 / CntTotal:F2}%"
	};
}
