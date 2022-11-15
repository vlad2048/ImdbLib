using ImdbLib.Logic.Datasets.TsvLoading.Structs;
using ImdbLib.Structs.Enums;

namespace ImdbLib.Logic.Datasets.Structs;

/*

TitleBasics (rows:9322337	mem:4.5GB	load:01:50)
===========
tconst	titleType	primaryTitle	originalTitle	isAdult	startYear	endYear	runtimeMinutes	genres
tt0000001	short	Carmencita	Carmencita	0	1894	\N	1	Documentary,Short
tt0110912	movie	Pulp Fiction	Pulp Fiction	0	1994	\N	154	Crime,Drama

*/

public record TitleBasicsRec(
	int Id,
	string Type,
	string PrimaryTitle,
	string OriginalTitle,
	bool IsAdult,
	int? StartYear,
	int? EndYear,
	int? RuntimeMinute,
	Genre Genres
)
{
	internal static readonly ICol[] Cols =
	{
		new TitleIdCol(0),
		new CategoryCol(1, "Type"),
		new StringCol(2, "PrimaryTitle"),
		new StringCol(3, "OriginalTitle"),
		new BoolCol(4, "IsAdult"),
		new NumberOptCol(5, "StartYear"),
		new NumberOptCol(6, "EndYear"),
		new NumberOptCol(7, "RuntimeMinute"),
		new GenresCol(8, "Genres"),
	};
	public static TitleBasicsRec MakeFromVals(object?[] vals) => new(
		(int)vals[0]!,
		(string)vals[1]!,
		(string)vals[2]!,
		(string)vals[3]!,
		(bool)vals[4]!,
		(int?)vals[5],
		(int?)vals[6],
		(int?)vals[7],
		(Genre)vals[8]!
	);
}