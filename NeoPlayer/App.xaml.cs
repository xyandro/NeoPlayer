using System.Windows;

namespace NeoPlayer
{
	partial class App
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			new NeoPlayerWindow().Show();
		}

		public App()
		{
			DispatcherUnhandledException += (s, e) => e.Handled = true;
		}
	}
}
