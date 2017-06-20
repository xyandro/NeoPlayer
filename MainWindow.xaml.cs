using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Controls;
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
			actions.EnqueueImages(Directory.EnumerateFiles(@"D:\Documents\Transfer\Pictures"));
			//actions.EnqueueSongs(Directory.EnumerateFiles(Settings.SlideShowSongsPath));
			actions.CurrentAction = ActionType.Slideshow;

			Server.Run(7399, HandleServiceCall);

			vlc.AutoPlay = vlc.Toolbar = vlc.Branding = false;
			vlc.MediaPlayerEndReached += Vlc_MediaPlayerEndReached;
			System.Windows.Forms.Cursor.Hide();
			Loaded += (s, e) => WindowState = WindowState.Maximized;
		}

		DispatcherTimer timer = null;
		void ActionChanged()
		{
			if (timer != null)
				return;

			timer = new DispatcherTimer();
			timer.Tick += (s, e) =>
			{
				timer.Stop();
				timer = null;
				HandleActions();
			};
			timer.Start();
		}

		DispatcherTimer changeImageTimer = null;
		string currentImage = null;
		string currentSong = null;
		string currentVideo = null;
		void HandleActions()
		{
			// Stop image timer if necessary
			if ((currentImage != null) && ((actions.CurrentAction != ActionType.Slideshow) || (currentImage != actions.CurrentImage)))
			{
				currentImage = null;
				changeImageTimer.Stop();
				changeImageTimer = null;
				if (actions.CurrentAction != ActionType.Slideshow)
					image1.Source = image2.Source = null;
			}

			// Stop current song if necessary
			if ((currentSong != null) && ((actions.CurrentAction != ActionType.Slideshow) || (currentSong != actions.CurrentSong)))
			{
				currentSong = null;
				vlc.playlist.stop();
				vlc.playlist.items.clear();
			}

			// Stop current video if necessary
			if ((currentVideo != null) && ((actions.CurrentAction != ActionType.Videos) || (currentVideo != actions.CurrentVideo)))
			{
				currentVideo = null;
				vlc.playlist.stop();
				vlc.playlist.items.clear();
			}

			// Hide things
			image1.Visibility = image2.Visibility = actions.CurrentAction == ActionType.Slideshow ? Visibility.Visible : Visibility.Hidden;
			vlcHost.Visibility = actions.CurrentAction == ActionType.Videos ? Visibility.Visible : Visibility.Hidden;

			// Display new image
			if ((actions.CurrentAction == ActionType.Slideshow) && (currentImage != actions.CurrentImage))
			{
				currentImage = actions.CurrentImage;
				var bitmap = System.Drawing.Image.FromFile(currentImage) as System.Drawing.Bitmap;
				using (var memory = new MemoryStream())
				{
					bitmap.Save(memory, ImageFormat.Png);
					memory.Position = 0;
					var bitmapImage = new BitmapImage();
					bitmapImage.BeginInit();
					bitmapImage.StreamSource = memory;
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.EndInit();
					image2.Source = bitmapImage;
				}

				image1.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(1))));
				var anim2 = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(1)));
				anim2.Completed += (s, e) =>
				{
					image1.Source = image2.Source;
					//image1.Opacity = 1;
					//image2.Opacity = 0;
					//image2.Source = null;
				};
				image2.BeginAnimation(OpacityProperty, anim2);

				changeImageTimer = new DispatcherTimer();
				changeImageTimer.Interval = TimeSpan.FromSeconds(2);
				changeImageTimer.Tick += (s, e) => actions.CycleImage();
				changeImageTimer.Start();
			}

			// Start new song
			if ((actions.CurrentAction == ActionType.Slideshow) && (currentSong != actions.CurrentSong))
			{
				currentSong = actions.CurrentSong;
				vlc.playlist.add($@"file:///{currentSong}");
				vlc.playlist.playItem(0);
			}

			// Start new video
			if ((actions.CurrentAction == ActionType.Videos) && (currentVideo != actions.CurrentVideo))
			{
				currentVideo = actions.CurrentVideo;
				vlc.playlist.add($@"file:///{Settings.VideosPath}\{currentVideo}");
				vlc.playlist.playItem(0);
			}
		}

		void Vlc_MediaPlayerEndReached(object sender, EventArgs e) => Next();

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
				case "videos": return GetVideos();
				case "enqueue": return Enqueue(parameters["video"], true);
				case "dequeue": return Enqueue(parameters["video"], false);
				case "pause": return Pause();
				case "next": return Next();
				case "setposition": return SetPosition(int.Parse(parameters["position"].FirstOrDefault() ?? "0"), bool.Parse(parameters["relative"].FirstOrDefault() ?? "false"));
				case "getplayinfo": return GetPlayInfo();
				default:
					if (Settings.Debug)
						MessageBox.Show($"Service: {url}");
					return Response.Empty;
			}
		}

		Response GetVideos()
		{
			var files = Directory
				.EnumerateFiles(Settings.VideosPath)
				.Select(file => Path.GetFileName(file))
				.OrderBy(file => Regex.Replace(file, @"\d+", match => match.Value.PadLeft(10, '0')))
				.ToList();
			var str = $"[ {string.Join(", ", files.Select(file => $@"{{ ""name"": ""{file}"", ""queued"": {actions.VideoIsQueued(file).ToString().ToLowerInvariant()} }}"))} ]";
			return Response.CreateFromText(str);
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

		Response GetPlayInfo()
		{
			return Dispatcher.Invoke(() =>
			{
				var max = Math.Max(0, (int)vlc.input.length / 1000);
				var position = Math.Min(max, Math.Max(0, (int)vlc.input.time / 1000));
				var playing = vlc.playlist.isPlaying;
				string currentSong = "";
				if (vlc.playlist.currentItem != -1)
					try { currentSong = Path.GetFileName(vlc.mediaDescription.title); } catch { }
				return Response.CreateFromText($@"{{ ""Position"": {position}, ""Max"": {max}, ""Playing"": {playing.ToString().ToLowerInvariant()}, ""CurrentSong"": ""{currentSong}"" }}");
			});
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if ((e.Key == Key.S) && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
			{
				new SettingsDialog().ShowDialog();
				e.Handled = true;
			}
			if (e.Key == Key.Right)
				actions.CycleImage();
			base.OnKeyDown(e);
		}
	}
}
