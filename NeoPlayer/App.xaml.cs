using System.Windows;

namespace NeoPlayer
{
	partial class App
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			Restarter.CheckRestart(e.Args);

			Restarter.Start(7398);
			new NeoPlayerWindow().Show();
		}

		public App()
		{
			DispatcherUnhandledException += (s, e) => e.Handled = true;
		}
	}
}
