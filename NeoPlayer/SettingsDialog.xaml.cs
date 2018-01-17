using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using NeoPlayer.Models;

namespace NeoPlayer
{
	partial class SettingsDialog
	{
		static DependencyProperty SlidesQueryProperty = DependencyProperty.Register(nameof(SlidesQuery), typeof(string), typeof(SettingsDialog));
		static DependencyProperty SlidesSizeProperty = DependencyProperty.Register(nameof(SlidesSize), typeof(string), typeof(SettingsDialog));
		static DependencyProperty SlideDisplayTimeProperty = DependencyProperty.Register(nameof(SlideDisplayTime), typeof(int), typeof(SettingsDialog));
		static DependencyProperty AddressesProperty = DependencyProperty.Register(nameof(Addresses), typeof(string), typeof(SettingsDialog));

		static DependencyProperty ShortcutsListProperty = DependencyProperty.Register(nameof(ShortcutsList), typeof(ObservableCollection<Shortcut>), typeof(SettingsDialog));

		static DependencyProperty SlidesPathProperty = DependencyProperty.Register(nameof(SlidesPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty MusicPathProperty = DependencyProperty.Register(nameof(MusicPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty VideosPathProperty = DependencyProperty.Register(nameof(VideosPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty YouTubeDLPathProperty = DependencyProperty.Register(nameof(YouTubeDLPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty FFMpegPathProperty = DependencyProperty.Register(nameof(FFMpegPath), typeof(string), typeof(SettingsDialog));
		static DependencyProperty PortProperty = DependencyProperty.Register(nameof(Port), typeof(int), typeof(SettingsDialog));

		string SlidesQuery { get { return (string)GetValue(SlidesQueryProperty); } set { SetValue(SlidesQueryProperty, value); } }
		string SlidesSize { get { return (string)GetValue(SlidesSizeProperty); } set { SetValue(SlidesSizeProperty, value); } }
		int SlideDisplayTime { get { return (int)GetValue(SlideDisplayTimeProperty); } set { SetValue(SlideDisplayTimeProperty, value); } }
		string Addresses { get => (string)GetValue(AddressesProperty); set => SetValue(AddressesProperty, value); }

		ObservableCollection<Shortcut> ShortcutsList { get { return (ObservableCollection<Shortcut>)GetValue(ShortcutsListProperty); } set { SetValue(ShortcutsListProperty, value); } }

		string SlidesPath { get { return (string)GetValue(SlidesPathProperty); } set { SetValue(SlidesPathProperty, value); } }
		string MusicPath { get { return (string)GetValue(MusicPathProperty); } set { SetValue(MusicPathProperty, value); } }
		string VideosPath { get { return (string)GetValue(VideosPathProperty); } set { SetValue(VideosPathProperty, value); } }
		string YouTubeDLPath { get { return (string)GetValue(YouTubeDLPathProperty); } set { SetValue(YouTubeDLPathProperty, value); } }
		string FFMpegPath { get { return (string)GetValue(FFMpegPathProperty); } set { SetValue(FFMpegPathProperty, value); } }
		int Port { get { return (int)GetValue(PortProperty); } set { SetValue(PortProperty, value); } }

		readonly NeoPlayerWindow neoPlayerWindow;
		readonly List<Shortcut> initial;
		SettingsDialog(NeoPlayerWindow neoPlayerWindow)
		{
			Owner = neoPlayerWindow;
			this.neoPlayerWindow = neoPlayerWindow;

			InitializeComponent();
			DataContext = this;

			SlidesQuery = neoPlayerWindow.SlidesQuery;
			SlidesSize = neoPlayerWindow.SlidesSize;
			SlideDisplayTime = neoPlayerWindow.SlideDisplayTime;
			GetAddresses();

			initial = Database.GetAsync<Shortcut>().Result;
			ShortcutsList = new ObservableCollection<Shortcut>(initial.Select(shortcut => Helpers.Copy(shortcut)).OrderBy(shortcut => shortcut.Name));

			SlidesPath = Settings.SlidesPath;
			MusicPath = Settings.MusicPath;
			VideosPath = Settings.VideosPath;
			YouTubeDLPath = Settings.YouTubeDLPath;
			FFMpegPath = Settings.FFMpegPath;
			Port = Settings.Port;
		}

		void GetAddresses()
		{
			var sb = new StringBuilder();
			foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces().OrderBy(inter => inter.NetworkInterfaceType == NetworkInterfaceType.Loopback).ThenBy(inter => inter.OperationalStatus))
				foreach (var address in networkInterface.GetIPProperties().UnicastAddresses.Select(property => property.Address).OrderBy(address => address.AddressFamily != AddressFamily.InterNetwork))
					sb.AppendLine($"{address}: ({networkInterface.NetworkInterfaceType}, {networkInterface.OperationalStatus})");
			Addresses = sb.ToString();
		}

		void OnDeleteShortcutClick(object sender, RoutedEventArgs e) => ShortcutsList.Remove((sender as Button).Tag as Shortcut);

		void OnAddShortcutClick(object sender, RoutedEventArgs e) => ShortcutsList.Add(new Shortcut());

		void OnYouTubeDLUpdateClick(object sender, RoutedEventArgs e) => VideoFileDownloader.Update();

		void OnOKClick(object sender, RoutedEventArgs e)
		{
			neoPlayerWindow.SlidesQuery = SlidesQuery;
			neoPlayerWindow.SlidesSize = SlidesSize;
			neoPlayerWindow.SlideDisplayTime = SlideDisplayTime;

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

		static public void Run(NeoPlayerWindow neoPlayerWindow) => new SettingsDialog(neoPlayerWindow).ShowDialog();
	}
}
