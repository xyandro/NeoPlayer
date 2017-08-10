using System.Windows;

namespace NeoPlayer
{
	partial class App
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			Restarter.CheckRestart(e.Args);

			Restarter.Start(Settings.Port - 1);
			new NeoPlayerWindow().Show();
		}

		public App()
		{
			DispatcherUnhandledException += (s, e) => e.Handled = true;
		}
	}
}
