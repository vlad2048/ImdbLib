using System.Reactive;
using System.Reactive.Linq;
using PowRxVar;

namespace ImdbLib.Logic.Scraping.Utils;

static class ScraperRxUtils
{
	public static IDisposable HookInterruptableOperation(
		IObservable<Unit> whenStart,
		IObservable<Unit> whenStop,
		Func<CancellationToken, Action, Task> task
	)
	{
		var d = new Disp();

		var isRunning = false;

		whenStart.Where(_ => !isRunning)
			.Subscribe(_ =>
			{
				isRunning = true;
				Task.Run(async () =>
				{
					try
					{
						var cancelSource = new CancellationTokenSource();
						var cancelToken = cancelSource.Token;
						whenStop.Take(1).Subscribe(_ => cancelSource.Cancel());

						await task(cancelToken, () => cancelSource.Cancel());
					}
					catch (OperationCanceledException)
					{
						Console.WriteLine("Cancel (Exception)");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Unexpected Exception: {ex}");
					}
					finally
					{
						isRunning = false;
					}
				});
			}).D(d);

		return d;
	}
}