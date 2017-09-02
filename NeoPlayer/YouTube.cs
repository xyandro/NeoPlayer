using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NeoPlayer
{
	static class YouTube
	{
		class WorkItem
		{
			public string URL { get; set; }
			public Uri URI { get; set; }
			public Exception Exception { get; set; }
			public List<TaskCompletionSource<Uri>> Tasks { get; } = new List<TaskCompletionSource<Uri>>();
		}
		readonly static List<WorkItem> workItems = new List<WorkItem>();

		static string GetID(string baseUrl, string url)
		{
			var match = Regex.Match(url, @"[&?]v=([^&]*)(?:&|$)");
			if (!match.Success)
				return null;
			return match.Groups[1].Value;
		}

		async public static Task<List<MediaData>> GetSuggestionsAsync(string searchTerm, CancellationToken token)
		{
			var url = $"https://www.youtube.com/results?sp=EgIQAQ%253D%253D&q={HttpUtility.UrlEncode(searchTerm)}";
			var html = await URLDownloader.GetURLString(url, token);

			var match = Regex.Match(html, @"window\[""ytInitialData""]\s*=\s*([^;]+);");
			if (!match.Success)
				return new List<MediaData>();

			var jsonStr = match.Groups[1].Value;
			var json = JsonConvert.DeserializeObject(jsonStr) as JObject;
			return json.Descendants()
				.OfType<JProperty>()
				.Where(x => x.Name == "videoRenderer")
				.Select(x => new
				{
					id = x.Descendants()
						.OfType<JProperty>()
						.Where(y => y.Name == "videoId")
						.Select(y => y.Value.Value<string>())
						.FirstOrDefault(),
					title = x.Descendants()
						.OfType<JProperty>()
						.Where(y => y.Name == "title")
						.SelectMany(y => y.Descendants())
						.OfType<JProperty>()
						.Where(y => y.Name == "simpleText")
						.Select(y => y.Value.Value<string>())
						.FirstOrDefault()
				})
				.Where(x => (!string.IsNullOrWhiteSpace(x.id)) && (!string.IsNullOrWhiteSpace(x.title)))
				.Select(x => new MediaData
				{
					Description = x.title,
					URL = $"http://127.0.0.1:{Settings.Port}/fetch?url={HttpUtility.UrlEncode($"youtube://{x.id}")}",
				})
				.ToList();
		}

		public async static void PrepURL(string url)
		{
			string urlPrefix = $"http://127.0.0.1:{Settings.Port}/fetch?url=youtube%3a%2f%2f";
			if (!url.StartsWith(urlPrefix))
				return;

			url = HttpUtility.ParseQueryString(new Uri(url).Query)["url"];
			if (workItems.Any(item => item.URL == url))
				return;

			var workItem = new WorkItem { URL = url };
			workItems.Add(workItem);

			try
			{
				url = $"https://www.youtube.com/watch?v={url.Substring("youtube://".Length)}";
				url = await AsyncHelper.ThreadPoolRunAsync(() =>
				{
					var auth = "";

					tryagain:
					var process = new Process
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = Settings.YouTubeDLPath,
							Arguments = $"-g -f best {auth} {url}",
							UseShellExecute = false,
							RedirectStandardOutput = true,
							RedirectStandardError = true,
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
					var error = process.StandardError.ReadToEnd();
					process.WaitForExit();

					if ((!string.IsNullOrWhiteSpace(error)) && (auth == ""))
					{
						auth = Cryptor.Decrypt("EAAAABmwMiKoDUeKbZV2BxXYkyU+2w+qqAgxIHab8Q1RiPHrFzBKe2MnNISvvyuMKQFry31r/G2XhODOQpVwll3icTk=");
						goto tryagain;
					}

					return result;
				});

				if (string.IsNullOrWhiteSpace(url))
					throw new Exception("Unable to get YouTube URL");

				var httpClient = new HttpClient();
				httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(0, 100);
				var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
				response.EnsureSuccessStatusCode();

				workItem.URI = response.RequestMessage.RequestUri;
				workItem.Tasks.ForEach(task => task.TrySetResult(workItem.URI));
			}
			catch (Exception ex)
			{
				workItem.Exception = ex;
				workItem.Tasks.ForEach(task => task.TrySetException(ex));
			}
			workItem.Tasks.Clear();
		}

		public async static Task<Uri> GetURLAsync(string url)
		{
			var workItem = workItems.FirstOrDefault(item => item.URL == url);
			if (workItem == null)
				return new Uri(url);

			if (workItem.URI != null)
				return workItem.URI;
			if (workItem.Exception != null)
				throw workItem.Exception;

			var tcs = new TaskCompletionSource<Uri>();
			workItem.Tasks.Add(tcs);
			return await tcs.Task;
		}
	}
}
