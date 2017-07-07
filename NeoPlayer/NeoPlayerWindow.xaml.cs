using System;
using System.Collections.Generic;
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

namespace NeoPlayer
{
	partial class NeoPlayerWindow
	{
		public static NeoPlayerWindow Current { get; private set; }

		readonly SingleRunner mediaDataUpdate, updateState;
		readonly DispatcherTimer changeSlideTimer = null;

		public NeoPlayerWindow()
		{
			mediaDataUpdate = new SingleRunner(MediaDataUpdate);
			updateState = new SingleRunner(UpdateState);

			Current = this;

			InitializeComponent();

			// Keep screen/computer on
			Win32.SetThreadExecutionState(Win32.ES_CONTINUOUS | Win32.ES_DISPLAY_REQUIRED | Win32.ES_SYSTEM_REQUIRED);

			var random = new Random();
			Directory.EnumerateFiles(Settings.MusicPath).OrderBy(x => random.Next()).ForEach(fileName => AddMusic(new MediaData { Description = Path.GetFileNameWithoutExtension(fileName), URL = $"file:///{fileName}" }));

			NetServer.Run(7399);

			mediaPlayer.MediaEnded += (s, e) => Forward();
			mediaPlayer.Volume = .5;
			System.Windows.Forms.Cursor.Hide();

			changeSlideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.25) };
			changeSlideTimer.Tick += (s, e) => CheckCycleSlide();
			changeSlideTimer.Start();

