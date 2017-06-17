using System.Windows;

namespace NeoMedia
{
	partial class SettingsDialog
	{
		static DependencyProperty MoviesPathProperty = DependencyProperty.Register(nameof(MoviesPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty SlideShowSongsPathProperty = DependencyProperty.Register(nameof(SlideShowSongsPath), typeof(string), typeof(SettingsDialog));

		string MoviesPath { get { return (string)GetValue(MoviesPathProperty); } set { SetValue(MoviesPathProperty, value); } }
		string SlideShowSongsPath { get { return (string)GetValue(SlideShowSongsPathProperty); } set { SetValue(SlideShowSongsPathProperty, value); } }

		public SettingsDialog()
		{
			InitializeComponent();
			DataContext = this;
			MoviesPath = Settings.MoviesPath;
			SlideShowSongsPath = Settings.SlideShowSongsPath;
		}

		void OnOKClick(object sender, RoutedEventArgs e)
		{
			Settings.MoviesPath = MoviesPath;
			Settings.SlideShowSongsPath = SlideShowSongsPath;
			DialogResult = true;
		}
	}
}
