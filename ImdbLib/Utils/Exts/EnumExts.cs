using System.Collections.Concurrent;

namespace ImdbLib.Utils.Exts;

static class EnumExts
{
	public static IAsyncEnumerator<T> GetAsyncEnumerator<T>(this IAsyncEnumerator<T> enumerator) => enumerator;

	public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
	{
		foreach (var elt in source)
			action(elt);
	}

	public static U[] SelectToArray<T, U>(this IEnumerable<T> source, Func<T, U> fun) => source.Select(fun).ToArray();

	public static T[] WhereToArray<T>(this IEnumerable<T> source, Func<T, bool> predicate) => source.Where(predicate).ToArray();

	public static HashSet<U> ToHashSet<T, U>(this IEnumerable<T> source, Func<T, U> selFun) => source.Select(selFun).ToHashSet();

	public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> source)
	{
		foreach (var elt in source)
			bag.Add(elt);
	}

	public static Queue<T> ToQueue<T>(this IEnumerable<T> source)
	{
		var queue = new Queue<T>();
		foreach (var elt in source)
			queue.Enqueue(elt);
		return queue;
	}
}