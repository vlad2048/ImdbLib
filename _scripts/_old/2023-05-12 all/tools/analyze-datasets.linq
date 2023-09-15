<Query Kind="Program" />

/*
TitleBasics
===========
tconst	titleType	primaryTitle	originalTitle	isAdult	startYear	endYear	runtimeMinutes	genres
tt0000001	short	Carmencita	Carmencita	0	1894	\N	1	Documentary,Short
tt0110912	movie	Pulp Fiction	Pulp Fiction	0	1994	\N	154	Crime,Drama

TitleAkas
=========
titleId	ordering	title	region	language	types	attributes	isOriginalTitle
tt0000001	1	Карменсіта	UA	\N	imdbDisplay	\N	0
tt0110912	10	Pulp Fiction	US	\N	imdbDisplay	\N	0
*/

void Main()
{
	CheckBasics();
}

// rows: 9322337
record TitleBasicsRec(
	int Id,						// 1 - 22985164
	string Type,				// [11] short, movie, tvMiniSeries, tvShort, tvMovie, tvSeries, tvEpisode, tvSpecial, video, videoGame, tvPilot
	string PrimaryTitle,		// all populated (half are unique)
	string OriginalTitle,		// all populated (half are unique)
	bool IsAdult,				// false: 9032806	true: 289531	(3.10% adult)
	int? StartYear,				// populated: 8074429	(86.61%)		min:1874	max:2029
	int? EndYear,				// populated:   97720	( 1.04%)		min:1906	max:2028
	int? RuntimeMinute,			// populated: 2534183	(27.18%)		min:0		max:51420
	string Genres				// [28] 'Documentary', 'Short', 'Animation', 'Comedy', 'Romance', 'Sport', 'News', 'Drama', 'Fantasy', 'Horror', 'Biography', 'Music', 'War', 'Crime', 'Western', 'Family', 'Adventure', 'Action', 'History', 'Mystery', 'Sci-Fi', 'Musical', 'Thriller', 'Film-Noir', 'Talk-Show', 'Game-Show', 'Reality-TV', 'Adult'
);

void CheckBasics()
{
	var fileBasics = @"C:\caches\imdb\datasets\title.basics.tsv";
	
	var data = LoadFile<(
		string,
		string,
		string,
		string,
		bool,
		int?,
		int?,
		int?,
		string
	)>(fileBasics);
	
	Check(data, 0, "TitleId", CheckType.TitleId);
	Check(data, 1, "TitleType", CheckType.Category);
	Check(data, 2, "PrimaryTitle", CheckType.String);
	Check(data, 3, "OriginalTitle", CheckType.String);
	Check(data, 4, "IsAdult", CheckType.Bool);
	Check(data, 5, "StartYear", CheckType.NumberOpt);
	Check(data, 6, "EndYear", CheckType.NumberOpt);
	Check(data, 7, "RuntimeMinutes", CheckType.NumberOpt);
	Check(data, 8, "Genres", CheckType.StringArray);
}

void CheckAkas()
{
	var fileAkas = @"C:\caches\imdb\datasets-dev\title.akas.tsv";
}

enum CheckType
{
	TitleId,
	Category,
	String,
	Bool,
	StringNumber,
	StringArray,
	NumberOpt
}

