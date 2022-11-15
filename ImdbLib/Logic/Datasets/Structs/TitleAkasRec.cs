using ImdbLib.Logic.Datasets.TsvLoading.Structs;

namespace ImdbLib.Logic.Datasets.Structs;

/*

TitleAkas
=========
titleId	ordering	title	region	language	types	attributes	isOriginalTitle
tt0000001	1	Карменсіта	UA	\N	imdbDisplay	\N	0
tt0110912	10	Pulp Fiction	US	\N	imdbDisplay	\N	0

*/

record TitleAkasRec(
	int Id,
	int Ordering,
	string Title,
	string Region,
	string Language,
	string[] Types,
	string Attributes,
	bool IsOriginalTitle
)
{
	public static readonly ICol[] Cols =
	{
		new TitleIdCol(0),
		new NumberCol(1, "Ordering"),
		new StringCol(2, "Title"),
		new StringCol(3, "Region"),
		new StringCol(4, "Language"),
		new CategoryArrayCol(5, "Types"),
		new StringCol(6, "Attributes"),
		new BoolCol(7, "IsOriginalTitle"),
	};
	public static TitleAkasRec MakeFromVals(object?[] vals) => new(
		(int)vals[0]!,
		(int)vals[1]!,
		(string)vals[2]!,
		(string)vals[3]!,
		(string)vals[4]!,
		(string[])vals[5]!,
		(string)vals[6]!,
		(bool)vals[7]!
	);
}