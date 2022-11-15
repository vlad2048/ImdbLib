using PowMaybeErr;
using RestSharp;

namespace ImdbLib.Utils;

static class HtmlUtils
{
	public static async Task<MaybeErr<string>> GetPage(string url, CancellationToken cancelToken) => await WrapExT(async () =>
	{
		var client = new RestClient(url);
		var request = new RestRequest(string.Empty);
		var response = await client.ExecuteGetAsync(request, cancelToken);
		return response.Content!;
	});

	public static async Task<MaybeErr<bool>> DownloadPage(string url, string file) => await WrapExT(async () =>
	{
		var client = new RestClient(url);
		var bytes = await client.DownloadDataAsync(new RestRequest(string.Empty));
		await File.WriteAllBytesAsync(file, bytes!);
		return true;
	});
}