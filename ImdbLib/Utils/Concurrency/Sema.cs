namespace ImdbLib.Utils.Concurrency;

class SemaOptions
{
	public int Concurrency { get; set; } = 4;
	public TimeSpan FinishCheckTimeout { get; set; } = TimeSpan.FromSeconds(2);
	public TimeSpan FinishCheckPeriod { get; set; } = TimeSpan.FromMilliseconds(100);
	public CancellationToken MainCancelToken { get; set; } = CancellationToken.None;
	public static SemaOptions Build(Action<SemaOptions> fun)
	{
		var opt = new SemaOptions();
		fun(opt);
		return opt;
	}
}


class Sema
{
	private readonly SemaOptions opt;
	private readonly SemaphoreSlim slim;

	public int MaxConcurrency { get; }
	public int Concurrency { get; private set; }

	public async Task SetConcurrency(int newVal)
	{
		if (newVal < 0 || newVal > MaxConcurrency)
			throw new ArgumentException();
		if (newVal == Concurrency)
			return;
		var srcWait = MaxConcurrency - Concurrency;
		var dstWait = MaxConcurrency - newVal;
		var deltaWait = dstWait - srcWait;
		if (deltaWait == 0) throw new ArgumentException();
		if (deltaWait > 0)
			for (var i = 0; i < deltaWait; i++)
				await Wait(opt.MainCancelToken);
		else
			for (var i = 0; i < -deltaWait; i++)
				Release();
		Concurrency = newVal;
	}

	public Sema(Action<SemaOptions> optFun)
	{
		opt = SemaOptions.Build(optFun);
		slim = new SemaphoreSlim(opt.Concurrency);
		MaxConcurrency = Concurrency = opt.Concurrency;
	}

	public override string ToString()
	{
		var freeSpots = slim.CurrentCount;
		return $"Sema running: {opt.Concurrency - freeSpots}/{opt.Concurrency}";
	}

	public async Task Wait(CancellationToken cancelToken) => await slim.WaitAsync(cancelToken);

	public void Release() => slim.Release();

	/// <summary>
	/// Waits for all the tasks to finish
	/// </summary>
	/// <returns>Returns true if the waiting succeeds. Returns false if we timeout</returns>
	public async Task<bool> WaitForAllToFinish()
	{
		await SetConcurrency(MaxConcurrency);

		bool DoesNeedWaiting() => slim.CurrentCount < opt.Concurrency;

		var timeStart = DateTime.Now;
		while (DoesNeedWaiting()) {
			await Task.Delay(opt.FinishCheckPeriod);
			var delta = DateTime.Now - timeStart;
			if (delta >= opt.FinishCheckTimeout && !DoesNeedWaiting())
				return false;
		}
		return true;
	}
}
