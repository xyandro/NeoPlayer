using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NeoMedia
{
	partial class App
	{
		void ShowExceptionMessage(Exception ex)
		{
			var message = "";
			for (var ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
				message += $"{ex2.Message}\n";

			var window = Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
			MessageBox.Show(window, message, "Error");

#if DEBUG
			if ((Debugger.IsAttached) && ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None))
			{
				var inner = ex;
				while (inner.InnerException != null)
					inner = inner.InnerException;
				var er = inner?.StackTrace?.Split('\r', '\n').FirstOrDefault(a => a.Contains(":line"));
				if (er != null)
				{
					var idx = er.LastIndexOf(" in ");
					if (idx != -1)
						er = er.Substring(idx + 4);
					idx = er.IndexOf(":line ");
					er = $"{er.Substring(0, idx)} {er.Substring(idx + 6)}";
					Clipboard.SetText(er);
				}
				Debugger.Break();
			}
#endif
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

		Result GetMovies()
		{
			var inQueue = new HashSet<string>(Queue);
			var random = new Random();
			var files = Directory.EnumerateFiles(Settings.MoviesPath).Select(file => Path.GetFileName(file)).ToList();
			var str = $"[ {string.Join(", ", files.Select(file => $@"{{ ""name"": ""{file}"", ""queued"": {inQueue.Contains(file).ToString().ToLowerInvariant()} }}"))} ]";
			return Result.CreateFromText(str);
		}

		List<string> Queue = new List<string>();
		Result Enqueue(IEnumerable<string> fileNames, bool enqueue)
		{
			foreach (var fileName in fileNames)
			{
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

		public App()
		{
			DispatcherUnhandledException += App_DispatcherUnhandledException;
			Server.Run(7399, HandleServiceCall);
			var outputFile = Path.Combine(Path.GetDirectoryName(typeof(App).Assembly.Location), "Interceptor.txt");
			//Interceptor.Run(5555, "localhost", 2073, outputFile);
			//Interceptor.Run(5555, "localhost", 7399, outputFile);
			new MainWindow().ShowDialog();
			Environment.Exit(0);
		}

		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			ShowExceptionMessage(e.Exception);
			e.Handled = true;
		}
	}
}
