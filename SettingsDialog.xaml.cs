using System.Windows;

namespace NeoRemote
{
	partial class SettingsDialog
	{
		static DependencyProperty VideosPathProperty = DependencyProperty.Register(nameof(VideosPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty SlideShowSongsPathProperty = DependencyProperty.Register(nameof(SlideShowSongsPath), typeof(string), typeof(SettingsDialog));

		string VideosPath { get { return (string)GetValue(VideosPathProperty); } set { SetValue(VideosPathProperty, value); } }
		string SlideShowSongsPath { get { return (string)GetValue(SlideShowSongsPathProperty); } set { SetValue(SlideShowSongsPathProperty, value); } }

		public SettingsDialog()
		{
			InitializeComponent();
			DataContext = this;
			VideosPath = Settings.VideosPath;
			SlideShowSongsPath = Settings.SlideShowSongsPath;
		}

		void OnOKClick(object sender, RoutedEventArgs e)
		{
			Settings.VideosPath = VideosPath;
			Settings.SlideShowSongsPath = SlideShowSongsPath;
			DialogResult = true;
		}
	}
}
