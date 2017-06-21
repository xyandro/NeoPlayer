using System.Windows;

namespace NeoRemote
{
	partial class SettingsDialog
	{
		static DependencyProperty SlidesPathProperty = DependencyProperty.Register(nameof(SlidesPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty SlideShowSongsPathProperty = DependencyProperty.Register(nameof(SlideShowSongsPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty VideosPathProperty = DependencyProperty.Register(nameof(VideosPath), typeof(string), typeof(SettingsDialog));

		string SlidesPath { get { return (string)GetValue(SlidesPathProperty); } set { SetValue(SlidesPathProperty, value); } }
		string SlideShowSongsPath { get { return (string)GetValue(SlideShowSongsPathProperty); } set { SetValue(SlideShowSongsPathProperty, value); } }
		string VideosPath { get { return (string)GetValue(VideosPathProperty); } set { SetValue(VideosPathProperty, value); } }

		public SettingsDialog()
		{
			InitializeComponent();
			DataContext = this;
			SlidesPath = Settings.SlidesPath;
			SlideShowSongsPath = Settings.SlideShowSongsPath;
			VideosPath = Settings.VideosPath;
		}

		void OnOKClick(object sender, RoutedEventArgs e)
		{
			Settings.SlidesPath = SlidesPath;
			Settings.SlideShowSongsPath = SlideShowSongsPath;
			Settings.VideosPath = VideosPath;
			DialogResult = true;
		}
	}
}
