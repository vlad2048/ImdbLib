using System.Reactive;
using PowRxVar;

namespace ImdbLib.Logic.Scraping;

enum StopReason
{
	User,
	RateLimit,
	FetchTimeout
}

static class JobRunner
{
	public static IDisposable Run(
		IObservable<Unit> whenUserStart,
		IObservable<Unit> whenUserStop,
		Func<CancellationToken, Action<StopReason>, Task> job
	)
	{
		var d = new Disp();

		//async Task Run(CancellationToken cancelToken)

		return d;
	}
}