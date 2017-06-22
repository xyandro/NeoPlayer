﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NeoRemote
{
	static class SlideDownloader
	{
		const int DownloaderCount = 20;

		static Task task = null;
		static CancellationTokenSource token = null;

		static Task ThreadPoolRunAsync(Action action)
		{
			var tcs = new TaskCompletionSource<object>();
			ThreadPool.QueueUserWorkItem(state =>
			{
				try
				{
					action();
					tcs.SetResult(null);
				}
				catch (Exception ex) { tcs.SetException(ex); }
			});
			return tcs.Task;
		}

		async public static void Run(string slidesQuery, string size, Actions actions)
		{
			if (task != null)
			{
				token.Cancel();
				await task;
				token = null;
				task = null;
			}

			var regex = new Regex($@"^{nameof(NeoRemote)}-Slide-[0-9a-f]{{32}}\.bmp$$", RegexOptions.IgnoreCase);
			if (!Settings.Debug)
				Directory.EnumerateFiles(Settings.SlidesPath).Where(file => regex.IsMatch(Path.GetFileName(file))).ToList().ForEach(file => File.Delete(file));
			actions.ClearSlides();

			if (string.IsNullOrWhiteSpace(slidesQuery))
				return;

			token = new CancellationTokenSource();
			task = DownloadSlides(slidesQuery, size, actions, token.Token);
		}

		async static Task<List<TOutput>> RunTasks<TInput, TOutput>(IEnumerable<TInput> input, Func<TInput, Task<TOutput>> func, CancellationToken token)
		{
			var tasks = new HashSet<Task<TOutput>>();
			var results = new List<TOutput>();
			var enumerator = input.GetEnumerator();
			while (true)
			{
				while ((tasks.Count < DownloaderCount) && (!token.IsCancellationRequested) && (enumerator.MoveNext()))
					tasks.Add(func(enumerator.Current));

				if (!tasks.Any())
					break;

				var finished = await Task.WhenAny(tasks.ToArray());
				results.Add(finished.Result);
				tasks.Remove(finished);
			}
			return results;
		}

		async static Task RunTasks<TInput>(IEnumerable<TInput> input, Func<TInput, Task> func, CancellationToken token) => await RunTasks(input, async item => { await func(item); return false; }, token);

		async static Task DownloadSlides(string slidesQuery, string size, Actions actions, CancellationToken token)
		{
			var client = new HttpClient();
			client.Timeout = TimeSpan.FromSeconds(30);
			client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0");

			var queries = slidesQuery.Split('\n').ToList();

			var urls = (await RunTasks(queries, query => GetSlideURLs(client, query, size, token), token)).SelectMany(x => x).ToList();

			var random = new Random();
			urls = urls.OrderBy(x => random.Next()).ToList();

			await RunTasks(urls, url => FetchSlide(client, url, actions, token), token);
		}

		async static Task<List<string>> GetSlideURLs(HttpClient client, string query, string size, CancellationToken token)
		{
			try
			{
				List<string> urls = null;

				var fileName = $@"{Settings.SlidesPath}\{nameof(NeoRemote)}-URLs-{query}-{size}.txt";
				if ((Settings.Debug) && (File.Exists(fileName)))
					urls = File.ReadAllLines(fileName).ToList();

				if (urls == null)
				{
					var uri = $"https://www.google.com/search?q={Uri.EscapeUriString(query)}&tbm=isch&tbs=isz:lt,islt:{size}";
					var response = await client.GetAsync(uri, token);
					var data = await response.Content.ReadAsStringAsync();
					urls = Regex.Matches(data, @"""ou"":""(.*?)""").Cast<Match>().Select(match => match.Groups[1].Value).ToList();

					if (Settings.Debug)
						File.WriteAllLines(fileName, urls);
				}

				return urls;
			}
			catch { return new List<string>(); }
		}

		async static Task FetchSlide(HttpClient client, string url, Actions actions, CancellationToken token)
		{
			string md5;
			using (var md5cng = new MD5Cng())
				md5 = BitConverter.ToString(md5cng.ComputeHash(Encoding.UTF8.GetBytes(url))).Replace("-", "");
			var fileName = $@"{Settings.SlidesPath}\{nameof(NeoRemote)}-Slide-{md5}.bmp";

			if (!File.Exists(fileName))
			{
				try
				{
					using (var request = new HttpRequestMessage(HttpMethod.Get, url))
					{
						request.Headers.Referrer = new Uri(url);
						using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token))
						{
							if (response.StatusCode != System.Net.HttpStatusCode.OK)
								throw new Exception($"Failed to fetch page: {response.StatusCode}");

							using (var ms = new MemoryStream((int)(response.Content.Headers.ContentLength ?? 0)))
							{
								using (var stream = await response.Content.ReadAsStreamAsync())
									await stream.CopyToAsync(ms);
								ms.Position = 0;
								await ThreadPoolRunAsync(() => ShrinkSlide(ms, fileName, token));
							}
						}
					}
				}
				catch { File.WriteAllBytes(fileName, new byte[] { }); }
			}

			var fileInfo = new FileInfo(fileName);
			if ((fileInfo.Exists) && (fileInfo.Length != 0))
				actions.EnqueueSlides(new List<string> { fileName });
		}

		static void ShrinkSlide(Stream stream, string outputFileName, CancellationToken token)
		{
			token.ThrowIfCancellationRequested();

			var slide = new BitmapImage();
			slide.BeginInit();
			slide.StreamSource = stream;
			slide.CacheOption = BitmapCacheOption.OnLoad;
			slide.EndInit();

			var scale = Math.Min(SystemParameters.PrimaryScreenWidth / slide.PixelWidth, SystemParameters.PrimaryScreenHeight / slide.PixelHeight);
			var resizedSlide = new TransformedBitmap(slide, new ScaleTransform(scale, scale));

			var encoder = new BmpBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(resizedSlide));
			using (var output = File.Create(outputFileName))
				encoder.Save(output);
		}
	}
}
