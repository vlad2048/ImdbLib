using ImdbLib.Logic.Datasets.TsvLoading.Structs;
using ImdbLib.Utils.Exts;

namespace ImdbLib.Logic.Datasets.TsvLoading;

static class TsvLoader
{
	public static T[] Load<T>(
		string file,
		ICol[] cols,
		Func<object?[], T> makeFun,
		Func<T, bool>? predicate = null
	)
	{
		var list = new List<T>();
	
		using var fs = File.OpenText(file);
		fs.ReadLine();
		while (!fs.EndOfStream)
		{
			var line = fs.ReadLine();
			var parts = line!.Split(new[] { '\t' }, StringSplitOptions.TrimEntries);
			if (parts.Length != cols.Length) throw new ArgumentException();
			var vals = cols.SelectToArray(e => e.Parse(parts[e.Index]));
			var obj = makeFun(vals);

			var isValid = predicate switch
			{
				null => true,
				not null => predicate(obj)
			};

			if (isValid)
				list.Add(obj);
		}

		return list.ToArray();
	}
}