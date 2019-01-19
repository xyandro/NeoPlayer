using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using NeoPlayer.Downloaders;
using NeoPlayer.Misc;
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

		string SlidesQuery { get => (string)GetValue(SlidesQueryProperty); set => SetValue(SlidesQueryProperty, value); }
		string SlidesSize { get => (string)GetValue(SlidesSizeProperty); set => SetValue(SlidesSizeProperty, value); }
		int SlideDisplayTime { get => (int)GetValue(SlideDisplayTimeProperty); set => SetValue(SlideDisplayTimeProperty, value); }
		string Addresses { get => (string)GetValue(AddressesProperty); set => SetValue(AddressesProperty, value); }

		ObservableCollection<Shortcut> ShortcutsList { get => (ObservableCollection<Shortcut>)GetValue(ShortcutsListProperty); set => SetValue(ShortcutsListProperty, value); }

		string SlidesPath { get => (string)GetValue(SlidesPathProperty); set => SetValue(SlidesPathProperty, value); }
		string MusicPath { get => (string)GetValue(MusicPathProperty); set => SetValue(MusicPathProperty, value); }
		string VideosPath { get => (string)GetValue(VideosPathProperty); set => SetValue(VideosPathProperty, value); }
		string YouTubeDLPath { get => (string)GetValue(YouTubeDLPathProperty); set => SetValue(YouTubeDLPathProperty, value); }
		string FFMpegPath { get => (string)GetValue(FFMpegPathProperty); set => SetValue(FFMpegPathProperty, value); }
		int Port { get => (int)GetValue(PortProperty); set => SetValue(PortProperty, value); }

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

		void OnSyncVideosClick(object sender, RoutedEventArgs e)
		{
			var videoFiles = Database.GetAsync<VideoFile>().Result.ToDictionary(videoFile => videoFile.Identifier);

			foreach (var path in Directory.EnumerateFiles(VideosPath))
			{
				var file = Path.GetFileNameWithoutExtension(path);
				var identifierIndex = file.IndexOf("-NEID-");
				if (identifierIndex == -1)
					continue;

				var identifier = file.Substring(identifierIndex);
				if (videoFiles.ContainsKey(identifier))
				{
					videoFiles.Remove(identifier);
					continue;
				}

				Database.SaveVideoFileAsync(new VideoFile
				{
					Title = file.Substring(0, identifierIndex),
					FileName = Path.GetFileName(path),
					Identifier = identifier,
					DownloadDate = new FileInfo(path).LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
				}).Wait();
			}

			foreach (var videoFile in videoFiles.Values)
				Database.DeleteAsync(videoFile).Wait();

			neoPlayerWindow.UpdateVideoFiles();
		}

		async void OnYouTubeSyncClick(object sender, RoutedEventArgs e)
		{
			var keep = new HashSet<string>((await Database.GetAsync<VideoFile>()).Where(x => x.Identifier.StartsWith("-NEID-youtube-")).Select(x => x.Identifier.Substring("-NEID-youtube-".Length)));

			UserCredential credential;
			using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
			{
				credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					new[] { YouTubeService.Scope.Youtube },
					"user",
					CancellationToken.None
				);
			}
			var service = new YouTubeService(new BaseClientService.Initializer { HttpClientInitializer = credential, ApplicationName = "NeoPlayer" });

			var playlists = await new PlaylistsResource.ListRequest(service, "snippet") { Mine = true }.ExecuteAsync();
			var coolPlaylist = playlists.Items.Single(item => item.Snippet.Title.Equals("cool", StringComparison.OrdinalIgnoreCase));

			var list = new List<PlaylistItem>();
			var token = default(string);
			while (true)
			{
				var listRequest = new PlaylistItemsResource.ListRequest(service, "snippet") { PlaylistId = coolPlaylist.Id, MaxResults = 50, PageToken = token };
				var listResult = await listRequest.ExecuteAsync();
				list.AddRange(listResult.Items);
				token = listResult.NextPageToken;
				if (listResult.Items.Count != 50)
					break;
			}

			var delete = list.Where(item => !keep.Contains(item.Snippet.ResourceId.VideoId)).ToList();
			if (!delete.Any())
			{
				MessageBox.Show("Nothing found to remove.");
				return;
			}

			if (MessageBox.Show($"Are you sure you want to remove these {delete.Count} items from YouTube?\n\n{string.Join("\n", delete.Select(item => item.Snippet.Title).OrderBy(x => x))}", "Confirm", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
				return;

			foreach (var item in delete)
				await new PlaylistItemsResource.DeleteRequest(service, item.Id).ExecuteAsync();
			MessageBox.Show("Items removed.");
		}

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
				Database.DeleteByIDAsync<Shortcut>(shortcutID).Wait();

			var oldShortcuts = initial.ToDictionary(shortcut => shortcut.ShortcutID);
			var updateShortcuts = ShortcutsList.Where(shortcut => (shortcut.ShortcutID == 0) || (!Helpers.Match(shortcut, oldShortcuts[shortcut.ShortcutID]))).ToList();
			foreach (var shortcut in updateShortcuts)
				Database.AddOrUpdateAsync(shortcut).Wait();

			DialogResult = true;
		}

		static public void Run(NeoPlayerWindow neoPlayerWindow) => new SettingsDialog(neoPlayerWindow).ShowDialog();
	}
}
