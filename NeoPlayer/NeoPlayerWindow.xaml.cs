﻿using System;
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
		readonly List<string> music = new List<string>();
		readonly List<string> videos = new List<string>();

		int currentSlideIndex = 0;
		public string CurrentSlide => slides.Any() ? slides[currentSlideIndex % slides.Count] : null;
		public string CurrentMusic => music.FirstOrDefault();
		public string CurrentVideo => videos.FirstOrDefault();

		public bool VideoIsQueued(string video) => videos.Contains(video);

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
		public void EnqueueMusic(IEnumerable<string> fileNames, bool enqueue = true) => EnqueueItems(music, fileNames, enqueue);
		public void EnqueueVideos(IEnumerable<string> fileNames, bool enqueue = true) => EnqueueItems(videos, fileNames, enqueue);

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
			InitializeComponent();

			// Keep screen/computer on
			Win32.SetThreadExecutionState(Win32.ES_CONTINUOUS | Win32.ES_DISPLAY_REQUIRED | Win32.ES_SYSTEM_REQUIRED);

			var random = new Random();
			EnqueueMusic(Directory.EnumerateFiles(Settings.MusicPath).OrderBy(x => random.Next()));

			Server.Run(7399, HandleServiceCall);

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

		string currentMusic = null;
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
			vlc.playlist.add($@"file:///{currentMusic}");
			vlc.playlist.playItem(0);
		}

		string currentVideo = null;
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
			vlc.playlist.add($@"file:///{Settings.VideosPath}\{currentVideo}");
			vlc.playlist.playItem(0);
		}

		Response HandleServiceCall(Request request)
		{
			if (!request.URL.StartsWith("Service/"))
				return null;
			var url = request.URL.Substring("Service/".Length);

			switch (url)
			{
				case "GetStatus": return GetStatus();
				case "Enqueue": return Enqueue(request.Parameters["Video"], true);
				case "Dequeue": return Enqueue(request.Parameters["Video"], false);
				case "Pause": return Pause();
				case "Next": return Next();
				case "SetPosition": return SetPosition(int.Parse(request.Parameters["Position"].FirstOrDefault() ?? "0"), bool.Parse(request.Parameters["Relative"].FirstOrDefault() ?? "false"));
				case "SetSlideDisplayTime": return SetSlideDisplayTime(int.Parse(request.Parameters["DisplayTime"].FirstOrDefault() ?? "0"));
				case "ChangeSlide": return ChangeSlide(int.Parse(request.Parameters["Offset"].FirstOrDefault() ?? "0"));
				case "SetSlidesQuery": return SetSlidesQuery(request.Parameters["SlidesQuery"].FirstOrDefault(), request.Parameters["SlidesSize"].FirstOrDefault() ?? "2mp");
				case "ToggleSlidesPaused": return ToggleSlidesPaused();
				default: return Response.Code404;
			}
		}

		Response ToggleSlidesPaused()
		{
			SlidesPaused = !SlidesPaused;
			return Response.Empty;
		}

		Response SetSlideDisplayTime(int displayTime)
		{
			SlideDisplayTime = displayTime;
			return Response.Empty;
		}


		Response ChangeSlide(int offset)
		{
			if (offset > 0)
				CycleSlide();
			if (offset < 0)
				CycleSlide(false);
			return Response.Empty;
		}

		Response GetStatus()
		{
			var status = new Status();
			status.Videos = Directory.EnumerateFiles(Settings.VideosPath)
				.Select(file => Path.GetFileName(file))
				.OrderBy(file => Regex.Replace(file, @"\d+", match => match.Value.PadLeft(10, '0')))
				.Select(file => new Status.MusicData
				{
					Name = file,
					Queued = VideoIsQueued(file),
				})
				.ToList();
			status.PlayerMax = Math.Max(0, (int)vlc.input.length / 1000);
			status.PlayerPosition = Math.Max(0, Math.Min((int)vlc.input.time / 1000, status.PlayerMax));
			status.PlayerIsPlaying = vlc.playlist.isPlaying;
			status.PlayerTitle = "";
			if (vlc.playlist.currentItem != -1)
			{
				try { status.PlayerTitle = Path.GetFileNameWithoutExtension(vlc.mediaDescription.title?.Trim()); } catch { }
				try
				{
					var artist = vlc.mediaDescription.artist?.Trim();
					if (!string.IsNullOrWhiteSpace(artist))
					{
						if (status.PlayerTitle != "")
							status.PlayerTitle += " - ";
						status.PlayerTitle += artist;
					}
				}
				catch { }
			}
			status.SlidesQuery = SlidesQuery;
			status.SlidesSize = SlidesSize;
			status.SlideDisplayTime = SlideDisplayTime;
			status.SlidesPaused = SlidesPaused;

			return JSON.GetResponse(status);
		}

		Response Enqueue(IEnumerable<string> fileNames, bool enqueue)
		{
			EnqueueVideos(fileNames, enqueue);
			if ((enqueue) && (fileNames.Any()))
				CurrentAction = ActionType.Videos;
			return Response.Empty;
		}

		Response Pause()
		{
			if (CurrentAction == ActionType.Slideshow)
				MusicAutoPlay = true;

			vlc.playlist.togglePause();
			return Response.Empty;
		}

		Response Next()
		{
			if (CurrentAction == ActionType.Videos)
				CycleVideo();
			else
				CycleMusic();
			return Response.Empty;
		}

		Response SetPosition(int position, bool relative)
		{
			vlc.input.time = (relative ? vlc.input.time : 0) + position * 1000;
			return Response.Empty;
		}

		Response SetSlidesQuery(string slidesQuery, string slidesSize)
		{
			SlidesQuery = slidesQuery;
			SlidesSize = slidesSize;
			return Response.Empty;
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