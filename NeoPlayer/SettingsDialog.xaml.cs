using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoPlayer.Models;

namespace NeoPlayer
{
	partial class SettingsDialog
	{
		static DependencyProperty ShortcutsListProperty = DependencyProperty.Register(nameof(ShortcutsList), typeof(ObservableCollection<Shortcut>), typeof(SettingsDialog));

		static DependencyProperty SlidesPathProperty = DependencyProperty.Register(nameof(SlidesPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty MusicPathProperty = DependencyProperty.Register(nameof(MusicPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty VideosPathProperty = DependencyProperty.Register(nameof(VideosPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty YouTubeDLPathProperty = DependencyProperty.Register(nameof(YouTubeDLPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty FFMpegPathProperty = DependencyProperty.Register(nameof(FFMpegPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty PortProperty = DependencyProperty.Register(nameof(Port), typeof(int), typeof(SettingsDialog));

		ObservableCollection<Shortcut> ShortcutsList { get { return (ObservableCollection<Shortcut>)GetValue(ShortcutsListProperty); } set { SetValue(ShortcutsListProperty, value); } }

		string SlidesPath { get { return (string)GetValue(SlidesPathProperty); } set { SetValue(SlidesPathProperty, value); } }
		string MusicPath { get { return (string)GetValue(MusicPathProperty); } set { SetValue(MusicPathProperty, value); } }
		string VideosPath { get { return (string)GetValue(VideosPathProperty); } set { SetValue(VideosPathProperty, value); } }
		string YouTubeDLPath { get { return (string)GetValue(YouTubeDLPathProperty); } set { SetValue(YouTubeDLPathProperty, value); } }
		string FFMpegPath { get { return (string)GetValue(FFMpegPathProperty); } set { SetValue(FFMpegPathProperty, value); } }
		int Port { get { return (int)GetValue(PortProperty); } set { SetValue(PortProperty, value); } }

		readonly List<Shortcut> initial;
		public SettingsDialog()
		{
			InitializeComponent();
			DataContext = this;

			initial = Database.GetAsync<Shortcut>().Result;
			ShortcutsList = new ObservableCollection<Shortcut>(initial.Select(shortcut => Helpers.Copy(shortcut)).OrderBy(shortcut => shortcut.Name));

			SlidesPath = Settings.SlidesPath;
			MusicPath = Settings.MusicPath;
			VideosPath = Settings.VideosPath;
			YouTubeDLPath = Settings.YouTubeDLPath;
			FFMpegPath = Settings.FFMpegPath;
			Port = Settings.Port;
		}

		void OnDeleteShortcutClick(object sender, RoutedEventArgs e) => ShortcutsList.Remove((sender as Button).Tag as Shortcut);

		void OnAddShortcutClick(object sender, RoutedEventArgs e) => ShortcutsList.Add(new Shortcut());

		void OnYouTubeDLUpdateClick(object sender, RoutedEventArgs e) => VideoFileDownloader.Update();

		void OnOKClick(object sender, RoutedEventArgs e)
		{
			Settings.SlidesPath = SlidesPath;
			Settings.MusicPath = MusicPath;
			Settings.VideosPath = VideosPath;
			Settings.YouTubeDLPath = YouTubeDLPath;
			Settings.FFMpegPath = FFMpegPath;
			Settings.Port = Port;

			var deleteIDs = initial.Select(shortcut => shortcut.ShortcutID).Except(ShortcutsList.Select(shortcut => shortcut.ShortcutID)).ToList();
			foreach (var shortcutID in deleteIDs)
				Database.DeleteAsync<Shortcut>(shortcutID).Wait();

			var oldShortcuts = initial.ToDictionary(shortcut => shortcut.ShortcutID);
			var updateShortcuts = ShortcutsList.Where(shortcut => (shortcut.ShortcutID == 0) || (!Helpers.Match(shortcut, oldShortcuts[shortcut.ShortcutID]))).ToList();
			foreach (var shortcut in updateShortcuts)
				Database.AddOrUpdateAsync(shortcut).Wait();

			DialogResult = true;
		}
	}
}
