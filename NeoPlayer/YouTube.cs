using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

namespace NeoPlayer
{
	static class YouTube
	{
		static string GetID(string baseUrl, string url)
		{
			var match = Regex.Match(url, @"[&?]v=([^&]*)(?:&|$)");
			if (!match.Success)
				throw new Exception("Unable to get stream URL");
			return match.Groups[1].Value;
		}

		async public static Task<List<MediaData>> GetSuggestions(string searchTerm, CancellationToken token)
		{
			var url = $"https://www.youtube.com/results?sp=EgIQAQ%253D%253D&q={HttpUtility.UrlEncode(searchTerm)}";
			var html = await URLDownloader.GetURLString(url, token);

			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var videoNodes = doc.DocumentNode.SelectNodes("//ol[@class='item-section']//li//h3//a").ToList();

			return videoNodes.Select(videoNode => new MediaData
			{
				Description = HttpUtility.HtmlDecode(videoNode.InnerText.Trim()),
				URL = $"youtube:///{GetID(url, videoNode.Attributes["href"]?.Value)}",
			}).ToList();
		}

		public async static Task<string> GetURL(string url)
		{
			if (!url.StartsWith("youtube:///"))
				return url;

			var id = url.Substring("youtube:///".Length);
			return await AsyncHelper.ThreadPoolRunAsync(() =>
			{
				var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = Settings.YouTubeDLPath,
						Arguments = $"-g https://www.youtube.com/watch?v={id}",
						UseShellExecute = false,
						RedirectStandardOutput = true,
						CreateNoWindow = true,
					}
				};
				process.Start();
				var result = "";
				while (!process.StandardOutput.EndOfStream)
				{
					var line = process.StandardOutput.ReadLine();
					if (!string.IsNullOrWhiteSpace(line))
						result = line;
				}
				process.WaitForExit();
				return result;
			});
		}
	}
}
