using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NeoMedia
{
	partial class MainWindow
	{
		readonly Actions actions;

		public MainWindow()
		{
			InitializeComponent();

			actions = new Actions(ActionChanged);
			Server.Run(7399, HandleServiceCall);

			vlc.AutoPlay = vlc.Toolbar = vlc.Branding = false;
			vlc.MediaPlayerEndReached += Vlc_MediaPlayerEndReached;
			System.Windows.Forms.Cursor.Hide();
			Loaded += (s, e) => WindowState = WindowState.Maximized;
		}

		DispatcherTimer timer = null;
		object timerLock = new object();
		void ActionChanged()
		{
			if (timer != null)
				return;

			lock (timerLock)
			{
				if (timer != null)
					return;

				timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle, Dispatcher);
				timer.Tick += (s, e) =>
				{
					timer.Stop();
					timer = null;
					HandleActions();
				};
				timer.Start();
			}
		}

		string playing = null;
		void HandleActions()
		{
			var current = actions.CurrentVideo;
			if (current == playing)
				return;

			playing = current;
			vlc.playlist.stop();
			vlc.playlist.items.clear();

			if (playing != null)
			{
				vlc.playlist.add($@"file:///{Settings.VideosPath}\{playing}");
				vlc.playlist.playItem(0);
			}
		}

		private void Vlc_MediaPlayerEndReached(object sender, EventArgs e) => actions.RemoveFirst();

		Result HandleServiceCall(string url)
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
					return Result.Empty;
			}
		}

		Result GetVideos()
		{
			var files = Directory.EnumerateFiles(Settings.VideosPath).Select(file => Path.GetFileName(file)).ToList();
			var queued = actions.Queued;
			var str = $"[ {string.Join(", ", files.Select(file => $@"{{ ""name"": ""{file}"", ""queued"": {queued.Contains(file).ToString().ToLowerInvariant()} }}"))} ]";
			return Result.CreateFromText(str);
		}

		Result Enqueue(IEnumerable<string> fileNames, bool enqueue)
		{
			actions.Enqueue(fileNames, enqueue);
			return Result.Empty;
		}

		Result Pause()
		{
			Dispatcher.Invoke(() => vlc.playlist.togglePause());
			return Result.Empty;
		}

		Result Next()
		{
			actions.RemoveFirst();
			return Result.Empty;
		}

		Result SetPosition(int position, bool relative)
		{
			Dispatcher.Invoke(() => vlc.input.time = (relative ? vlc.input.time : 0) + position * 1000);
			return Result.Empty;
		}

		private Result GetPlayInfo()
		{
			return Dispatcher.Invoke(() =>
			{
				var max = Math.Max(0, (int)vlc.input.length / 1000);
				var position = Math.Min(max, Math.Max(0, (int)vlc.input.time / 1000));
				var playing = vlc.playlist.isPlaying;
				string currentSong = "";
				if (vlc.playlist.currentItem != -1)
					try { currentSong = Path.GetFileName(vlc.mediaDescription.title); } catch { }
				return Result.CreateFromText($@"{{ ""Position"": {position}, ""Max"": {max}, ""Playing"": {playing.ToString().ToLowerInvariant()}, ""CurrentSong"": ""{currentSong}"" }}");
			});
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
