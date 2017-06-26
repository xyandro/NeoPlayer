using System.Windows;

namespace NeoPlayer
{
	partial class App
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			new MainWindow().Show();
		}

		public App()
		{
			DispatcherUnhandledException += (s, e) => e.Handled = true;
		}
	}
}
