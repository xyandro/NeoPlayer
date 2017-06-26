using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeoPlayer
{
	static class URLDownloader
	{
		static readonly HttpClient client;
		static readonly string cacheDir = Path.Combine(Path.GetDirectoryName(typeof(SlideDownloader).Assembly.Location), "Cache");

		static URLDownloader()
		{
			client = new HttpClient();
			client.Timeout = TimeSpan.FromSeconds(30);
			client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0");

			if (Settings.Debug)
				Directory.CreateDirectory(cacheDir);
		}

		public async static Task<MemoryStream> GetURLData(string url, CancellationToken token)
		{
			string fileName;
			using (var md5cng = new MD5Cng())
				fileName = $@"{cacheDir}\Cache-{BitConverter.ToString(md5cng.ComputeHash(Encoding.UTF8.GetBytes(url))).Replace("-", "")}.dat";

			MemoryStream result;
			if (Settings.Debug)
			{
				var fileInfo = new FileInfo(fileName);
				if (fileInfo.Exists)
				{
					result = new MemoryStream((int)fileInfo.Length);
					using (var input = File.OpenRead(fileName))
						await input.CopyToAsync(result, 81920, token);
					result.Position = 0;
					return result;
				}
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url))
			{
				request.Headers.Referrer = new Uri(url);
				using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token))
				{
					if (response.StatusCode != HttpStatusCode.OK)
						throw new Exception($"Failed to URL: {response.StatusCode}");

					result = new MemoryStream((int)(response.Content.Headers.ContentLength ?? 0));
					using (var stream = await response.Content.ReadAsStreamAsync())
						await stream.CopyToAsync(result, 81920, token);
					result.Position = 0;
				}
			}

			if (Settings.Debug)
			{
				using (var output = File.Create(fileName))
					await result.CopyToAsync(output, 81920, token);
				result.Position = 0;
			}

			return result;
		}

		public async static Task<string> GetURLString(string url, CancellationToken token)
		{
			using (var stream = await GetURLData(url, token))
			using (var reader = new StreamReader(stream))
				return await reader.ReadToEndAsync();
		}
	}
}