			updateState.Signal();
		}

		enum MediaState
		{
			None,
			Pause,
			Play,
		}

		string slidesQuery = Settings.Debug ? "test" : "landscape";
		public string SlidesQuery
		{
			get => slidesQuery;
			set
			{
				slidesQuery = value;
				slidesQuery = Regex.Replace(slidesQuery, @"[\r,]", "\n");
				slidesQuery = Regex.Replace(slidesQuery, @"[^\S\n]+", " ");
				slidesQuery = Regex.Replace(slidesQuery, @"(^ | $)", "", RegexOptions.Multiline);
				slidesQuery = Regex.Replace(slidesQuery, @"\n+", "\n");
				slidesQuery = Regex.Replace(slidesQuery, @"(^\n|\n$)", "");
				slidesQuery = GetTumblrInfo(slidesQuery);
				if (!slidesQuery.StartsWith("tumblr:", StringComparison.OrdinalIgnoreCase))
					slidesQuery = slidesQuery?.ToLowerInvariant() ?? "";
				NetServer.SendAll(NetServer.GetSlidesData());
				updateState.Signal();
			}
		}

		string slidesSize = "2mp";
		public string SlidesSize
		{
			get => slidesSize;
			set
			{
				slidesSize = value;
				updateState.Signal();
				NetServer.SendAll(NetServer.GetSlidesData());
			}
		}

		int slideDisplayTime = 60;
		public int SlideDisplayTime
		{
			get => slideDisplayTime;
			set
			{
				slideDisplayTime = value;
				NetServer.SendAll(NetServer.GetSlidesData());
			}
		}
		bool slidesPaused;
		public bool SlidesPaused
		{
			get => slidesPaused;
			set
			{
				slidesPaused = value;
				NetServer.SendAll(NetServer.GetSlidesData());
			}
		}

		readonly List<string> slides = new List<string>();
		readonly List<MediaData> music = new List<MediaData>();
		readonly List<MediaData> videos = new List<MediaData>();
		MediaState videoState = MediaState.None;
		MediaState musicState = MediaState.None;

		int currentSlideIndex = 0;
		public string CurrentSlide => slides.Any() ? slides[currentSlideIndex] : null;
		public MediaData CurrentMusic => music.FirstOrDefault();
		public MediaData CurrentVideo => videos.FirstOrDefault();

		public IEnumerable<MediaData> QueueVideos => videos;

		public int Volume
		{
			get => (int)(mediaPlayer.Volume * 100);
			set
			{
				mediaPlayer.Volume = Math.Max(0, Math.Min(value / 100.0, 1));
				NetServer.SendAll(NetServer.GetVolume());
			}
		}

		public void AddSlide(string fileName)
		{
			slides.Add(fileName);
			updateState.Signal();
		}

		public void AddMusic(MediaData musicData)
		{
			music.Add(musicData);
			updateState.Signal();
		}

		public void ToggleVideo(MediaData videoData)
		{
			var match = videos.IndexOf(video => video.URL == videoData.URL).DefaultIfEmpty(-1).First();
			if (match == -1)
				videos.Add(videoData);
			else
			{
				videos.RemoveAt(match);
				if (match == 0)
					videoState = MediaState.None;
			}
			NetServer.SendAll(NetServer.GetQueue());
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
		MediaData previousMusic, previousVideo, currentMedia;
		void UpdateState()
		{
			SetupSlideDownloader();

			ValidateState();

			if ((previousSlide != null) && (videoState != MediaState.None))
			{
				previousSlide = null;
				slideTime = null;
			}

			if (((musicState == MediaState.None) && (previousMusic != null)) || ((videoState == MediaState.None) && (previousVideo != null)))
			{
				mediaPlayer.Stop();
				mediaPlayer.Source = null;
				previousMusic = previousVideo = currentMedia = null;
			}

			if (videoState != MediaState.None)
			{
				if (media.Opacity != 1)
				{
					if (fadeAnimation == null)
					{
						SavePreviousImage();
						FadeInUIElement(media);
					}
					return;
				}

				if (previousVideo != CurrentVideo)
				{
					previousVideo = currentMedia = CurrentVideo;
					mediaPlayer.Source = new Uri(previousVideo.URL);
				}

				switch (videoState)
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
					slideImage.Source = slideBitmap;
					slideTime = DateTime.Now;
				}
				FadeInUIElement(slide);
			}

			currentMedia = CurrentVideo ?? CurrentMusic;
			if (musicState != MediaState.None)
			{
				if (previousMusic != CurrentMusic)
				{
					previousMusic = CurrentMusic;
					mediaPlayer.Source = new Uri(previousMusic.URL);
				}
				currentMedia = previousMusic;
				switch (musicState)
				{
					case MediaState.Play: mediaPlayer.Play(); break;
					case MediaState.Pause: mediaPlayer.Pause(); break;
				}
			}
		}

		void ValidateState()
		{
			if (videos.Count == 0)
				videoState = MediaState.None;
			if (music.Count == 0)
				musicState = MediaState.None;
			if (slides.Count == 0)
				currentSlideIndex = 0;
			else
			{
				while (currentSlideIndex < 0)
					currentSlideIndex += slides.Count;
				while (currentSlideIndex >= slides.Count)
					currentSlideIndex -= slides.Count;
			}

			if (videoState != MediaState.None)
				musicState = MediaState.None;
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
			else
				GoogleSlideSource.Run(currentSlidesQuery, currentSlidesSize, fileName => AddSlide(fileName), tokenSource.Token);
		}

		public void MediaDataUpdate()
		{
			var playing = (videoState == MediaState.Play) || (musicState == MediaState.Play);
			var title = currentMedia?.Description ?? "";
			var maxPosition = mediaPlayer.NaturalDuration.HasTimeSpan ? (int)mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds : 0;
			var position = (int)mediaPlayer.Position.TotalSeconds;
			NetServer.SendAll(NetServer.MediaData(playing, title, position, maxPosition));
		}

		void CheckCycleSlide()
		{
			MediaDataUpdate();
			if ((slideTime == null) || (SlidesPaused))
				return;
			if ((DateTime.Now - slideTime.Value).TotalSeconds >= SlideDisplayTime)
				CycleSlide();
		}

		void ToggleSlidesPaused()
		{
			SlidesPaused = !SlidesPaused;
		}

		void SetSlideDisplayTime(int displayTime)
		{
			SlideDisplayTime = displayTime;
		}

		public void Play()
		{
			if ((musicState == MediaState.None) && (videos.Any()))
				videoState = videoState == MediaState.Play ? MediaState.Pause : MediaState.Play;
			else
				musicState = musicState == MediaState.Play ? MediaState.Pause : MediaState.Play;

			updateState.Signal();
		}

		public void Forward()
		{
			updateState.Signal();
			if ((musicState != MediaState.None) || (!videos.Any()))
			{
				if (music.Any())
				{
					music.Add(music[0]);
					music.RemoveAt(0);
				}
				if (videos.Any())
					musicState = MediaState.None;
			}
			else
			{
				if (videos.Any())
				{
					videos.RemoveAt(0);
					videoState = MediaState.None;
					NetServer.SendAll(NetServer.GetQueue());
				}

			}

		}

		public void SetPosition(int position, bool relative)
		{
			mediaPlayer.Position = TimeSpan.FromSeconds((relative ? mediaPlayer.Position.TotalSeconds : 0) + position);
			MediaDataUpdate();
		}

		void SetSlidesQuery(string slidesQuery, string slidesSize)
		{
			SlidesQuery = slidesQuery;
			SlidesSize = slidesSize;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if ((e.Key == Key.S) && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
			{
				new SettingsDialog().ShowDialog();
				e.Handled = true;
			}
			if (e.Key == Key.Space)
			{
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
					Play();
				else
					ToggleSlidesPaused();
			}
			if (e.Key == Key.Right)
			{
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
					Forward();
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
			base.OnKeyDown(e);
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
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
