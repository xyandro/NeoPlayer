using System;
using System.Collections.Concurrent;
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
	static class ImageDownloader
	{
		const int NumTasks = 20;

		static Task task = null;
		static CancellationTokenSource token = null;
		static readonly BlockingCollection<Action> threadWork = new BlockingCollection<Action>();

		static ImageDownloader()
		{
			new Thread(() => { while (true) threadWork.Take()(); }).Start();
		}

		static Task RunInThread(Action action)
		{
			var tcs = new TaskCompletionSource<object>();
			threadWork.Add(() =>
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

		async public static void Run(string slidesQuery, string imageSize, Actions actions)
		{
			if (task != null)
			{
				token.Cancel();
				await task;
				token = null;
				task = null;
			}

			var regex = new Regex($@"^{nameof(NeoRemote)}-Image-[0-9a-f]{{32}}\.bmp$$", RegexOptions.IgnoreCase);
			if (!Settings.Debug)
				Directory.EnumerateFiles(Settings.SlidesPath).Where(file => regex.IsMatch(Path.GetFileName(file))).ToList().ForEach(file => File.Delete(file));
			actions.ClearImages();

			if (string.IsNullOrWhiteSpace(slidesQuery))
				return;

			token = new CancellationTokenSource();
			task = DownloadImages(slidesQuery, imageSize, actions, token.Token);
		}

		async static Task<List<TOutput>> RunTasks<TInput, TOutput>(IEnumerable<TInput> input, Func<TInput, Task<TOutput>> func, CancellationToken token)
		{
			var tasks = new HashSet<Task<TOutput>>();
			var results = new List<TOutput>();
			var enumerator = input.GetEnumerator();
			while (true)
			{
				while ((tasks.Count < NumTasks) && (!token.IsCancellationRequested) && (enumerator.MoveNext()))
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

		async static Task DownloadImages(string slidesQuery, string imageSize, Actions actions, CancellationToken token)
		{
			var client = new HttpClient();
			client.Timeout = TimeSpan.FromSeconds(30);
			client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0");

			var queries = slidesQuery.Split('\n').ToList();

			var urls = (await RunTasks(queries, query => GetImageURLs(client, query, imageSize, token), token)).SelectMany(x => x).ToList();

			var random = new Random();
			urls = urls.OrderBy(x => random.Next()).ToList();

			await RunTasks(urls, url => FetchImage(client, url, actions, token), token);
		}

		async static Task<List<string>> GetImageURLs(HttpClient client, string query, string imageSize, CancellationToken token)
		{
			try
			{
				List<string> urls = null;

				var fileName = $@"{Settings.SlidesPath}\{nameof(NeoRemote)}-URLs-{query}.txt";
				if ((Settings.Debug) && (File.Exists(fileName)))
					urls = File.ReadAllLines(fileName).ToList();

				if (urls == null)
				{
					var uri = $"https://www.google.com/search?q={Uri.EscapeUriString(query)}&tbm=isch&tbs=isz:lt,islt:{imageSize}";
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

		async static Task FetchImage(HttpClient client, string url, Actions actions, CancellationToken token)
		{
			string md5;
			using (var md5cng = new MD5Cng())
				md5 = BitConverter.ToString(md5cng.ComputeHash(Encoding.UTF8.GetBytes(url))).Replace("-", "");
			var fileName = $@"{Settings.SlidesPath}\{nameof(NeoRemote)}-Image-{md5}.bmp";

			try
			{
				if (!File.Exists(fileName))
				{
					var request = new HttpRequestMessage(HttpMethod.Get, url);
					request.Headers.Referrer = new Uri(url);
					var response = await client.SendAsync(request, token);
					if (response.StatusCode != System.Net.HttpStatusCode.OK)
						throw new Exception($"Failed to fetch page: {response.StatusCode}");
					var stream = await response.Content.ReadAsStreamAsync();

					await RunInThread(() => ShrinkImage(stream, fileName, token));
				}

				actions.EnqueueImages(new List<string> { fileName });
			}
			catch { File.WriteAllBytes(fileName, new byte[] { }); }
		}

		static void ShrinkImage(Stream stream, string outputFileName, CancellationToken token)
		{
			token.ThrowIfCancellationRequested();

			var image = new BitmapImage();
			image.BeginInit();
			image.StreamSource = stream;
			image.CacheOption = BitmapCacheOption.OnLoad;
			image.EndInit();

			var width = image.PixelWidth;
			var height = image.PixelHeight;
			var group = new DrawingGroup();
			RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
			group.Children.Add(new ImageDrawing(image, new Rect(0, 0, width, height)));

			var drawingVisual = new DrawingVisual();
			using (var drawingContext = drawingVisual.RenderOpen())
				drawingContext.DrawDrawing(group);

			var scale = Math.Min(SystemParameters.PrimaryScreenWidth / width, SystemParameters.PrimaryScreenHeight / height);
			var resizedImage = new RenderTargetBitmap((int)(width * scale), (int)(height * scale), 96, 96, PixelFormats.Default);
			resizedImage.Render(drawingVisual);

			var encoder = new BmpBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(resizedImage));
			using (var output = File.Create(outputFileName))
				encoder.Save(output);
		}
	}
}
