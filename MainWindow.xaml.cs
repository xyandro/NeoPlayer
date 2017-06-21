using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

		public MainWindow()
		{
			InitializeComponent();

			actions = new Actions(ActionChanged);
			var random = new Random();
			actions.EnqueueSongs(Directory.EnumerateFiles(Settings.SlideShowSongsPath).OrderBy(x => random.Next()));

			Server.Run(7399, HandleServiceCall);

			vlc.AutoPlay = vlc.Toolbar = vlc.Branding = false;
			vlc.MediaPlayerEndReached += (s, e) => Next();
			System.Windows.Forms.Cursor.Hide();
			Loaded += (s, e) => WindowState = WindowState.Maximized;
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
				actions.CurrentAction = ActionType.SlideshowImages;

			SetupImageDownloader();

			HideImageIfNecessary();
			StopSongIfNecessary();
			StopVideoIfNecessary();

			SetControlsVisibility();

			DisplayNewImage();
			StartNewSong();
			StartNewVideo();
		}

		string currentQuery;
		void SetupImageDownloader()
		{
			if (currentQuery == actions.ImageQuery)
				return;
			currentQuery = actions.ImageQuery;
			ImageDownloader.Run(currentQuery, "2mp", actions);
		}

		void SetControlsVisibility()
		{
			image1.Visibility = image2.Visibility = actions.CurrentAction.HasFlag(ActionType.SlideshowImages) ? Visibility.Visible : Visibility.Hidden;
			vlcHost.Visibility = actions.CurrentAction.HasFlag(ActionType.Videos) ? Visibility.Visible : Visibility.Hidden;
		}

		string currentImage = null;
		DispatcherTimer changeImageTimer = null;
		DoubleAnimation fadeAnimation;
		void HideImageIfNecessary()
		{
			if ((currentImage == null) || ((actions.CurrentAction.HasFlag(ActionType.SlideshowImages)) && (currentImage == actions.CurrentImage)))
				return;

			currentImage = null;

			changeImageTimer.Stop();
			changeImageTimer = null;

			StopImageFade();

			if ((!actions.CurrentAction.HasFlag(ActionType.SlideshowImages)) || (actions.CurrentImage == null))
				image1.Source = null;
		}

		void DisplayNewImage()
		{
			if (!actions.CurrentAction.HasFlag(ActionType.SlideshowImages))
				return;

			while (true)
			{
				if (currentImage == actions.CurrentImage)
					return;

				try
				{
					var bitmapImage = new BitmapImage();
					bitmapImage.BeginInit();
					bitmapImage.UriSource = new Uri(actions.CurrentImage);
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.EndInit();
					image2.Source = bitmapImage;
					break;
				}
				catch
				{
					actions.EnqueueImages(new List<string> { actions.CurrentImage }, false);
					continue;
				}
			}

			currentImage = actions.CurrentImage;

			fadeAnimation = new DoubleAnimation(1, new Duration(TimeSpan.FromSeconds(1)));
			fadeAnimation.Completed += StopImageFade;
			fadeImage.BeginAnimation(OpacityProperty, fadeAnimation);

			changeImageTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(actions.SlideshowDelay) };
			changeImageTimer.Tick += (s, e) => actions.CycleImage();
			changeImageTimer.Start();
		}

		void StopImageFade(object sender = null, EventArgs e = null)
		{
			if (fadeAnimation == null)
				return;

			fadeAnimation.Completed -= StopImageFade;
			fadeAnimation = null;
			fadeImage.BeginAnimation(OpacityProperty, null);
			fadeImage.Opacity = 0;
			image1.Source = image2.Source ?? image1.Source;
			image2.Source = null;
		}

		string currentSong = null;
		void StopSongIfNecessary()
		{
			if ((currentSong == null) || ((actions.CurrentAction.HasFlag(ActionType.SlideshowImages)) && (currentSong == actions.CurrentSong)))
				return;

			currentSong = null;
			vlc.playlist.stop();
			vlc.playlist.items.clear();
		}

		void StartNewSong()
		{
			if ((!actions.CurrentAction.HasFlag(ActionType.SlideshowSongs)) || (currentSong == actions.CurrentSong))
				return;

			currentSong = actions.CurrentSong;
			vlc.playlist.add($@"file:///{currentSong}");
			vlc.playlist.playItem(0);
		}

		string currentVideo = null;
		void StopVideoIfNecessary()
		{
			if ((currentVideo == null) || ((actions.CurrentAction.HasFlag(ActionType.Videos)) && (currentVideo == actions.CurrentVideo)))
				return;

			currentVideo = null;
			vlc.playlist.stop();
			vlc.playlist.items.clear();
		}

		void StartNewVideo()
		{
			if ((!actions.CurrentAction.HasFlag(ActionType.Videos)) || (currentVideo == actions.CurrentVideo))
				return;

			currentVideo = actions.CurrentVideo;
			vlc.playlist.add($@"file:///{Settings.VideosPath}\{currentVideo}");
			vlc.playlist.playItem(0);
		}

		Response HandleServiceCall(string url)
		{
			if (url.StartsWith("service/"))
				url = url.Substring("service/".Length);

			var queryIndex = url.IndexOf('?');
			var query = queryIndex == -1 ? "" : url.Substring(queryIndex + 1);
			url = queryIndex == -1 ? url : url.Remove(queryIndex);
			var parsed = HttpUtility.ParseQueryString(query);
			var parameters = parsed.AllKeys.ToDictionary(key => key, key => parsed.GetValues(key));
			switch (url)
			{
				case "getStatus": return GetStatus();
				case "enqueue": return Enqueue(parameters["video"], true);
				case "dequeue": return Enqueue(parameters["video"], false);
				case "pause": return Pause();
				case "next": return Next();
				case "setPosition": return SetPosition(int.Parse(parameters["position"].FirstOrDefault() ?? "0"), bool.Parse(parameters["relative"].FirstOrDefault() ?? "false"));
				case "setSlideshowDelay": return SetSlideshowDelay(int.Parse(parameters["delay"].FirstOrDefault() ?? "0"));
				case "changeImage": return ChangeImage(int.Parse(parameters["offset"].FirstOrDefault() ?? "0"));
				case "setQuery": return SetQuery(parameters["query"].FirstOrDefault());
				default: return Response.Code404;
			}
		}

		Response SetSlideshowDelay(int delay)
		{
			actions.SlideshowDelay = delay;
			return Response.Empty;
		}


		Response ChangeImage(int offset)
		{
			if (offset > 0)
				actions.CycleImage();
			if (offset < 0)
				actions.CycleImage(false);
			return Response.Empty;
		}

		Response GetStatus()
		{
			return Dispatcher.Invoke(() =>
			{
				var status = new Status();
				status.Videos = Directory
					.EnumerateFiles(Settings.VideosPath)
					.Select(file => Path.GetFileName(file))
					.OrderBy(file => Regex.Replace(file, @"\d+", match => match.Value.PadLeft(10, '0')))
					.Select(file => new Status.SongData
					{
						Name = file,
						Queued = actions.VideoIsQueued(file),
					})
					.ToList();
				status.Max = Math.Max(0, (int)vlc.input.length / 1000);
				status.Position = Math.Max(0, Math.Min((int)vlc.input.time / 1000, status.Max));
				status.Playing = vlc.playlist.isPlaying;
				status.CurrentSong = "";
				if (vlc.playlist.currentItem != -1)
					try { status.CurrentSong = Path.GetFileName(vlc.mediaDescription.title); } catch { }
				status.ImageQuery = actions.ImageQuery.Replace(@"""", "'");
				status.SlideshowDelay = actions.SlideshowDelay;

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
			if (actions.CurrentAction == ActionType.SlideshowImages)
				actions.CurrentAction = ActionType.Slideshow;
			else
				Dispatcher.Invoke(() => vlc.playlist.togglePause());
			return Response.Empty;
		}

		Response Next()
		{
			if (actions.CurrentAction == ActionType.Videos)
				actions.CycleVideo();
			else
				actions.CycleSong();
			return Response.Empty;
		}

		Response SetPosition(int position, bool relative)
		{
			Dispatcher.Invoke(() => vlc.input.time = (relative ? vlc.input.time : 0) + position * 1000);
			return Response.Empty;
		}

		Response SetQuery(string query)
		{
			actions.ImageQuery = query?.ToLowerInvariant();
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
