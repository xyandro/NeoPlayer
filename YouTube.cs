using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

namespace NeoRemote
{
	static class YouTube
	{
		static string GetID(string baseUrl, string url)
		{
			if (string.IsNullOrWhiteSpace(url))
				return null;

			url = new Uri(new Uri(baseUrl), url).AbsoluteUri;
			var query = new Uri(url).Query;
			var id = HttpUtility.ParseQueryString(query).Get("v");
			return id;
		}

		async public static void GetSuggestions(string searchTerm, CancellationToken token)
		{
			var url = $"https://www.youtube.com/results?search_query={HttpUtility.UrlEncode(searchTerm)}";
			var html = await URLDownloader.GetURLString(url, token);
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var videoNodes = doc.DocumentNode.SelectNodes("//ol[@class='item-section']/li");
			var youTubeItems = new List<YouTubeItem>();
			foreach (var videoNode in videoNodes)
			{
				youTubeItems.Add(new YouTubeItem
				{
					Title = videoNode.SelectSingleNode(".//h3/a")?.InnerText.Trim(),
					ID = GetID(url, videoNode.SelectSingleNode(".//h3/a")?.Attributes["href"]?.Value),
				});
			}
			var thumbnails = new AsyncQueue<YouTubeItem>();
			youTubeItems.ForEach(x => thumbnails.Enqueue(x));
			thumbnails.SetFinished();
			await AsyncHelper.RunTasks(thumbnails, item => GetThumbnail(item, token), token);
		}

		async static Task GetThumbnail(YouTubeItem youTubeItem, CancellationToken token)
		{
			var url = $"https://img.youtube.com/vi/{youTubeItem.ID}/0.jpg";
			youTubeItem.Thumbnail = (await URLDownloader.GetURLData(url, token)).ToArray();
		}

		public async static Task<string> GetURL(string ID)
		{
			return await AsyncHelper.ThreadPoolRunAsync(() =>
			{
				var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = Path.Combine(Path.GetDirectoryName(typeof(YouTube).Assembly.Location), "youtube-dl.exe"),
						Arguments = $"-g https://www.youtube.com/watch?v={ID}",
						UseShellExecute = false,
						RedirectStandardOutput = true,
						CreateNoWindow = true,
					}
				};
				process.Start();
				var url = "";
				while (!process.StandardOutput.EndOfStream)
				{
					var line = process.StandardOutput.ReadLine();
					if (!string.IsNullOrWhiteSpace(line))
						url = line;
				}
				process.WaitForExit();
				return url;
			});
		}
	}
}
