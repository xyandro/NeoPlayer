using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NeoPlayer
{
	partial class NeoPlayerWindow
	{
		public static NeoPlayerWindow Current { get; private set; }

		public enum ActionType
		{
			Slideshow,
			Videos,
		}

		ActionType currentAction = ActionType.Slideshow;
		public ActionType CurrentAction { get { return currentAction; } set { currentAction = value; ActionChanged(); } }

		string slidesQuery = Settings.Debug ? "test" : "landscape";
		public string SlidesQuery
		{
			get { return slidesQuery; }
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
				ActionChanged();
			}
		}

		string slidesSize = "2mp";
		public string SlidesSize { get { return slidesSize; } set { slidesSize = value; ActionChanged(); } }

		public int SlideDisplayTime { get; set; } = 60;
		public bool SlidesPaused { get; set; }
		public bool musicAutoPlay { get; set; } = false;
		public bool MusicAutoPlay { get { return musicAutoPlay; } set { musicAutoPlay = value; ActionChanged(); } }

		readonly List<string> slides = new List<string>();
		readonly List<MediaData> music = new List<MediaData>();
		readonly List<MediaData> videos = new List<MediaData>
		{
			new MediaData("Randon", "http://randon.com"),
			new MediaData("Ben", "http://ben.com"),
			new MediaData("Sophia", "http://sophia.com"),
			new MediaData("Timothy", "http://timothy.com"),
			new MediaData("Katelyn", "http://katelyn.com"),
			new MediaData("Phoebe", "http://phoebe.com"),
			new MediaData("Megan", "http://megan.com"),
		};

		int currentSlideIndex = 0;
		public string CurrentSlide => slides.Any() ? slides[currentSlideIndex % slides.Count] : null;
		public MediaData CurrentMusic => music.FirstOrDefault();
		public MediaData CurrentVideo => videos.FirstOrDefault();

		public IEnumerable<MediaData> QueuedVideos => videos;

		public bool VideoIsQueued(string url) => videos.Any(video => video.URL == url);

		void EnqueueItems(List<string> list, IEnumerable<string> items, bool enqueue)
		{
			var found = false;
			foreach (var fileName in items)
			{
				var present = list.Contains(fileName);
				if (present == enqueue)
					continue;

				if (enqueue)
					list.Add(fileName);
				else
					list.Remove(fileName);
				found = true;
			}
			if (found)
				ActionChanged();
		}

		public void EnqueueSlides(IEnumerable<string> fileNames, bool enqueue = true) => EnqueueItems(slides, fileNames, enqueue);
		public void EnqueueMusic(MediaData musicData)
		{
			music.Add(musicData);
			ActionChanged();
		}
		public void EnqueueVideo(MediaData videoData)
		{
			videos.Add(videoData);
			ActionChanged();
		}

		public void CycleSlide(bool fromStart = true)
		{
			if (!slides.Any())
				return;

			currentSlideIndex = Math.Max(0, Math.Min(currentSlideIndex, slides.Count - 1));
			currentSlideIndex += (fromStart ? 1 : -1);
			while (currentSlideIndex < 0)
				currentSlideIndex += slides.Count;
			while (currentSlideIndex >= slides.Count)
				currentSlideIndex -= slides.Count;
			ActionChanged();
		}

		public void CycleMusic()
		{
			if (!music.Any())
				return;

			music.Add(music[0]);
			music.RemoveAt(0);

			ActionChanged();
		}

		public void CycleVideo()
		{
			if (!videos.Any())
				return;

			videos.RemoveAt(0);
			ActionChanged();
		}

		public void ClearSlides()
		{
			if (!slides.Any())
				return;

			slides.Clear();
			currentSlideIndex = 0;
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

		readonly DispatcherTimer changeSlideTimer = null;

		public NeoPlayerWindow()
		{
			Current = this;

			InitializeComponent();

			// Keep screen/computer on
			Win32.SetThreadExecutionState(Win32.ES_CONTINUOUS | Win32.ES_DISPLAY_REQUIRED | Win32.ES_SYSTEM_REQUIRED);

			var random = new Random();
			Directory.EnumerateFiles(Settings.MusicPath).OrderBy(x => random.Next()).ForEach(fileName => EnqueueMusic(new MediaData(Path.GetFileNameWithoutExtension(fileName), $"file:///{fileName}")));

			NetServer.Run(7399);

			vlc.AutoPlay = vlc.Toolbar = vlc.Branding = false;
			vlc.MediaPlayerEndReached += (s, e) => Next();
			System.Windows.Forms.Cursor.Hide();
			Loaded += (s, e) => WindowState = WindowState.Maximized;

			changeSlideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.25) };
			changeSlideTimer.Tick += (s, e) => CheckCycleSlide();
			changeSlideTimer.Start();
		}

		DispatcherTimer actionChangedTimer = null;
		void ActionChanged()
		{
			if (actionChangedTimer != null)
				return;

			actionChangedTimer = new DispatcherTimer();
			actionChangedTimer.Tick += (s, e) =>
			{
				actionChangedTimer.Stop();
				actionChangedTimer = null;
				HandleActions();
			};
			actionChangedTimer.Start();
		}

		void HandleActions()
		{
			if ((CurrentAction == ActionType.Videos) && (CurrentVideo == null))
			{
				CurrentAction = ActionType.Slideshow;
				MusicAutoPlay = false;
			}

			SetupSlideDownloader();

			HideSlideIfNecessary();
			StopMusicIfNecessary();
			StopVideoIfNecessary();

			SetControlsVisibility();

			DisplayNewSlide();
			StartNewMusic();
			StartNewVideo();
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
				TumblrSlideSource.Run(parts[1], Cryptor.Decrypt(parts[2].Substring(1)), fileName => EnqueueSlides(new List<string> { fileName }), tokenSource.Token);
			}
			else
				GoogleSlideSource.Run(currentSlidesQuery, currentSlidesSize, fileName => EnqueueSlides(new List<string> { fileName }), tokenSource.Token);
		}

		void SetControlsVisibility()
		{
			slide1.Visibility = slide2.Visibility = CurrentAction == ActionType.Slideshow ? Visibility.Visible : Visibility.Hidden;
			vlcHost.Visibility = CurrentAction == ActionType.Videos ? Visibility.Visible : Visibility.Hidden;
		}

		string currentSlide = null;
		DoubleAnimation fadeAnimation;
		DateTime? slideTime = null;

		void CheckCycleSlide()
		{
			if ((slideTime == null) || (SlidesPaused))
				return;
			if ((DateTime.Now - slideTime.Value).TotalSeconds >= SlideDisplayTime)
				CycleSlide();
		}

		void HideSlideIfNecessary()
		{
			if ((currentSlide == null) || ((CurrentAction == ActionType.Slideshow) && (currentSlide == CurrentSlide)))
				return;

			currentSlide = null;
			slideTime = null;

			StopSlideFade();

			if ((CurrentAction != ActionType.Slideshow) || (CurrentSlide == null))
				slide1.Source = null;
		}

		void DisplayNewSlide()
		{
			if ((CurrentAction != ActionType.Slideshow) || (currentSlide == CurrentSlide))
				return;

			currentSlide = CurrentSlide;
			var slide = new BitmapImage();
			slide.BeginInit();
			slide.UriSource = new Uri(currentSlide);
			slide.CacheOption = BitmapCacheOption.OnLoad;
			slide.EndInit();
			slide2.Source = slide;
			slideTime = DateTime.Now;

			fadeAnimation = new DoubleAnimation(1, new Duration(TimeSpan.FromSeconds(1)));
			fadeAnimation.Completed += StopSlideFade;
			fadeSlide.BeginAnimation(OpacityProperty, fadeAnimation);
		}

		void StopSlideFade(object sender = null, EventArgs e = null)
		{
			if (fadeAnimation == null)
				return;

			fadeAnimation.Completed -= StopSlideFade;
			fadeAnimation = null;
			fadeSlide.BeginAnimation(OpacityProperty, null);
			fadeSlide.Opacity = 0;
			slide1.Source = slide2.Source ?? slide1.Source;
			slide2.Source = null;
		}

		MediaData currentMusic = null;
		void StopMusicIfNecessary()
		{
			if ((currentMusic == null) || ((CurrentAction == ActionType.Slideshow) && (currentMusic == CurrentMusic)))
				return;

			currentMusic = null;
			vlc.playlist.stop();
			vlc.playlist.items.clear();
		}

		void StartNewMusic()
		{
			if ((CurrentAction != ActionType.Slideshow) || (currentMusic == CurrentMusic) || (!MusicAutoPlay))
				return;

			currentMusic = CurrentMusic;
			vlc.playlist.add(currentMusic.URL);
			vlc.playlist.playItem(0);
		}

		MediaData currentVideo = null;
		void StopVideoIfNecessary()
		{
			if ((currentVideo == null) || ((CurrentAction == ActionType.Videos) && (currentVideo == CurrentVideo)))
				return;

			currentVideo = null;
			vlc.playlist.stop();
			vlc.playlist.items.clear();
		}

		void StartNewVideo()
		{
			if ((CurrentAction != ActionType.Videos) || (currentVideo == CurrentVideo))
				return;

			currentVideo = CurrentVideo;
			vlc.playlist.add(currentVideo.URL);
			vlc.playlist.playItem(0);
		}

		void ToggleSlidesPaused()
		{
			SlidesPaused = !SlidesPaused;
		}

		void SetSlideDisplayTime(int displayTime)
		{
			SlideDisplayTime = displayTime;
		}


		void ChangeSlide(int offset)
		{
			if (offset > 0)
				CycleSlide();
			if (offset < 0)
				CycleSlide(false);
		}

		void Pause()
		{
			if (CurrentAction == ActionType.Slideshow)
				MusicAutoPlay = true;

			vlc.playlist.togglePause();
		}

		void Next()
		{
			if (CurrentAction == ActionType.Videos)
				CycleVideo();
			else
				CycleMusic();
		}

		void SetPosition(int position, bool relative)
		{
			vlc.input.time = (relative ? vlc.input.time : 0) + position * 1000;
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
					Pause();
				else
					ToggleSlidesPaused();
			}
			if (e.Key == Key.Right)
			{
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
					Next();
				else
					ChangeSlide(1);
			}
			if (e.Key == Key.Left)
				ChangeSlide(-1);
			if (e.Key == Key.Q)
				new QueryDialog(this).ShowDialog();
			base.OnKeyDown(e);
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