void Check(object[][] data, int colIdx, string colName, CheckType type)
{
	var str = $"[{colIdx}] - '{colName}'".Dump();
	new string('=', str.Length).Dump();
	var vals = data.Select(e => e[colIdx]).ToArray();

	switch (type)
	{
		case CheckType.TitleId:
			{
				var ids = vals.Select(e => GetTitleId((string)e));
				$"  minId: {ids.Min()}".Dump();
				$"  maxId: {ids.Max()}".Dump();
				break;
			}
		case CheckType.Category:
			{
				var cats = vals.Select(e => (string)e).Distinct().ToArray();
				if (cats.Any(e => e == null)) throw new ArgumentException();
				$"  cats: {cats.Length}".Dump();
				("  vals: " + string.Join(", ", cats.Take(50))).Dump();
				$"  empty: {cats.Count(e => e == string.Empty)}".Dump();
				break;
			}
		case CheckType.String:
			{
				var strs = vals.Select(e => (string)e).ToArray();
				if (strs.Any(e => e == null)) throw new ArgumentException();
				$"  empty: {strs.Count(e => e == string.Empty)}".Dump();
				$"  distinct: {strs.Distinct().Count()}".Dump();
				break;
			}
		case CheckType.Bool:
			{
				var cntTrue = vals.Count(e => (bool)e);
				var cntFalse = vals.Count(e => !(bool)e);
				$"  true : {cntTrue}".Dump();
				$"  false: {cntFalse}".Dump();
				break;
			}
		case CheckType.StringNumber:
			{
				var okNums = vals.Count(e => int.TryParse((string)e, out _));
				$"  nums: {okNums}/{vals.Length}".Dump();
				var nums = vals.Where(e => int.TryParse((string)e, out _)).Select(e => int.Parse((string)e)).ToArray();
				var wrongStrings = vals.Where(e => !int.TryParse((string)e, out _)).Select(e => (string)e).Distinct().ToArray();
				if (nums.Length == 0)
				{
					"  min: _".Dump();
					"  max: _".Dump();
				}
				else
				{
					$"  min: {nums.Min()}".Dump();
					$"  max: {nums.Max()}".Dump();
				}
				if (wrongStrings.Length > 0)
				{
					$"  wrong: {wrongStrings.Length}".Dump();
					$"  wrongVals: {wrongStrings.JoinText()}".Dump();
				}
				break;
			}
		case CheckType.StringArray:
			{
				var cats = vals.SelectMany(e => ((string)e).Split(',')).Where(e => e != string.Empty).Distinct().ToArray();
				$"  cats: {cats.Length}".Dump();
				$"  catsVals: {cats.JoinText()}".Dump();
				break;
			}
		case CheckType.NumberOpt:
			{
				var nums = vals.Select(e => ((int?)e)).ToArray();
				var okNums = nums.Where(e => e != null).Select(e => e!.Value).ToArray();
				$"  nulls: {nums.Count(e => e == null)}".Dump();
				if (okNums.Length == 0)
				{
					"  min: _".Dump();
					"  max: _".Dump();
				}
				else
				{
					$"  min: {okNums.Min()}".Dump();
					$"  max: {okNums.Max()}".Dump();
				}
				break;
			}
		default:
			throw new ArgumentException();
	}

	" ".Dump();
}

public static class StrExt
{
	public static string JoinText<T>(this IEnumerable<T> source) => string.Join(", ", source.Take(50).Select(e => $"'{e}'"));
}

int GetTitleId(string str) => int.Parse(str[2..]);




private static Type[] GetArgTypes(Type tupleType)
{
	if (!tupleType.Name.StartsWith("ValueTuple`")) return new Type[] { tupleType };
	return tupleType.GenericTypeArguments;
}

private static object[][] LoadFile<T>(string file)
{
	var argTypes = (
		from topType in GetArgTypes(typeof(T))
		from subType in GetArgTypes(topType)
		select subType
	).ToArray();
	
	var list = new List<object[]>();

	
	using var fs = File.OpenText(file);
	fs.ReadLine();
	while (!fs.EndOfStream)
	{
		var line = fs.ReadLine();
		var parts = line!.Split(new[] { '\t' }, StringSplitOptions.TrimEntries);
		if (parts.Length != argTypes.Length) throw new ArgumentException();
		var vals = parts
			.Select((part, idx) => ReadVal(part, argTypes[idx]))
			.ToArray();
		list.Add(vals);

	}

	return list.ToArray();
}

private static object? ReadVal(string str, Type type)
{
	var isNull = str == @"\N";
	if (type == typeof(string)) return isNull switch
	{
		false => str,
		true => string.Empty
	};
	if (type == typeof(int?)) return isNull switch
	{
		false => int.Parse(str),
		true => (int?)null
	};
	if (type == typeof(int)) return int.Parse(str);
	if (type == typeof(bool)) return int.Parse(str) != 0;
	if (type == typeof(string[])) return str.Split(',').Where(e => e != @"\N").ToArray();
	throw new ArgumentException($"Type {type.Name} not supported");
}