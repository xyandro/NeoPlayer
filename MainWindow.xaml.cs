using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NeoRemote
{
	partial class MainWindow
	{
		readonly Actions actions;
		readonly DispatcherTimer changeSlideTimer = null;

		public MainWindow()
		{
			InitializeComponent();

			actions = new Actions(ActionChanged);
			var random = new Random();
			actions.EnqueueMusic(Directory.EnumerateFiles(Settings.MusicPath).OrderBy(x => random.Next()));

			Server.Run(7399, HandleServiceCall);

			vlc.AutoPlay = vlc.Toolbar = vlc.Branding = false;
			vlc.MediaPlayerEndReached += (s, e) => Next();
			System.Windows.Forms.Cursor.Hide();
			Loaded += (s, e) => WindowState = WindowState.Maximized;

			changeSlideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
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
			if ((actions.CurrentAction == ActionType.Videos) && (actions.CurrentVideo == null))
			{
				actions.CurrentAction = ActionType.Slideshow;
				actions.SlideMusicAutoPlay = false;
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
		void SetupSlideDownloader()
		{
			if (currentSlidesQuery == actions.SlidesQuery)
				return;
			currentSlidesQuery = actions.SlidesQuery;
			SlideDownloader.Run(currentSlidesQuery, "2mp", actions);
		}

		void SetControlsVisibility()
		{
			slide1.Visibility = slide2.Visibility = actions.CurrentAction == ActionType.Slideshow ? Visibility.Visible : Visibility.Hidden;
			vlcHost.Visibility = actions.CurrentAction == ActionType.Videos ? Visibility.Visible : Visibility.Hidden;
		}

		string currentSlide = null;
		DoubleAnimation fadeAnimation;
		DateTime? slideTime = null;

		void CheckCycleSlide()
		{
			if ((slideTime == null) || (actions.SlidesPaused))
				return;
			if ((DateTime.Now - slideTime.Value).TotalSeconds >= actions.SlideDisplayTime)
				actions.CycleSlide();
		}

		void HideSlideIfNecessary()
		{
			if ((currentSlide == null) || ((actions.CurrentAction == ActionType.Slideshow) && (currentSlide == actions.CurrentSlide)))
				return;

			currentSlide = null;
			slideTime = null;

			StopSlideFade();

			if ((actions.CurrentAction != ActionType.Slideshow) || (actions.CurrentSlide == null))
				slide1.Source = null;
		}

		void DisplayNewSlide()
		{
			if (actions.CurrentAction != ActionType.Slideshow)
				return;

			while (true)
			{
				if (currentSlide == actions.CurrentSlide)
					return;

				try
				{
					var slide = new BitmapImage();
					slide.BeginInit();
					slide.UriSource = new Uri(actions.CurrentSlide);
					slide.CacheOption = BitmapCacheOption.OnLoad;
					slide.EndInit();
					slide2.Source = slide;
					break;
				}
				catch
				{
					actions.EnqueueSlides(new List<string> { actions.CurrentSlide }, false);
					continue;
				}
			}

			currentSlide = actions.CurrentSlide;
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
			if ((currentMusic == null) || ((actions.CurrentAction == ActionType.Slideshow) && (currentMusic == actions.CurrentMusic)))
				return;

			currentMusic = null;
			vlc.playlist.stop();
			vlc.playlist.items.clear();
		}

		void StartNewMusic()
		{
			if ((actions.CurrentAction != ActionType.Slideshow) || (currentMusic == actions.CurrentMusic))
				return;

			currentMusic = actions.CurrentMusic;
			vlc.playlist.add($@"file:///{currentMusic}");
			vlc.playlist.playItem(0);
			if (!actions.SlideMusicAutoPlay)
			{
				Thread.Sleep(50);
				vlc.playlist.pause();
			}
		}

		string currentVideo = null;
		void StopVideoIfNecessary()
		{
			if ((currentVideo == null) || ((actions.CurrentAction == ActionType.Videos) && (currentVideo == actions.CurrentVideo)))
				return;

			currentVideo = null;
			vlc.playlist.stop();
			vlc.playlist.items.clear();
		}

		void StartNewVideo()
		{
			if ((actions.CurrentAction != ActionType.Videos) || (currentVideo == actions.CurrentVideo))
				return;

			currentVideo = actions.CurrentVideo;
			vlc.playlist.add($@"file:///{Settings.VideosPath}\{currentVideo}");
			vlc.playlist.playItem(0);
		}

		Response HandleServiceCall(string url)
		{
			if (url.StartsWith("Service/"))
				url = url.Substring("Service/".Length);

			var queryIndex = url.IndexOf('?');
			var query = queryIndex == -1 ? "" : url.Substring(queryIndex + 1);
			url = queryIndex == -1 ? url : url.Remove(queryIndex);
			var parsed = HttpUtility.ParseQueryString(query);
			var parameters = parsed.AllKeys.ToDictionary(key => key, key => parsed.GetValues(key));
			switch (url)
			{
				case "GetStatus": return GetStatus();
				case "Enqueue": return Enqueue(parameters["Video"], true);
				case "Dequeue": return Enqueue(parameters["Video"], false);
				case "Pause": return Pause();
				case "Next": return Next();
				case "SetPosition": return SetPosition(int.Parse(parameters["Position"].FirstOrDefault() ?? "0"), bool.Parse(parameters["Relative"].FirstOrDefault() ?? "false"));
				case "SetSlideDisplayTime": return SetSlideDisplayTime(int.Parse(parameters["DisplayTime"].FirstOrDefault() ?? "0"));
				case "ChangeSlide": return ChangeSlide(int.Parse(parameters["Offset"].FirstOrDefault() ?? "0"));
				case "SetSlidesQuery": return SetSlidesQuery(parameters["SlidesQuery"].FirstOrDefault());
				case "ToggleSlidesPaused": return ToggleSlidesPaused();
				default: return Response.Code404;
			}
		}

		Response ToggleSlidesPaused()
		{
			actions.SlidesPaused = !actions.SlidesPaused;
			return Response.Empty;
		}

		Response SetSlideDisplayTime(int displayTime)
		{
			actions.SlideDisplayTime = displayTime;
			return Response.Empty;
		}


		Response ChangeSlide(int offset)
		{
			if (offset > 0)
				actions.CycleSlide();
			if (offset < 0)
				actions.CycleSlide(false);
			return Response.Empty;
		}

		Response GetStatus()
		{
			return Dispatcher.Invoke(() =>
			{
				var status = new Status();
				status.Videos = Directory.EnumerateFiles(Settings.VideosPath)
					.Select(file => Path.GetFileName(file))
					.OrderBy(file => Regex.Replace(file, @"\d+", match => match.Value.PadLeft(10, '0')))
					.Select(file => new Status.MusicData
					{
						Name = file,
						Queued = actions.VideoIsQueued(file),
					})
					.ToList();
				status.PlayerMax = Math.Max(0, (int)vlc.input.length / 1000);
				status.PlayerPosition = Math.Max(0, Math.Min((int)vlc.input.time / 1000, status.PlayerMax));
				status.PlayerIsPlaying = vlc.playlist.isPlaying;
				status.PlayerTitle = "";
				if (vlc.playlist.currentItem != -1)
					try { status.PlayerTitle = Path.GetFileName(vlc.mediaDescription.title); } catch { }
				status.SlidesQuery = actions.SlidesQuery;
				status.SlideDisplayTime = actions.SlideDisplayTime;
				status.SlidesPaused = actions.SlidesPaused;

				return JSON.GetResponse(status);
			});
		}

		Response Enqueue(IEnumerable<string> fileNames, bool enqueue)
		{
			actions.EnqueueVideos(fileNames, enqueue);
			if ((enqueue) && (fileNames.Any()))
				actions.CurrentAction = ActionType.Videos;
			return Response.Empty;
		}

		Response Pause()
		{
			if (actions.CurrentAction == ActionType.Slideshow)
				actions.SlideMusicAutoPlay = true;
			Dispatcher.Invoke(() => vlc.playlist.togglePause());
			return Response.Empty;
		}

		Response Next()
		{
			if (actions.CurrentAction == ActionType.Videos)
				actions.CycleVideo();
			else
				actions.CycleMusic();
			return Response.Empty;
		}

		Response SetPosition(int position, bool relative)
		{
			Dispatcher.Invoke(() => vlc.input.time = (relative ? vlc.input.time : 0) + position * 1000);
			return Response.Empty;
		}

		Response SetSlidesQuery(string slidesQuery)
		{
			slidesQuery = slidesQuery?.ToLowerInvariant() ?? "";
			slidesQuery = Regex.Replace(slidesQuery, @"[\r,]", "\n");
			slidesQuery = Regex.Replace(slidesQuery, @"[^\S\n]+", " ");
			slidesQuery = Regex.Replace(slidesQuery, @"(^ | $)", "", RegexOptions.Multiline);
			slidesQuery = Regex.Replace(slidesQuery, @"\n+", "\n");
			slidesQuery = Regex.Replace(slidesQuery, @"(^\n|\n$)", "");
			actions.SlidesQuery = slidesQuery;
			return Response.Empty;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if ((e.Key == Key.S) && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
			{
				new SettingsDialog().ShowDialog();
				e.Handled = true;
			}
			base.OnKeyDown(e);
		}
	}
}
