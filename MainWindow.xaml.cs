using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
				actions.MusicAutoPlay = false;
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
			if ((currentSlidesQuery == actions.SlidesQuery) && (currentSlidesSize == actions.SlidesSize))
				return;

			if (tokenSource != null)
				tokenSource.Cancel();
			actions.ClearSlides();
			currentSlidesQuery = actions.SlidesQuery;
			currentSlidesSize = actions.SlidesSize;
			tokenSource = new CancellationTokenSource();

			if (currentSlidesQuery.StartsWith("tumblr:"))
			{
				var parts = currentSlidesQuery.Split(':');
				var password = Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(parts[2].Substring(1))));
				TumblrSlideSource.Run(parts[1], password, fileName => actions.EnqueueSlides(new List<string> { fileName }), tokenSource.Token);
			}
			else
				GoogleSlideSource.Run(currentSlidesQuery, currentSlidesSize, fileName => actions.EnqueueSlides(new List<string> { fileName }), tokenSource.Token);
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
			if ((actions.CurrentAction != ActionType.Slideshow) || (currentSlide == actions.CurrentSlide))
				return;

			currentSlide = actions.CurrentSlide;
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
			if ((currentMusic == null) || ((actions.CurrentAction == ActionType.Slideshow) && (currentMusic == actions.CurrentMusic)))
				return;

			currentMusic = null;
			vlc.playlist.stop();
			vlc.playlist.items.clear();
		}

		void StartNewMusic()
		{
			if ((actions.CurrentAction != ActionType.Slideshow) || (currentMusic == actions.CurrentMusic) || (!actions.MusicAutoPlay))
				return;

			currentMusic = actions.CurrentMusic;
			vlc.playlist.add($@"file:///{currentMusic}");
			vlc.playlist.playItem(0);
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
			status.SlidesQuery = actions.SlidesQuery;
			status.SlidesSize = actions.SlidesSize;
			status.SlideDisplayTime = actions.SlideDisplayTime;
			status.SlidesPaused = actions.SlidesPaused;

			return JSON.GetResponse(status);
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
				actions.MusicAutoPlay = true;

			vlc.playlist.togglePause();
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
			vlc.input.time = (relative ? vlc.input.time : 0) + position * 1000;
			return Response.Empty;
		}

		Response SetSlidesQuery(string slidesQuery, string slidesSize)
		{
			slidesQuery = Regex.Replace(slidesQuery, @"[\r,]", "\n");
			slidesQuery = Regex.Replace(slidesQuery, @"[^\S\n]+", " ");
			slidesQuery = Regex.Replace(slidesQuery, @"(^ | $)", "", RegexOptions.Multiline);
			slidesQuery = Regex.Replace(slidesQuery, @"\n+", "\n");
			slidesQuery = Regex.Replace(slidesQuery, @"(^\n|\n$)", "");
			slidesQuery = GetTumblrInfo(slidesQuery);
			if (!slidesQuery.StartsWith("tumblr:", StringComparison.OrdinalIgnoreCase))
				slidesQuery = slidesQuery?.ToLowerInvariant() ?? "";
			actions.SlidesQuery = slidesQuery;
			actions.SlidesSize = slidesSize;
			return Response.Empty;
		}

		static byte[] Encrypt(byte[] data)
		{
			using (var alg = new AesCryptoServiceProvider())
			{
				alg.Key = Convert.FromBase64String("uSuggboVimnGAZ1cO8SOi+/GVAebh7lHKzc03OeiLBc=");

				using (var encryptor = alg.CreateEncryptor())
				using (var ms = new MemoryStream())
				{
					ms.Write(BitConverter.GetBytes(alg.IV.Length), 0, sizeof(int));
					ms.Write(alg.IV, 0, alg.IV.Length);
					var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
					ms.Write(encrypted, 0, encrypted.Length);
					return ms.ToArray();
				}
			}
		}

		static byte[] Decrypt(byte[] data)
		{
			using (var alg = new AesCryptoServiceProvider())
			{
				alg.Key = Convert.FromBase64String("uSuggboVimnGAZ1cO8SOi+/GVAebh7lHKzc03OeiLBc=");

				var iv = new byte[BitConverter.ToInt32(data, 0)];
				Array.Copy(data, sizeof(int), iv, 0, iv.Length);
				alg.IV = iv;

				using (var decryptor = alg.CreateDecryptor())
					return decryptor.TransformFinalBlock(data, sizeof(int) + iv.Length, data.Length - sizeof(int) - iv.Length);
			}
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
				parts[2] = $"#{Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(parts[2])))}";
			return string.Join(":", parts);
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
