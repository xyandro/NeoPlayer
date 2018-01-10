using System.Windows;

namespace NeoPlayer
{
	partial class SettingsDialog
	{
		static DependencyProperty SlidesPathProperty = DependencyProperty.Register(nameof(SlidesPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty MusicPathProperty = DependencyProperty.Register(nameof(MusicPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty VideosPathProperty = DependencyProperty.Register(nameof(VideosPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty YouTubeDLPathProperty = DependencyProperty.Register(nameof(YouTubeDLPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty FFMpegPathProperty = DependencyProperty.Register(nameof(FFMpegPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty PortProperty = DependencyProperty.Register(nameof(Port), typeof(int), typeof(SettingsDialog));

		string SlidesPath { get { return (string)GetValue(SlidesPathProperty); } set { SetValue(SlidesPathProperty, value); } }
		string MusicPath { get { return (string)GetValue(MusicPathProperty); } set { SetValue(MusicPathProperty, value); } }
		string VideosPath { get { return (string)GetValue(VideosPathProperty); } set { SetValue(VideosPathProperty, value); } }
		string YouTubeDLPath { get { return (string)GetValue(YouTubeDLPathProperty); } set { SetValue(YouTubeDLPathProperty, value); } }
		string FFMpegPath { get { return (string)GetValue(FFMpegPathProperty); } set { SetValue(FFMpegPathProperty, value); } }
		int Port { get { return (int)GetValue(PortProperty); } set { SetValue(PortProperty, value); } }

		public SettingsDialog()
		{
			InitializeComponent();
			DataContext = this;
			SlidesPath = Settings.SlidesPath;
			MusicPath = Settings.MusicPath;
			VideosPath = Settings.VideosPath;
			YouTubeDLPath = Settings.YouTubeDLPath;
			FFMpegPath = Settings.FFMpegPath;
			Port = Settings.Port;
		}

		void OnOKClick(object sender, RoutedEventArgs e)
		{
			Settings.SlidesPath = SlidesPath;
			Settings.MusicPath = MusicPath;
			Settings.VideosPath = VideosPath;
			Settings.YouTubeDLPath = YouTubeDLPath;
			Settings.FFMpegPath = FFMpegPath;
			Settings.Port = Port;
			DialogResult = true;
		}
	}
}
