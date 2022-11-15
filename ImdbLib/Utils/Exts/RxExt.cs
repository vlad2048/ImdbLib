using System.Reactive.Linq;
using PowRxVar;

namespace ImdbLib.Utils.Exts;

static class RxExt
{
	public static (CancellationToken, IDisposable) MakeCancelWhen<T>(this IRoVar<T> rwVar, T val, IDisposable dispOnCancel)
	{
		var d = new Disp();
		var cancelSource = new CancellationTokenSource();
		rwVar.Where(e => Equals(e, val)).Subscribe(_ =>
		{
			cancelSource.Cancel();
			dispOnCancel.Dispose();
		}).D(d);
		return (cancelSource.Token, d);
	}
}
