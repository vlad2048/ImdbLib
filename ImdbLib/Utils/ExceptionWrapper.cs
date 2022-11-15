using PowMaybeErr;

namespace ImdbLib.Utils;

static class ExceptionWrapper
{
	public static async Task<MaybeErr<T>> WrapExT<T>(Func<Task<T>> fun)
	{
		try
		{
			return MayErr.Some(await fun());
		}
		catch (Exception ex)
		{
			return MayErr.None<T>($"{ex}");
		}
	}

	public static MaybeErr<T> WrapEx<T>(Func<T> fun)
	{
		try
		{
			return MayErr.Some(fun());
		}
		catch (Exception ex)
		{
			return MayErr.None<T>($"{ex}");
		}
	}
}