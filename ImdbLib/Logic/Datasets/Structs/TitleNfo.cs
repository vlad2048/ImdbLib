using ImdbLib.Structs.Enums;

namespace ImdbLib.Logic.Datasets.Structs;

record TitleNfo(
	int Id,
	string Name,
	string? OriginalName,
	int Year,
	int? Runtime,
	Genre Genres
)
{
	public virtual bool Equals(TitleNfo? other) => other?.Id == Id;
	public override int GetHashCode() => Id.GetHashCode();

	public static bool CanMakeFromTitleBasics(TitleBasicsRec t) => t.StartYear.HasValue;

	public static TitleNfo MakeFromTitleBasics(TitleBasicsRec t) => new(
		t.Id,
		t.PrimaryTitle,
		(t.OriginalTitle != t.PrimaryTitle) switch
		{
			true => t.OriginalTitle,
			false => null
		},
		t.StartYear!.Value,
		t.RuntimeMinute,
		t.Genres
	);
}