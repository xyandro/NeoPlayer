using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NeoPlayer.Downloaders;
using NeoPlayer.Misc;
using NeoPlayer.Models;
using NeoPlayer.Networking;

namespace NeoPlayer
{
	partial class NeoPlayerWindow
	{
		readonly SingleRunner updateState;
		readonly DispatcherTimer changeSlideTimer = null;
		readonly NeoServer neoServer;
		readonly Status status;
		readonly Dictionary<int, DownloadData> downloads = new Dictionary<int, DownloadData>();

		public NeoPlayerWindow()
		{
			neoServer = new NeoServer();
			neoServer.OnMessage += OnMessage;
			neoServer.OnConnect += OnConnect;
			neoServer.Run(Settings.Port);

			status = new Status(neoServer);
			updateState = new SingleRunner(UpdateState);
			slides.CollectionChanged += (s, e) => updateState.Signal();
			music.CollectionChanged += (s, e) => updateState.Signal();
			queue.CollectionChanged += (s, e) => { status.Queue = queue.Select(videoFile => videoFile.VideoFileID).ToList(); updateState.Signal(); };

			InitializeComponent();

			// Keep screen/computer on
			Win32.SetThreadExecutionState(Win32.ES_CONTINUOUS | Win32.ES_DISPLAY_REQUIRED | Win32.ES_SYSTEM_REQUIRED);

			var random = new Random();
			Directory.EnumerateFiles(Settings.MusicPath).OrderBy(x => random.Next()).Select(fileName => new MusicFile { FileName = fileName, Title = Path.GetFileNameWithoutExtension(fileName) }).ForEach(file => music.Add(file));

			SlidesQuery = Helpers.Debug ? "test" : "landscape";
			SlidesSize = "2mp";
			SlideDisplayTime = 60;
			SlidesPlaying = true;
			VideoState = null;
			MusicState = null;
			Volume = 50;

			mediaPlayer.MediaEnded += (s, e) => { AddHistory(); MediaForward(); };
			mediaPlayer.MediaFailed += (s, e) => MediaForward();

			changeSlideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.25) };
			changeSlideTimer.Tick += (s, e) => CheckCycleSlide();
			changeSlideTimer.Start();

			UpdateVideoFiles();

