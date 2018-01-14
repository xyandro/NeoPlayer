using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using NeoPlayer.Models;

namespace NeoPlayer
{
	partial class NeoPlayerWindow
	{
		static readonly string SavedQueriesFile = Path.Combine(Path.GetDirectoryName(typeof(NeoPlayerWindow).Assembly.Location), "Saved.xml");

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
			queue.CollectionChanged += (s, e) => { status.Queue = queue.ToList(); updateState.Signal(); };

			InitializeComponent();

			// Keep screen/computer on
			Win32.SetThreadExecutionState(Win32.ES_CONTINUOUS | Win32.ES_DISPLAY_REQUIRED | Win32.ES_SYSTEM_REQUIRED);

			var random = new Random();
			Directory.EnumerateFiles(Settings.MusicPath).OrderBy(x => random.Next()).Select(fileName => new MusicFile { FileName = fileName, Title = Path.GetFileNameWithoutExtension(fileName) }).ForEach(file => music.Add(file));

			SlidesQuery = Helpers.Debug ? "test" : "landscape";
			SlidesSize = "2mp";
			SlideDisplayTime = 60;
			SlidesPlaying = true;
			VideoState = MediaState.None;
			MusicState = MediaState.None;
			Volume = 50;

			mediaPlayer.MediaEnded += (s, e) => MediaForward();
			mediaPlayer.MediaFailed += (s, e) => MediaForward();

			changeSlideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.25) };
			changeSlideTimer.Tick += (s, e) => CheckCycleSlide();
			changeSlideTimer.Start();

			status.Queue = new List<VideoFile>();
			UpdateCool();
			status.Downloads = new List<DownloadData>();
		}

		void UpdateCool() => status.Cool = Database.GetAsync<VideoFile>().Result;

		void OnConnect(AsyncQueue<byte[]> queue) => status.SendAll(queue);

		void OnMessage(Message message, AsyncQueue<byte[]> queue)
		{
			var command = message.GetString();
			switch (command)
			{
				case "QueueVideo": QueueVideo(message.GetInt(), message.GetBool()); break;
				case "SetPosition": SetPosition(message.GetInt(), message.GetBool()); break;
				case "ToggleMediaPlaying": ToggleMediaPlaying(); break;
				case "SetVolume": SetVolume(message.GetInt(), message.GetBool()); break;
				case "MediaForward": MediaForward(); break;
				case "ToggleSlidesPlaying": ToggleSlidesPlaying(); break;
				case "SetSlidesQuery": SlidesQuery = message.GetString(); SlidesSize = message.GetString(); break;
				case "SetSlideDisplayTime": SlideDisplayTime = message.GetInt(); break;
				case "CycleSlide": CycleSlide(message.GetBool() ? 1 : -1); break;
				case "DownloadURL": DownloadURL(message.GetString()); break;
				default: throw new Exception("Invalid command");
			}
		}

		async void DownloadURL(string url)
		{
			var result = await Database.GetAsync<Shortcut>($"{nameof(Shortcut.Name)} = @Name", new Dictionary<string, object> { ["@Name"] = url });
			url = result.Select(shortcut => shortcut.Value).FirstOrDefault() ?? url;
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
			}, UpdateCool);
		}

		void SetVolume(int volume, bool relative) => Volume = (relative ? Volume : 0) + volume;

		void QueueVideo(int videoFileID, bool top)
		{
			var videoFile = Database.GetAsync<VideoFile>(videoFileID).Result;
			if (videoFile == null)
				return;

			var topIndex = VideoState == MediaState.None ? 0 : 1;
			var match = queue.IndexOf(video => video.VideoFileID == videoFileID).DefaultIfEmpty(-1).First();
			if (match == -1)
				queue.Insert(top ? topIndex : queue.Count, videoFile);
			else if (top)
				queue.Move(match, topIndex);
			else
			{
				queue.RemoveAt(match);
				if (match == 0)
					VideoState = MediaState.None;
			}
		}

		enum MediaState
		{
			None,
			Pause,
			Play,
		}

		string slidesQueryField;
		public string SlidesQuery
		{
			get => slidesQueryField;
			set
			{
				slidesQueryField = value;
				if (slidesQueryField.StartsWith("saved:"))
					slidesQueryField = GetSavedQuery(slidesQueryField.Substring("saved:".Length).Trim().ToLowerInvariant());
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

		string GetSavedQuery(string query)
		{
			try
			{
				var xml = XElement.Load(SavedQueriesFile);
				var found = xml.Elements().FirstOrDefault(element => element.Attribute("Name").Value == query);
				if (found != null)
					query = found.Value;
			}
			catch { }
			return query;
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

		readonly ObservableCollection<string> slides = new ObservableCollection<string>();
		readonly ObservableCollection<MusicFile> music = new ObservableCollection<MusicFile>();
		readonly ObservableCollection<VideoFile> queue = new ObservableCollection<VideoFile>();

		MediaState videoStateField;
		MediaState VideoState
		{
			get => videoStateField;
			set
			{
				videoStateField = value;
				status.MediaPlaying = (VideoState == MediaState.Play) || (MusicState == MediaState.Play);
			}
		}
		MediaState musicStateField;
		MediaState MusicState
		{
			get => musicStateField;
			set
			{
				musicStateField = value;
				status.MediaPlaying = (VideoState == MediaState.Play) || (MusicState == MediaState.Play);
			}
		}

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

			if ((previousSlide != null) && (VideoState != MediaState.None))
			{
				previousSlide = null;
				slideTime = null;
			}

			if (((MusicState == MediaState.None) && (previousMusic != null)) || ((VideoState == MediaState.None) && (previousVideo != null)))
			{
				mediaPlayer.Stop();
				mediaPlayer.Source = null;
				previousMusic = null;
				previousVideo = null;
				status.MediaTitle = "";
			}

			if (VideoState != MediaState.None)
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
					case MediaState.Play: mediaPlayer.Play(); break;
					case MediaState.Pause: mediaPlayer.Pause(); break;
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
			if (MusicState != MediaState.None)
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
				VideoState = MediaState.None;
			if (music.Count == 0)
				MusicState = MediaState.None;
			if (slides.Count == 0)
				currentSlideIndex = 0;
			else
			{
				while (currentSlideIndex < 0)
					currentSlideIndex += slides.Count;
				while (currentSlideIndex >= slides.Count)
					currentSlideIndex -= slides.Count;
			}

			if (VideoState != MediaState.None)
				MusicState = MediaState.None;
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

		public void ToggleMediaPlaying()
		{
			if ((MusicState == MediaState.None) && (queue.Any()))
				VideoState = VideoState == MediaState.Play ? MediaState.Pause : MediaState.Play;
			else
				MusicState = MusicState == MediaState.Play ? MediaState.Pause : MediaState.Play;

			updateState.Signal();
		}

		public void MediaForward()
		{
			if ((MusicState != MediaState.None) || (!queue.Any()))
			{
				if (music.Any())
				{
					music.Add(music[0]);
					music.RemoveAt(0);
				}
				if (queue.Any())
					MusicState = MediaState.None;
			}
			else
			{
				if (queue.Any())
				{
					queue.RemoveAt(0);
					VideoState = MediaState.None;
				}

			}

		}

		public void SetPosition(int position, bool relative)
		{
			mediaPlayer.Position = TimeSpan.FromSeconds((relative ? mediaPlayer.Position.TotalSeconds : 0) + position);
			status.MediaPosition = (int)mediaPlayer.Position.TotalSeconds;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if ((e.Key == Key.D) && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
			{
				ShortcutsDialog.Run();
				e.Handled = true;
			}
			if ((e.Key == Key.S) && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
			{
				new SettingsDialog().ShowDialog();
				e.Handled = true;
			}
			if (e.Key == Key.Space)
			{
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
					ToggleMediaPlaying();
				else
					ToggleSlidesPlaying();
			}
			if (e.Key == Key.Right)
			{
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
					MediaForward();
				else
					CycleSlide(1);
			}
			if (e.Key == Key.Down)
				Volume -= 5;
			if (e.Key == Key.Up)
				Volume += 5;
			if (e.Key == Key.Left)
				CycleSlide(-1);
			if (e.Key == Key.Q)
				new QueryDialog(this).ShowDialog();
			if (e.Key == Key.N)
				WLAN.Start("NeoPlayer", "NeoPlayer");
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
