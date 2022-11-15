using ImdbLib.Logic.Datasets.TsvLoading.Utils;
using ImdbLib.Structs.Enums;
using ImdbLib.Utils.Exts;

namespace ImdbLib.Logic.Datasets.TsvLoading.Structs;

enum ColType
{
	TitleId,
	Bool,
	Number,
	NumberOpt,
	String,
	Category,
	CategoryArray,
	Genres,
}

interface ICol
{
	int Index { get; }
	string Name { get; }
	ColType ColType { get; }
	Type ValType { get; }
	object? Parse(string str);
	void Analyze(object?[] objs, ColLogger l);
}

class TitleIdCol : ICol
{
	public int Index { get; }
	public string Name => "TitleId";
	public ColType ColType => ColType.TitleId;
	public Type ValType => typeof(int);

	public TitleIdCol(int index) => Index = index;

	public object Parse(string str) => int.Parse(str[2..]);

	public void Analyze(object?[] objs, ColLogger l)
	{
		var vals = objs.SelectToArray(e => (int)e!);
		l.Log("min", vals.Min());
		l.Log("max", vals.Max());
		l.LogPerc("dups", vals.Length - vals.Distinct().Count());
	}
}

class BoolCol : ICol
{
	public int Index { get; }
	public string Name { get; }
	public ColType ColType => ColType.Bool;
	public Type ValType => typeof(int);

	public BoolCol(int index, string name)
	{
		Index = index;
		Name = name;
	}

	public object Parse(string str) => ColUtils.ParseBool(str);

	public void Analyze(object?[] objs, ColLogger l)
	{
		var vals = objs.SelectToArray(e => (bool)e!);
		l.Log("false", vals.Count(e => !e));
		l.Log("true", vals.Count(e => e));
	}
}

class NumberCol : ICol
{
	public int Index { get; }
	public string Name { get; }
	public ColType ColType => ColType.Number;
	public Type ValType => typeof(int);

	public NumberCol(int index, string name)
	{
		Index = index;
		Name = name;
	}

	public object Parse(string str) => int.Parse(str);

	public void Analyze(object?[] objs, ColLogger l)
	{
		var vals = objs.SelectToArray(e => (int)e!);
		l.Log("min", vals.Min());
		l.Log("max", vals.Max());
	}
}

class NumberOptCol : ICol
{
	public int Index { get; }
	public string Name { get; }
	public ColType ColType => ColType.NumberOpt;
	public Type ValType => typeof(int?);

	public NumberOptCol(int index, string name)
	{
		Index = index;
		Name = name;
	}

	public object? Parse(string str) => ColUtils.ParseNumberOpt(str);

	public void Analyze(object?[] objs, ColLogger l)
	{
		var vals = objs.SelectToArray(e => (int?)e);
		l.LogPerc("pops", vals.Count(e => e.HasValue));
		var pops = vals.Where(e => e.HasValue).SelectToArray(e => e!.Value);
		var (min, max) = pops.Length switch
		{
			0 => (0, 0),
			_ => (pops.Min(), pops.Max())
		};
		l.Log("min", min);
		l.Log("max", max);
	}
}

class StringCol : ICol
{
	public int Index { get; }
	public string Name { get; }
	public ColType ColType => ColType.String;
	public Type ValType => typeof(int);

	public StringCol(int index, string name)
	{
		Index = index;
		Name = name;
	}

	public object Parse(string str) => ColUtils.ParseString(str);

	public void Analyze(object?[] objs, ColLogger l)
	{
		var vals = objs.SelectToArray(e => (string)e!);
		var nonEmptyVals = vals.Where(e => e != string.Empty).ToArray();
		l.LogPerc("empty", vals.Count(e => e == string.Empty));
		l.LogPerc("distinct", vals.Distinct().Count());
		l.Log("min-lng", nonEmptyVals.Length == 0 ? 0 : nonEmptyVals.Min(e => e.Length));
		l.Log("max-lng", nonEmptyVals.Length == 0 ? 0 : nonEmptyVals.Max(e => e.Length));
	}
}

class CategoryCol : ICol
{
	public int Index { get; }
	public string Name { get; }
	public ColType ColType => ColType.Category;
	public Type ValType => typeof(string);

	public CategoryCol(int index, string name)
	{
		Index = index;
		Name = name;
	}

	public object Parse(string str) => ColUtils.ParseString(str);

	public void Analyze(object?[] objs, ColLogger l)
	{
		var vals = objs.SelectToArray(e => (string)e!);
		l.LogPerc("empty", vals.Count(e => e == string.Empty));
		var cats = vals.Distinct().OrderBy(e => e).ToArray();
		l.Log("catsCount", cats.Length);
		l.Log("cats", cats.Take(20).JoinText());
	}
}

class CategoryArrayCol : ICol
{
	public int Index { get; }
	public string Name { get; }
	public ColType ColType => ColType.CategoryArray;
	public Type ValType => typeof(string[]);

	public CategoryArrayCol(int index, string name)
	{
		Index = index;
		Name = name;
	}

	public object Parse(string str) => ColUtils.ParseCategoryArray(str);

	public void Analyze(object?[] objs, ColLogger l)
	{
		var vals = objs.SelectToArray(e => (string[])e!);
		l.Log("empty", vals.Count(e => e.Length == 0));
		var cats = vals.SelectMany(e => e).Distinct().OrderBy(e => e).ToArray();
		l.Log("catsCount", cats.Length);
		l.Log("cats", cats.Take(20).JoinText());
	}
}

class GenresCol : ICol
{
	public int Index { get; }
	public string Name { get; }
	public ColType ColType => ColType.Genres;
	public Type ValType => typeof(Genre);

	public GenresCol(int index, string name)
	{
		Index = index;
		Name = name;
	}

	public object Parse(string str) => GenreUtils.ParseGenres(str);

	public void Analyze(object?[] objs, ColLogger l)
	{
		var vals = objs.SelectToArray(e => (Genre)e!);
		var genres = Enum.GetValues<Genre>();
		var percs = (
			from genre in genres
			let genreCount = vals.Count(val => val.HasFlag(genre))
			select $"{genre} ({l.Perc(genreCount)})"
		).JoinText();
		l.Log("genres", percs);
	}
}