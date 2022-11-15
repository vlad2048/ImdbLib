using ImdbLib.Utils.Exts;

namespace ImdbLib.Structs.Enums;

[Flags]
public enum Genre : uint
{
	Action = 0x01,
	Adventure = 0x02,
	Biography = 0x04,
	Drama = 0x08,
	Fantasy = 0x10,
	Comedy = 0x20,
	War = 0x40,
	Documentary = 0x80,
	Crime = 0x100,
	Romance = 0x200,
	Family = 0x400,
	History = 0x800,
	SciFi = 0x1000,
	Thriller = 0x2000,
	Western = 0x4000,
	Short = 0x8000,
	Sport = 0x10000,
	Mystery = 0x20000,
	Horror = 0x40000,
	Music = 0x80000,
	Animation = 0x100000,
	Musical = 0x200000,
	FilmNoir = 0x400000,
	News = 0x800000,
	Adult = 0x1000000,
	RealityTV = 0x2000000,
	GameShow = 0x4000000,
	TalkShow = 0x8000000
}

public static class GenreUtils
{
	/*public static bool HasGenre(Genre genres, Genre genre)
	{
		var bits = genres & genre;
		return bits != 0;
	}*/

	public static string GetGenresDescription(Genre genres)
	{
		var vals = Enum.GetValues<Genre>();
		return vals
			.Where(e => genres.HasFlag(e))
			.Select(e => $"{e}")
			.JoinText();
	}

	public static Genre ParseGenres(string str)
	{
		if (string.IsNullOrEmpty(str) || str == @"\N")
			return 0;
		var vals = str
			.Split(',')
			.Select(e => e.Replace("-", ""))
			.Select(Enum.Parse<Genre>)
			.Select(e => (uint)e);
		uint res = 0;
		foreach (var val in vals)
			res |= val;
		return (Genre)res;
	}
}