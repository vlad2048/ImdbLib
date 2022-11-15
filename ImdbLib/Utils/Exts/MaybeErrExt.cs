using PowMaybe;
using PowMaybeErr;

namespace ImdbLib.Utils.Exts;

static class MaybeErrExt
{
	public static MaybeErr<bool> NegateMaybe(this MaybeErr<bool> may, string errMsg) => may.IsSome() switch
	{
		true => MayErr.None<bool>(errMsg),
		false => MayErr.Some(true)
	};
	
	public static MaybeErr<T> WithError<T, E>(this MaybeErr<T> may, E errMsg) => may.IsSome() switch
	{
		true => may,
		false => MayErr.None<T>($"{errMsg}")
	};

	public static MaybeErr<T> MapError<T>(this MaybeErr<T> may, Func<string, string> errFun) => may.IsSome(out _, out var err) switch
	{
		true => may,
		false => MayErr.None<T>(errFun(err!))
	};

	public static MaybeErr<T> FailWith<T>(this MaybeErr<T> may, T def) => may.IsSome() switch
	{
		true => may,
		false => MayErr.Some(def)
	};

	public static T FailWithValue<T>(this MaybeErr<T> may, T def) => may.IsSome(out var val) switch
	{
		true => val!,
		false => def
	};

	public static Maybe<T> AggregateMay<T>(this IEnumerable<Func<Maybe<T>>> source) =>
		source
			.Select(fun => fun())
			.WhereSome()
			.FirstOrMaybe();

	public static MaybeErr<T> ToMaybeErrWithMsg<T>(this Maybe<T> may, string errMsg) => may.IsSome(out var val) switch
	{
		true => MayErr.Some(val!),
		false => MayErr.None<T>(errMsg)
	};
}