using System.Collections.Concurrent;
using ImdbLib.Utils.Concurrency;
using ImdbLib.Utils.Exts;
using Shouldly;

namespace ImdbLib.Tests.Utils.Concurrency;

static class Ext
{
	public static Task<TResult[]> RunAll<TResult>(this IEnumerable<Task<TResult>> tasks) => Task.WhenAll(tasks);
}

class LooperTests
{
	private const double TimeTolerance = 0.2;
	private static readonly TimeSpan T = Ms(500);

	[Test]
	public async Task _01_Single()
	{
		var res = await Run(1, T, 1);
		Check(res, T);
	}

	[Test]
	public async Task _02_Multiple()
	{
		var res = await Run(8, T, 1);
		Check(res, T * 8);
	}

	[Test]
	public async Task _03_Concurrent()
	{
		var res = await Run(8, T, 2);
		Check(res, T * 4);
	}

	[Test]
	public async Task _04_Concurrent_2()
	{
		var res = await Run(8, T, 4);
		Check(res, T * 2);
	}


	record Res(
		int Count,
		TimeSpan Delay,
		int Concurrency,
		TimeSpan TimeTaken,
		List<int> ItemsDone
	);

	
	private async Task<Res> Run(int count, TimeSpan delay, int concurrency)
	{
		var list = Enumerable.Range(0, count).ToList();

		var itemsDone = new ConcurrentQueue<int>();
		var (looper, loop) = Looper.Make(list, concurrency, CancellationToken.None);
		var timeStart = DateTime.Now;

		await foreach (var elt in loop)
#pragma warning disable CS4014
			Task.Delay(delay)
				.Then(looper, () => itemsDone.Enqueue(elt));
#pragma warning restore CS4014

		return new Res(
			count,
			delay,
			concurrency,
			DateTime.Now - timeStart,
			itemsDone.ToList()
		);
	}


	private void Check(Res res, TimeSpan expectedTimeTaken)
	{
		var expectedItemsDone = Enumerable.Range(0, res.Count).ToList();
		string ToStr(List<int> list) => list.Select(e => $"{e}").JoinText();

		Console.WriteLine("Results");
		Console.WriteLine("=======");
		Console.WriteLine($"Count      :  {res.Count}");
		Console.WriteLine($"Delay      :  {res.Delay}");
		Console.WriteLine($"Concurrency:  {res.Concurrency}");
		Console.WriteLine();
		Console.WriteLine($"Actual   items:  {ToStr(res.ItemsDone)}");
		Console.WriteLine($"Expected items:  {ToStr(expectedItemsDone)}");
		Console.WriteLine();
		Console.WriteLine($"Actual   time taken:  {res.TimeTaken}");
		Console.WriteLine($"Expected time taken:  {expectedTimeTaken}");

		CollectionAssert.AreEquivalent(expectedItemsDone, res.ItemsDone);
		var minTime = TimeSpan.FromSeconds(expectedTimeTaken.TotalSeconds * (1.0 - TimeTolerance));
		var maxTime = TimeSpan.FromSeconds(expectedTimeTaken.TotalSeconds * (1.0 + TimeTolerance));
		res.TimeTaken.ShouldBeInRange(minTime, maxTime, "Wrong timing");
	}

	private static TimeSpan Ms(double v) => TimeSpan.FromMilliseconds(v);
}
