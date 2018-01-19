using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NeoPlayer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NeoPlayer.Downloaders
{
	static class VideoFileDownloader
	{
		const int Concurrency = 4;

		static int ID = 0;
		static int GetID() => ++ID;

		static readonly Dictionary<string, int> TagIDs = new Dictionary<string, int>();

		async public static void DownloadAsync(string url, Action<int, DownloadData> updateDownload, Action done)
		{
			List<VideoFile> videoFiles;
			var id = GetID();
			try
			{
				updateDownload(id, new DownloadData { Title = url, Progress = 0 });
				videoFiles = await GetVideoFiles(url);
			}
			finally
			{
				updateDownload(id, null);
			}
			if (!videoFiles.Any())
				return;

			var parameters = Enumerable.Range(0, videoFiles.Count).ToDictionary(index => $"@Identifier{index}", index => (object)videoFiles[index].Identifier);
			var found = await Database.GetAsync<VideoFile>($"Identifier IN ({string.Join(", ", parameters.Keys)})", parameters);
			var foundIdentifiers = new HashSet<string>(found.Select(file => file.Identifier));
			videoFiles = videoFiles.Where(file => !foundIdentifiers.Contains(file.Identifier)).ToList();

			await DownloadFilesAsync(videoFiles, updateDownload, done);
		}

		public static void Update() => Process.Start(Settings.YouTubeDLPath, "-U");

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
						DownloadDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
						URL = obj.Item1,
					});
				}

				return results;
			}
		}

		async static Task DownloadFilesAsync(List<VideoFile> videoFiles, Action<int, DownloadData> updateDownload, Action done)
		{
			var queue = new Queue<VideoFile>(videoFiles);
			var running = new HashSet<Task>();
			while (true)
			{
				while ((running.Count != Concurrency) && (queue.Any()))
				{
					var videoFile = queue.Dequeue();
					running.Add(DownloadFileAsync(videoFile, updateDownload, done));
				}
				if (running.Count == 0)
					break;
				running.Remove(await Task.WhenAny(running));
			}
		}

		async static Task DownloadFileAsync(VideoFile videoFile, Action<int, DownloadData> updateDownload, Action done)
		{
			for (var pass = 0; pass < 2; ++pass)
			{
				var found = Directory.EnumerateFiles(Settings.VideosPath).Where(path => Path.GetFileNameWithoutExtension(path).EndsWith($"-{videoFile.Identifier}")).FirstOrDefault();
				if (found != null)
				{
					videoFile.FileName = Path.GetFileName(found);
					await Database.AddOrUpdateAsync(videoFile);
					foreach (var pair in videoFile.Tags)
					{
						if (!TagIDs.ContainsKey(pair.Key))
						{
							var tag = Database.GetAsync<Tag>($"{nameof(Tag.Name)} = @Name", new Dictionary<string, object> { ["Name"] = pair.Key }).Result.FirstOrDefault();
							if (tag == null)
							{
								tag = new Tag { Name = pair.Key };
								await Database.AddOrUpdateAsync(tag);
							}
							TagIDs[pair.Key] = tag.TagID;
						}
						await Database.AddOrUpdateAsync(new TagValue { VideoFileID = videoFile.VideoFileID, TagID = TagIDs[pair.Key], Value = pair.Value });
					}
					done();
					return;
				}

				if (pass != 0)
					return;

				await RunYouTubeDL(videoFile, updateDownload, done);
			}
		}

		async static Task RunYouTubeDL(VideoFile videoFile, Action<int, DownloadData> updateDownload, Action done)
		{
			var id = GetID();
			try
			{
				var downloadData = new DownloadData { Title = videoFile.Title, Progress = 0 };
				updateDownload(id, downloadData);
				using (var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = Settings.YouTubeDLPath,
						Arguments = $@"-iwc -o ""{string.Concat($"{videoFile.Title}-{videoFile.Identifier}".Split(Path.GetInvalidFileNameChars()))}.%(ext)s"" --ffmpeg-location ""{Settings.FFMpegPath}"" --no-playlist ""{videoFile.URL}""",
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
							var percent = (int)double.Parse(match.Groups[1].Value);
							if (percent != downloadData.Progress)
							{
								downloadData.Progress = percent;
								updateDownload(id, downloadData);
							}
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
				}
			}
			finally
			{
				updateDownload(id, null);
			}
		}
	}
}
