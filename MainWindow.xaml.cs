using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Windows;
using System.Windows.Input;

namespace NeoMedia
{
	partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();

			Server.Run(7399, HandleServiceCall);

			vlc.AutoPlay = false;
			vlc.Toolbar = false;
			Loaded += (s, e) => WindowState = WindowState.Maximized;
		}

		Result HandleServiceCall(string url)
		{
			return Dispatcher.Invoke(() =>
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
					case "movies": return GetMovies();
					case "enqueue": return Enqueue(parameters["movie"], true);
					case "dequeue": return Enqueue(parameters["movie"], false);
					default:
#if DEBUG
						MessageBox.Show($"Service: {url}");
#endif
						return null;
				}
			});
		}

		List<string> Queue = new List<string>();

		Result GetMovies()
		{
			var inQueue = new HashSet<string>(Queue);
			var files = Directory.EnumerateFiles(Settings.MoviesPath).Select(file => Path.GetFileName(file)).ToList();
			var str = $"[ {string.Join(", ", files.Select(file => $@"{{ ""name"": ""{file}"", ""queued"": {inQueue.Contains(file).ToString().ToLowerInvariant()} }}"))} ]";
			return Result.CreateFromText(str);
		}

		Result Enqueue(IEnumerable<string> fileNames, bool enqueue)
		{
			var first = true;
			foreach (var fileName in fileNames)
			{
				if ((enqueue) && (first))
				{
					vlc.playlist.items.clear();
					vlc.playlist.add($@"file:///{Settings.MoviesPath}\{fileName}");
					vlc.playlist.playItem(0);
					vlc.input.time = 20000;
					first = false;
				}
				var present = Queue.Contains(fileName);
				if (present != enqueue)
				{
					if (enqueue)
						Queue.Add(fileName);
					else
						Queue.Remove(fileName);
				}
			}
			return Result.Empty;
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
