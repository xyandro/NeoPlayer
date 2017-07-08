using System.Windows;

namespace NeoPlayer
{
	partial class App
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			Restarter.CheckRestart(e.Args);

			new NeoPlayerWindow().Show();
			Restarter.Start(7397);
		}

		public App()
		{
			DispatcherUnhandledException += (s, e) => e.Handled = true;
		}
	}
}
