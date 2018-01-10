using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NeoPlayer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NeoPlayer
{
	static class VideoFileDownloader
	{
		const int Concurrency = 6;

		async public static void DownloadAsync(Database db, string url)
		{
			var videoFiles = await GetVideoFiles(url);
			if (!videoFiles.Any())
				return;

			var parameters = Enumerable.Range(0, videoFiles.Count).ToDictionary(index => $"@Identifier{index}", index => (object)videoFiles[index].Identifier);
			var found = await db.GetAsync<VideoFile>($"Identifier IN ({string.Join(", ", parameters.Keys)})", parameters);
			var foundIdentifiers = new HashSet<string>(found.Select(file => file.Identifier));
			videoFiles = videoFiles.Where(file => !foundIdentifiers.Contains(file.Identifier)).ToList();

			await DownloadFilesAsync(db, videoFiles);
		}

		async static Task<List<VideoFile>> GetVideoFiles(string url)
		{
			using (var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = Settings.YouTubeDLPath,
					Arguments = $@"-iJ ""{url}"" --flat-playlist",
					WorkingDirectory = Settings.VideosPath,
					UseShellExecute = false,
					StandardOutputEncoding = Encoding.Default,
					StandardErrorEncoding = Encoding.Default,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				}
			})
			{
				var output = "";
				process.OutputDataReceived += (s, e) =>
				{
					if (e.Data == null)
						return;

					output += e.Data;
				};
				process.ErrorDataReceived += (s, e) => { };
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				var tcs = new TaskCompletionSource<object>();
				process.EnableRaisingEvents = true;
				process.Exited += (s, e) => { process.WaitForExit(); tcs.TrySetResult(null); };
				try { await tcs.Task; }
				catch when (!process.HasExited)
				{
					process.Kill();
					throw;
				}

				var results = new List<VideoFile>();
				var queue = new Queue<Tuple<string, JToken>>();
				queue.Enqueue(Tuple.Create(url, JsonConvert.DeserializeObject(output) as JToken));
				while (queue.Any())
				{
					var obj = queue.Dequeue();
					var json = obj.Item2;
					var extractor = ((json["extractor"] as JValue) ?? (json["ie_key"] as JValue))?.ToString().ToLower();
					if (extractor == "youtube:playlist")
					{
						foreach (var child in json["entries"].Children())
						{
							var child_id = (child["id"] as JValue)?.ToString();
							if (child_id == null)
								continue;
							queue.Enqueue(Tuple.Create($"https://www.youtube.com/watch?v={child_id}", child));
						}
						continue;
					}
					var id = (json["id"] as JValue)?.ToString();
					var title = (json["title"] as JValue)?.ToString();
					if ((extractor == null) || (id == null) || (title == null))
						continue;

					results.Add(new VideoFile
					{
						Identifier = $"{extractor}-{id}",
						Title = title,
						URL = obj.Item1,
					});
				}

				return results;
			}
		}

		async static Task DownloadFilesAsync(Database db, List<VideoFile> videoFiles)
		{
			var queue = new Queue<VideoFile>(videoFiles);
			var running = new HashSet<Task>();
			while (true)
			{
				while ((running.Count != Concurrency) && (queue.Any()))
				{
					var videoFile = queue.Dequeue();
					running.Add(DownloadFileAsync(db, videoFile));
				}
				if (running.Count == 0)
					break;
				running.Remove(await Task.WhenAny(running));
			}
		}

		async static Task DownloadFileAsync(Database db, VideoFile videoFile)
		{
			using (var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = Settings.YouTubeDLPath,
					Arguments = $@"-iwc -o ""{videoFile.GetSanitizedTitle()}.%(ext)s"" --ffmpeg-location ""{Settings.FFMpegPath}"" --no-playlist ""{videoFile.URL}""",
					WorkingDirectory = Settings.VideosPath,
					UseShellExecute = false,
					StandardOutputEncoding = Encoding.Default,
					StandardErrorEncoding = Encoding.Default,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				}
			})
			{
				process.OutputDataReceived += (s, e) =>
				{
					if (e.Data == null)
						return;

					var match = Regex.Match(e.Data, @"^\[download\]\s*([0-9.]+)%(?:\s|$)");
					if (match.Success)
					{
						var percent = double.Parse(match.Groups[1].Value);
						//progress?.Report(new ProgressReport(percent, 100));
					}
				};
				process.ErrorDataReceived += (s, e) => { };
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				var tcs = new TaskCompletionSource<object>();
				process.EnableRaisingEvents = true;
				process.Exited += (s, e) => { process.WaitForExit(); tcs.TrySetResult(null); };
				try { await tcs.Task; }
				catch when (!process.HasExited)
				{
					process.Kill();
					throw;
				}

				db.AddOrUpdate(videoFile);
			}
		}
	}
}
