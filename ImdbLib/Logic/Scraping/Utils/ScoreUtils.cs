using PowMaybeErr;

namespace ImdbLib.Logic.Scraping.Utils;

static class ScoreUtils
{
	public static MaybeErr<byte> ConvertScore(this decimal dec) => dec switch
	{
		>= 0 and <= 10 => MayErr.Some((byte)(dec * 10)),
		_ => MayErr.None<byte>($"Score not in the right range: {dec}")
	};
}