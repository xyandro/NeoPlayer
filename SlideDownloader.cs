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

		public async static void Run(AsyncQueue<string> urls, Action<string> action, CancellationToken token)
		{
			var outputFiles = new AsyncQueue<string>();
			AsyncHelper.RunTasks(urls, url => FetchSlideAsync(url, action, token), outputFiles, token);

			var fileNames = new List<string>();
			while (await outputFiles.HasItemsAsync(token))
				fileNames.Add(outputFiles.Dequeue());

			try { await Task.Delay(-1, token); }
			catch (TaskCanceledException) { }
			fileNames.Where(file => file != null).ToList().ForEach(File.Delete);
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
