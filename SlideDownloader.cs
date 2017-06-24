using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		static SlideDownloader()
		{
			var regex = new Regex($@"^{Regex.Escape($@"{Settings.SlidesPath}\{nameof(NeoRemote)}-Slide-")}\d{{10}}.bmp$", RegexOptions.IgnoreCase);
			Directory.EnumerateFiles(Settings.SlidesPath).Where(file => regex.IsMatch(file)).ToList().ForEach(File.Delete);
		}

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
			var outputFiles = new AsyncQueue<string>();
			AsyncHelper.RunTasks(inputURLs, url => FetchSlideAsync(url, action, token), outputFiles, token);

			var fileNames = new List<string>();
			while (await outputFiles.HasItemsAsync(token))
				fileNames.Add(outputFiles.Dequeue());

			try { await Task.Delay(-1, token); }
			catch (TaskCanceledException) { }
			fileNames.Where(file => file != null).ToList().ForEach(File.Delete);
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

		static int slideCount = 0;
		async static Task<string> FetchSlideAsync(string url, Action<string> action, CancellationToken token)
		{
			var fileName = $@"{Settings.SlidesPath}\{nameof(NeoRemote)}-Slide-{++slideCount:0000000000}.bmp";

			try
			{
				using (var ms = await URLDownloader.GetURLData(url, token))
					await AsyncHelper.ThreadPoolRunAsync(() => ShrinkSlide(ms, fileName, token));

				token.ThrowIfCancellationRequested();

				action(fileName);
				return fileName;
			}
			catch
			{
				File.Delete(fileName);
				return null;
			}
		}

		static void ShrinkSlide(Stream stream, string outputFileName, CancellationToken token)
		{
			token.ThrowIfCancellationRequested();

			var slide = new BitmapImage();
			slide.BeginInit();
			slide.StreamSource = stream;
			slide.CacheOption = BitmapCacheOption.OnLoad;
			slide.EndInit();

			token.ThrowIfCancellationRequested();

			var scale = Math.Min(SystemParameters.PrimaryScreenWidth / slide.PixelWidth, SystemParameters.PrimaryScreenHeight / slide.PixelHeight);
			var resizedSlide = new TransformedBitmap(slide, new ScaleTransform(scale, scale));

			var encoder = new BmpBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(resizedSlide));
			using (var output = File.Create(outputFileName))
				encoder.Save(output);
		}
	}
}