			status.History = new List<int>();
			status.Queue = new List<int>();
			status.Downloads = new List<DownloadData>();
		}

		void AddHistory()
		{
			if (CurrentVideo == null)
				return;

			history.Remove(CurrentVideo.VideoFileID);
			history.Insert(0, CurrentVideo.VideoFileID);
			status.History = history.ToList();
		}

		public void UpdateVideoFiles() => status.VideoFiles = Database.GetVideoFilesAsync().Result;

		void OnConnect(AsyncQueue<byte[]> queue) => status.SendAll(queue);

		void OnMessage(Message message, AsyncQueue<byte[]> queue)
		{
			var command = message.GetString();
			switch (command)
			{
				case "QueueVideos": QueueVideos(message.GetInts(), message.GetBool()); break;
				case "SetPosition": SetPosition(message.GetInt(), message.GetBool()); break;
				case "ToggleMediaPlaying": ToggleMediaPlaying(message.GetBool()); break;
				case "SetVolume": SetVolume(message.GetInt(), message.GetBool()); break;
				case "MediaForward": MediaForward(); break;
				case "ToggleSlidesPlaying": ToggleSlidesPlaying(); break;
				case "SetSlidesQuery": SlidesQuery = message.GetString(); SlidesSize = message.GetString(); break;
				case "SetSlideDisplayTime": SlideDisplayTime = message.GetInt(); break;
				case "CycleSlide": CycleSlide(message.GetBool() ? 1 : -1); break;
				case "DownloadURL": DownloadURL(message.GetString()); break;
				case "EditTags": EditTags(message.GetEditTags()); break;
				case "DeleteVideos": DeleteVideos(message.GetInts()); break;
				default: throw new Exception("Invalid command");
			}
		}

		async Task<string> ResolveShortcut(string name)
		{
			var result = await Database.GetAsync<Shortcut>($"{nameof(Shortcut.Name)} = @Name", new Dictionary<string, object> { ["@Name"] = name });
			name = result.Select(shortcut => shortcut.Value).FirstOrDefault() ?? name;
			return name;
		}

		async void DownloadURL(string url)
		{
			url = await ResolveShortcut(url);
			VideoFileDownloader.DownloadAsync(url, (id, downloadData) =>
			{
				Dispatcher.Invoke(() =>
				{
					if (downloadData == null)
						downloads.Remove(id);
					else
						downloads[id] = downloadData;
					status.Downloads = downloads.Values.ToList();
				});
			}, UpdateVideoFiles);
		}

		async void EditTags(EditTags editTags)
		{
			var tags = await Database.GetAsync<Tag>();
			tags.AddRange(editTags.Tags.Keys.Except(tags.Select(tag => tag.Name), StringComparer.OrdinalIgnoreCase).Select(name => new Tag { Name = name }));
			tags.Where(tag => tag.TagID == 0).ForEach(async tag => await Database.AddOrUpdateAsync(tag));
			var tagIDs = tags.ToDictionary(tag => tag.Name, tag => tag.TagID, StringComparer.OrdinalIgnoreCase);

			var tagValues = (await Database.GetAsync<TagValue>()).GroupBy(tagValue => tagValue.VideoFileID).ToDictionary(group => group.Key, group => group.ToDictionary(tagValue => tagValue.TagID));
			foreach (var videoFileID in editTags.VideoFileIDs)
				foreach (var tag in editTags.Tags)
				{
					var tagID = tagIDs[tag.Key];

					TagValue tagValue = null;
					if ((tagValues.ContainsKey(videoFileID)) && (tagValues[videoFileID].ContainsKey(tagID)))
						tagValue = tagValues[videoFileID][tagID];
					else
						tagValue = new TagValue { VideoFileID = videoFileID, TagID = tagID };

					if (string.Equals(tagValue.Value, tag.Value, StringComparison.OrdinalIgnoreCase))
						continue;

					if (!string.IsNullOrWhiteSpace(tag.Value))
					{
						tagValue.Value = string.IsNullOrWhiteSpace(tag.Value) ? null : tag.Value;
						await Database.AddOrUpdateAsync(tagValue);
					}
					else if (tagValue.TagValueID != 0)
						await Database.DeleteAsync(tagValue);
				}

			await Database.ExecuteNonQueryAsync("DELETE FROM Tag WHERE TagID NOT IN (SELECT TagID FROM TagValue)");

			UpdateVideoFiles();
		}

		async void DeleteVideos(List<int> videoFileIDs)
		{
			var videoFiles = await Database.GetVideoFilesAsync(videoFileIDs);
			foreach (var videoFile in videoFiles)
			{
				await Database.DeleteAsync(videoFile);
				File.Delete(Path.Combine(Settings.VideosPath, videoFile.FileName));
			}
			UpdateVideoFiles();
		}

		void SetVolume(int volume, bool relative) => Volume = (relative ? Volume : 0) + volume;

		void QueueVideos(List<int> videoFileIDs, bool top)
		{
			var queueDict = queue.ToDictionary(videoFile => videoFile.VideoFileID);

			if ((top) && (VideoState != null))
				videoFileIDs.Remove(CurrentVideo.VideoFileID);

			var positions = Enumerable.Range(0, videoFileIDs.Count).ToDictionary(index => videoFileIDs[index], index => index);
			var videoFiles = Database.GetVideoFilesAsync(videoFileIDs).Result.OrderBy(videoFile => positions[videoFile.VideoFileID]).ToList();

			if ((top) || (videoFiles.Any(videoFile => queueDict.ContainsKey(videoFile.VideoFileID))))
			{
				var oldFirst = queue.FirstOrDefault();
				videoFiles.Where(videoFile => queueDict.ContainsKey(videoFile.VideoFileID)).ForEach(videoFile => queue.Remove(queueDict[videoFile.VideoFileID]));
				if (oldFirst != queue.FirstOrDefault())
					VideoState = null;
				if (!top)
					return;
			}

			var addIndex = queue.Count;
			if (top)
				addIndex = VideoState == null ? 0 : 1;

			foreach (var videoFile in videoFiles)
				queue.Insert(addIndex++, videoFile);
		}

		public enum MediaState
		{
			Pause = 0,
			Play = 1,
			AllPlay = 2,
		}

		string slidesQueryField;
		public string SlidesQuery
		{
			get => slidesQueryField;
			set
			{
				slidesQueryField = value;
				if (slidesQueryField.StartsWith("saved:"))
					slidesQueryField = ResolveShortcut(slidesQueryField).Result;
				slidesQueryField = Regex.Replace(slidesQueryField, @"[\r,]", "\n");
				slidesQueryField = Regex.Replace(slidesQueryField, @"[^\S\n]+", " ");
				slidesQueryField = Regex.Replace(slidesQueryField, @"(^ | $)", "", RegexOptions.Multiline);
				slidesQueryField = Regex.Replace(slidesQueryField, @"\n+", "\n");
				slidesQueryField = Regex.Replace(slidesQueryField, @"(^\n|\n$)", "");
				slidesQueryField = GetTumblrInfo(slidesQueryField);
				if (!slidesQueryField.StartsWith("tumblr:", StringComparison.OrdinalIgnoreCase))
					slidesQueryField = slidesQueryField?.ToLowerInvariant() ?? "";
				status.SlidesQuery = slidesQueryField;
				updateState.Signal();
			}
		}

		string slidesSizeField;
		public string SlidesSize
		{
			get => slidesSizeField;
			set
			{
				slidesSizeField = status.SlidesSize = value;
				updateState.Signal();
			}
		}

		int slideDisplayTimeField;
		public int SlideDisplayTime
		{
			get => slideDisplayTimeField;
			set => slideDisplayTimeField = status.SlideDisplayTime = value;
		}

		bool slidesPlayingField;
		public bool SlidesPlaying
		{
			get => slidesPlayingField;
			set => slidesPlayingField = status.SlidesPlaying = value;
		}

		readonly List<int> history = new List<int>();
		readonly ObservableCollection<string> slides = new ObservableCollection<string>();
		readonly ObservableCollection<MusicFile> music = new ObservableCollection<MusicFile>();
		readonly ObservableCollection<VideoFile> queue = new ObservableCollection<VideoFile>();

		MediaState? videoStateField;
		MediaState? VideoState
		{
			get => videoStateField;
			set
			{
				videoStateField = value;
				UpdateMediaPlaying();
			}
		}
		MediaState? musicStateField;
		MediaState? MusicState
		{
			get => musicStateField;
			set
			{
				musicStateField = value;
				UpdateMediaPlaying();
			}
		}

		void UpdateMediaPlaying() => status.MediaPlaying = VideoState ?? MusicState ?? MediaState.Pause;

		int currentSlideIndex = 0;
		public string CurrentSlide => slides.Any() ? slides[currentSlideIndex] : null;
		public MusicFile CurrentMusic => music.FirstOrDefault();
		public VideoFile CurrentVideo => queue.FirstOrDefault();

		public int Volume
		{
			get => (int)(mediaPlayer.Volume * 100);
			set
			{
				mediaPlayer.Volume = Math.Max(0, Math.Min(value / 100.0, 1));
				status.MediaVolume = value;
			}
		}

		public void AddSlide(string fileName)
		{
			slides.Add(fileName);
			updateState.Signal();
		}

		public void CycleSlide(int offset = 1)
		{
			currentSlideIndex += offset;
			updateState.Signal();
		}

		public void ClearSlides()
		{
			slides.Clear();
			currentSlideIndex = 0;
			updateState.Signal();
		}

		string GetTumblrInfo(string query)
		{
			if (!query.StartsWith("tumblr:", StringComparison.OrdinalIgnoreCase))
				return query;

			var nonTumblrQuery = "tumblr " + query.Remove(0, "tumblr:".Length);
			var parts = query.Split(':').ToList();
			if (parts.Count != 3)
				return nonTumblrQuery;
			parts[0] = parts[0].ToLowerInvariant();
			parts[1] = parts[1].ToLowerInvariant();
			if (!parts[2].StartsWith("#"))
				parts[2] = $"#{Cryptor.Encrypt(parts[2])}";
			return string.Join(":", parts);
		}

		void SavePreviousImage()
		{
			var previousBitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Default);
			previousBitmap.Render(visual);
			previous.Source = previousBitmap;

			StopUIElementFade();
			slide.Opacity = media.Opacity = 0;
		}

		DoubleAnimation fadeAnimation;
		UIElement fadeControl;
		bool FadeInUIElement(UIElement element)
		{
			// Check if already faded in
			if (element.Opacity == 1)
				return true;

			// Check if currently fading in
			if ((fadeAnimation != null) && (fadeControl == element))
				return false;

			fadeAnimation = new DoubleAnimation(1, new Duration(TimeSpan.FromSeconds(1)));
			fadeAnimation.Completed += StopUIElementFade;
			fadeControl = element;
			fadeControl.BeginAnimation(OpacityProperty, fadeAnimation);
			return false;
		}

		void StopUIElementFade(object sender = null, EventArgs e = null)
		{
			if (fadeAnimation == null)
				return;

			fadeAnimation.Completed -= StopUIElementFade;
			fadeControl.BeginAnimation(OpacityProperty, null);
			fadeControl.Opacity = 1;
			fadeAnimation = null;
			fadeControl = null;
			updateState.Signal();
		}

		DateTime? slideTime = null;
		string previousSlide;
		MusicFile previousMusic;
		VideoFile previousVideo;
		void UpdateState()
		{
			SetupSlideDownloader();

			ValidateState();

			if ((previousSlide != null) && (VideoState != null))
			{
				previousSlide = null;
				slideTime = null;
			}

			if (((MusicState == null) && (previousMusic != null)) || ((VideoState == null) && (previousVideo != null)))
			{
				mediaPlayer.Stop();
				mediaPlayer.Source = null;
				previousMusic = null;
				previousVideo = null;
				status.MediaTitle = "";
			}

			if (VideoState != null)
			{
				if (previousVideo != CurrentVideo)
				{
					previousVideo = CurrentVideo;
					status.MediaTitle = previousVideo.Title;
					mediaPlayer.Source = new Uri(Path.Combine(Settings.VideosPath, previousVideo.FileName));
					mediaPlayer.Pause();
				}

				if (media.Opacity != 1)
				{
					if (fadeAnimation == null)
					{
						SavePreviousImage();
						FadeInUIElement(media);
					}
					return;
				}

				switch (VideoState)
				{
					case MediaState.Pause: mediaPlayer.Pause(); break;
					case MediaState.Play: mediaPlayer.Play(); break;
					case MediaState.AllPlay: mediaPlayer.Play(); break;
				}
				return;
			}

			if (CurrentSlide != previousSlide)
			{
				SavePreviousImage();

				previousSlide = CurrentSlide;
				slideImage.Source = null;
				slideTime = null;
				if (previousSlide != null)
				{
					var slideBitmap = new BitmapImage();
					slideBitmap.BeginInit();
					slideBitmap.UriSource = new Uri(previousSlide);
					slideBitmap.CacheOption = BitmapCacheOption.OnLoad;
					slideBitmap.EndInit();

					// Set DPI to 96 (incorrect setting causes picture not to display properly)
					var stride = slideBitmap.PixelWidth * slideBitmap.Format.BitsPerPixel;
					var pixelData = new byte[stride * slideBitmap.PixelHeight];
					slideBitmap.CopyPixels(pixelData, stride, 0);

					slideImage.Source = BitmapSource.Create(slideBitmap.PixelWidth, slideBitmap.PixelHeight, 72, 72, slideBitmap.Format, slideBitmap.Palette, pixelData, stride);
					slideTime = DateTime.Now;
				}
				FadeInUIElement(slide);
			}

			status.MediaTitle = CurrentVideo?.Title ?? CurrentMusic?.Title ?? "";
			if (MusicState != null)
			{
				if (previousMusic != CurrentMusic)
				{
					previousMusic = CurrentMusic;
					mediaPlayer.Source = new Uri(previousMusic.FileName);
				}
				status.MediaTitle = previousMusic.Title;
				switch (MusicState)
				{
					case MediaState.Play: mediaPlayer.Play(); break;
					case MediaState.Pause: mediaPlayer.Pause(); break;
				}
			}
		}

		void ValidateState()
		{
			if (queue.Count == 0)
				VideoState = null;
			if (music.Count == 0)
				MusicState = null;
			if (slides.Count == 0)
				currentSlideIndex = 0;
			else
			{
				while (currentSlideIndex < 0)
					currentSlideIndex += slides.Count;
				while (currentSlideIndex >= slides.Count)
					currentSlideIndex -= slides.Count;
			}

			if (VideoState != null)
				MusicState = null;
		}

		string currentSlidesQuery;
		string currentSlidesSize;
		CancellationTokenSource tokenSource;
		void SetupSlideDownloader()
		{
			if ((currentSlidesQuery == SlidesQuery) && (currentSlidesSize == SlidesSize))
				return;

			if (tokenSource != null)
				tokenSource.Cancel();
			ClearSlides();
			currentSlidesQuery = SlidesQuery;
			currentSlidesSize = SlidesSize;
			tokenSource = new CancellationTokenSource();

			if (currentSlidesQuery.StartsWith("tumblr:"))
			{
				var parts = currentSlidesQuery.Split(':');
				TumblrSlideSource.Run(parts[1], Cryptor.Decrypt(parts[2].Substring(1)), fileName => AddSlide(fileName), tokenSource.Token);
			}
			else if (currentSlidesQuery.StartsWith("dir:"))
			{
				var random = new Random();
				Directory.EnumerateFiles(currentSlidesQuery.Substring("dir:".Length)).OrderBy(x => random.Next()).ForEach(file => AddSlide(file));
			}
			else
				GoogleSlideSource.Run(currentSlidesQuery, currentSlidesSize, fileName => AddSlide(fileName), tokenSource.Token);
		}

		void CheckCycleSlide()
		{
			status.MediaPosition = (int)mediaPlayer.Position.TotalSeconds;
			status.MediaMaxPosition = mediaPlayer.NaturalDuration.HasTimeSpan ? (int)mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds : 0;

			if ((slideTime == null) || (!SlidesPlaying))
				return;
			if ((DateTime.Now - slideTime.Value).TotalSeconds >= SlideDisplayTime)
				CycleSlide();
		}

		void ToggleSlidesPlaying() => SlidesPlaying = !SlidesPlaying;

		public void ToggleMediaPlaying(bool allPlay = false)
		{
			if ((MusicState == null) && (queue.Any()))
				VideoState = allPlay ? MediaState.AllPlay : VideoState == MediaState.AllPlay ? MediaState.Play : (VideoState ?? MediaState.Pause) != MediaState.Pause ? MediaState.Pause : MediaState.Play;
			else
				MusicState = allPlay ? MusicState : (MusicState ?? MediaState.Pause) != MediaState.Pause ? MediaState.Pause : MediaState.Play;

			updateState.Signal();
		}

		public void MediaForward()
		{
			if ((MusicState != null) || (!queue.Any()))
			{
				if (music.Any())
				{
					music.Add(music[0]);
					music.RemoveAt(0);
				}
				if (queue.Any())
					MusicState = null;
			}
			else
			{
				if (queue.Any())
				{
					queue.RemoveAt(0);
					if (VideoState != MediaState.AllPlay)
						VideoState = null;
				}

			}
		}

		void SetExternalDisplay() => Process.Start("DisplaySwitch", "/external");

		public void SetPosition(int position, bool relative)
		{
			mediaPlayer.Position = TimeSpan.FromSeconds((relative ? mediaPlayer.Position.TotalSeconds : 0) + position);
			status.MediaPosition = (int)mediaPlayer.Position.TotalSeconds;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			e.Handled = true;
			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
			{
				switch (e.Key)
				{
					case Key.S: SettingsDialog.Run(this); break;
					case Key.N: WLAN.Start("NeoPlayer", "NeoPlayer"); break;
					case Key.U: VideoFileDownloader.Update(); break;
					case Key.Space: ToggleMediaPlaying(); break;
					case Key.Right: MediaForward(); break;
					case Key.Enter: SetExternalDisplay(); break;
					default: e.Handled = false; break;
				}
			}
			else
			{
				switch (e.Key)
				{
					case Key.Space: ToggleSlidesPlaying(); break;
					case Key.Right: CycleSlide(1); break;
					case Key.Down: Volume -= 5; break;
					case Key.Up: Volume += 5; break;
					case Key.Left: CycleSlide(-1); break;
					default: e.Handled = false; break;
				}
			}
			base.OnKeyDown(e);
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Database.Close();
			Environment.Exit(0);
		}

		static class Win32
		{
			// Import SetThreadExecutionState Win32 API and necessary flags
			[DllImport("kernel32.dll")]
			public static extern uint SetThreadExecutionState(uint esFlags);

			public const uint ES_CONTINUOUS = 0x80000000;
			public const uint ES_SYSTEM_REQUIRED = 0x00000001;
			public const uint ES_DISPLAY_REQUIRED = 0x00000002;
		}
	}
}
