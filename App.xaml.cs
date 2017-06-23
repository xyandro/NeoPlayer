using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NeoRemote
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

			if ((Settings.Debug) && (Debugger.IsAttached) && ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None))
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
		}

		public App()
		{
			DispatcherUnhandledException += App_DispatcherUnhandledException;
			//var outputFile = Path.Combine(Path.GetDirectoryName(typeof(App).Assembly.Location), "Interceptor.txt");
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
