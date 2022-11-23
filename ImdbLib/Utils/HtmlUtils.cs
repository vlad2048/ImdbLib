using PowMaybeErr;
using RestSharp;

namespace ImdbLib.Utils;

static class HtmlUtils
{
	private static readonly Dictionary<string, string> headers = new()
	{
		{ "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36" },
	};

	public static async Task<MaybeErr<string>> GetPage(string url, CancellationToken cancelToken) => await WrapExT(async () =>
	{
		var client = new RestClient(url);
		var request = MakeRequest();
		var response = await client.ExecuteGetAsync(request, cancelToken);
		return response.Content!;
	});

	public static async Task<MaybeErr<bool>> DownloadPage(string url, string file) => await WrapExT(async () =>
	{
		var client = new RestClient(url);
		var request = MakeRequest();
		var bytes = await client.DownloadDataAsync(request);
		await File.WriteAllBytesAsync(file, bytes!);
		return true;
	});

	private static RestRequest MakeRequest()
	{
		var req = new RestRequest(string.Empty);
		req.AddHeaders(headers);
		return req;
	}
}