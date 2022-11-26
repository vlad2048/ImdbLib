using ImdbLib.Utils.Exts;

namespace ImdbLib.Utils.Concurrency;

interface IParallelLooper<out T> : IAsyncEnumerable<T>
{
	void Add(Task task);
	void Release();
}

static class Looper
{
	public static async Task Loop<T>(
		IEnumerable<T> source,
		int parallelism,
		CancellationToken cancelToken,
		Func<T, Task> action
	)
	{
		var (looper, loop) = Make(source, parallelism, cancelToken);
		await foreach (var title in loop)
		{
			looper.Add(Task.Run(async () =>
			{
				await action(title);
				looper.Release();
			}, cancelToken));
		}
	}


	public static (IParallelLooper<T>, IAsyncEnumerator<T>) Make<T>(
		IEnumerable<T> source,
		int concurrency,
		CancellationToken cancelToken
	)
	{
		var looper = new ParallelLooper<T>(source, concurrency);
		return (looper, looper.GetAsyncEnumerator(cancelToken));
	}


	public static Task Then<T>(
		this Task task,
		IParallelLooper<T> looper,
		Action action
	)
	{
		async Task NewTask()
		{
			await task;
			action();
			looper.Release();
		}
		var newTask = NewTask();
		looper.Add(newTask);
		return newTask;
	}


	public static Task Then<T, U>(
		this Task<U> task,
		IParallelLooper<T> looper,
		Action<U> action
	)
	{
		async Task NewTask()
		{
			var res = await task;
			action(res);
			looper.Release();
		}
		var newTask = NewTask();
		looper.Add(newTask);
		return newTask;
	}




	private class ParallelLooper<T> : IParallelLooper<T>
	{
		private readonly Sema sema;
		private readonly IEnumerator<T> sourceEnum;
		private readonly List<Task> tasks = new();

		internal ParallelLooper(IEnumerable<T> source, int concurrency)
		{
			sema = new Sema(opt => {
				opt.Concurrency = concurrency;
			});
			sourceEnum = source.GetEnumerator();
		}

		public void Add(Task task) => tasks.Add(task);
		public void Release() => sema.Release();

		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancelToken = new())
		{
			while (sourceEnum.MoveNext()) {
				await sema.Wait(cancelToken);
				yield return sourceEnum.Current;
			}
			await Task.WhenAll(tasks);
		}
	}

}