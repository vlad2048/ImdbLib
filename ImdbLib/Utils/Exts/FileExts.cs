using Directory = System.IO.Directory;

namespace ImdbLib.Utils.Exts;

static class FileExts
{
	public static string CreateFolderIFN(this string folder)
	{
		if (!Directory.Exists(folder))
			Directory.CreateDirectory(folder);
		return folder;
	}

	public static string CreateFolderForFileIFN(this string file)
	{
		Path.GetDirectoryName(file)!.CreateFolderIFN();
		return file;
	}

	public static string RemoveExtensionIf(this string file, bool condition) => condition switch
	{
		false => file,
		true => file[..^Path.GetExtension(file).Length]
	};

	public static string AddSuffixIf(this string file, bool condition, string suffix) => condition switch
	{
		false => file,
		true => Path.Combine(Path.GetDirectoryName(file)!, $"{Path.GetFileNameWithoutExtension(file)}{suffix}{Path.GetExtension(file)}")
	};
}