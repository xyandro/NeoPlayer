using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NeoPlayer
{
	static class GoogleSlideSource
	{
		public async static void Run(string slidesQuery, string size, Action<string> action, CancellationToken token)
		{
			if (string.IsNullOrWhiteSpace(slidesQuery))
				return;

			var inputQueries = new AsyncQueue<string>();
			slidesQuery.Split('\n').ToList().ForEach(query => inputQueries.Enqueue(query));
			inputQueries.SetFinished();
			var outputURLs = new AsyncQueue<List<string>>();

			AsyncHelper.RunTasks(inputQueries, query => GetSlideURLs(query, size, token), outputURLs, token);

			var urls = new List<string>();
			while (await outputURLs.HasItemsAsync(token))
				foreach (var item in outputURLs.Dequeue())
					urls.Add(item);

			var random = new Random();
			urls = urls.OrderBy(x => random.Next()).ToList();

			var inputURLs = new AsyncQueue<string>();
			urls.ForEach(url => inputURLs.Enqueue(url));
			inputURLs.SetFinished();

			SlideDownloader.Run(inputURLs, action, token);
		}

		async static Task<List<string>> GetSlideURLs(string query, string size, CancellationToken token)
		{
			try
			{
				var url = $"https://www.google.com/search?q={Uri.EscapeUriString(query)}&tbm=isch&tbs=isz:lt,islt:{size}";
				var data = await URLDownloader.GetURLString(url, token);
				return Regex.Matches(data, @"""ou"":""(.*?)""").Cast<Match>().Select(match => match.Groups[1].Value).ToList();
			}
			catch { return new List<string>(); }
		}
	}
}
