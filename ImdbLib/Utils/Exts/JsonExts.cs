using System.Text.Json;

namespace ImdbLib.Utils.Exts;

static class JsonExts
{
	private static readonly JsonSerializerOptions jsonOpt = new()
	{
		WriteIndented = true
	};

	public static void SaveJson<T>(this string file, T obj)
	{
		var str = JsonSerializer.Serialize(obj, jsonOpt);
		File.WriteAllText(file, str);
	}

	public static T LoadJson<T>(this string file, Func<T>? emptyFun = null)
	{
		if (emptyFun != null && !File.Exists(file))
			File.WriteAllText(file, JsonSerializer.Serialize(emptyFun(), jsonOpt));
		var str = File.ReadAllText(file);
		var obj = JsonSerializer.Deserialize<T>(str, jsonOpt);
		return obj!;
	}
}