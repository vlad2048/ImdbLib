using PowMaybeErr;

namespace ImdbLib.Utils;

static class Err
{
	public static MaybeErr<bool> MakeIf<T>(bool condition, T errMsg) => condition switch
	{
		true => MayErr.None<bool>($"{errMsg}"),
		false => MayErr.Some(true)
	};
}