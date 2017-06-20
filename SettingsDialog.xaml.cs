using System.Windows;

namespace NeoRemote
{
	partial class SettingsDialog
	{
		static DependencyProperty SlideShowImagesPathProperty = DependencyProperty.Register(nameof(SlideShowImagesPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty SlideShowSongsPathProperty = DependencyProperty.Register(nameof(SlideShowSongsPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty VideosPathProperty = DependencyProperty.Register(nameof(VideosPath), typeof(string), typeof(SettingsDialog));

		string SlideShowImagesPath { get { return (string)GetValue(SlideShowImagesPathProperty); } set { SetValue(SlideShowImagesPathProperty, value); } }
		string SlideShowSongsPath { get { return (string)GetValue(SlideShowSongsPathProperty); } set { SetValue(SlideShowSongsPathProperty, value); } }
		string VideosPath { get { return (string)GetValue(VideosPathProperty); } set { SetValue(VideosPathProperty, value); } }

		public SettingsDialog()
		{
			InitializeComponent();
			DataContext = this;
			SlideShowImagesPath = Settings.SlideShowImagesPath;
			SlideShowSongsPath = Settings.SlideShowSongsPath;
			VideosPath = Settings.VideosPath;
		}

		void OnOKClick(object sender, RoutedEventArgs e)
		{
			Settings.SlideShowImagesPath = SlideShowImagesPath;
			Settings.SlideShowSongsPath = SlideShowSongsPath;
			Settings.VideosPath = VideosPath;
			DialogResult = true;
		}
	}
}
