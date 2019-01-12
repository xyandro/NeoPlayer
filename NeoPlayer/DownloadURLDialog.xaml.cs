using System.Windows;

namespace NeoPlayer
{
	partial class DownloadURLDialog
	{
		static DependencyProperty URLProperty = DependencyProperty.Register(nameof(URL), typeof(string), typeof(DownloadURLDialog));

		string URL { get => (string)GetValue(URLProperty); set => SetValue(URLProperty, value); }

		DownloadURLDialog()
		{
			InitializeComponent();
		}

		private void OnDownload(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static string Run()
		{
			var window = new DownloadURLDialog();
			var result = window.ShowDialog();
			if (result.Value)
				return window.URL;
			return null;
		}
	}
}
