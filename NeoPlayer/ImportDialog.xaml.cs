using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using NeoPlayer.Models;

namespace NeoPlayer
{
	partial class ImportDialog
	{
		static DependencyProperty FileNameProperty = DependencyProperty.Register(nameof(FileName), typeof(string), typeof(ImportDialog));
		static DependencyProperty FileTitleProperty = DependencyProperty.Register(nameof(FileTitle), typeof(string), typeof(ImportDialog));

		string FileName { get => (string)GetValue(FileNameProperty); set => SetValue(FileNameProperty, value); }
		string FileTitle { get => (string)GetValue(FileTitleProperty); set => SetValue(FileTitleProperty, value); }

		readonly NeoPlayerWindow neoPlayerWindow;

		ImportDialog(NeoPlayerWindow neoPlayerWindow)
		{
			Owner = this.neoPlayerWindow = neoPlayerWindow;

			InitializeComponent();
			DataContext = this;

			FileName = FileTitle = "";
		}

		void OnBrowseClick(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			if (dialog.ShowDialog() != true)
				return;

			FileName = dialog.FileName;
			FileTitle = Path.GetFileNameWithoutExtension(FileName);
		}

		void OnOKClick(object sender, RoutedEventArgs e)
		{
			if ((string.IsNullOrWhiteSpace(FileName)) || (string.IsNullOrWhiteSpace(FileTitle)))
				return;

			var videoFiles = Database.GetAsync<VideoFile>().Result.ToDictionary(videoFile => videoFile.Identifier);

			var identifier = Guid.NewGuid().ToString().Replace("-", "");
			var copyPath = Path.Combine(Settings.VideosPath, $"{FileTitle}-NEID-{identifier}.{Path.GetExtension(FileName)}");
			File.Copy(FileName, copyPath);

			Database.SaveVideoFileAsync(new VideoFile
			{
				Title = FileTitle,
				FileName = Path.GetFileName(copyPath),
				Identifier = identifier,
				DownloadDate = new FileInfo(FileName).LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
			}).Wait();

			neoPlayerWindow.UpdateVideoFiles();

			DialogResult = true;
		}

		static public void Run(NeoPlayerWindow neoPlayerWindow) => new ImportDialog(neoPlayerWindow).ShowDialog();
	}
}
