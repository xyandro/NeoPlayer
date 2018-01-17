using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NeoPlayer.Models;

namespace NeoPlayer
{
	public static class Settings
	{
		const int DefaultPort = 7399;

		static readonly Dictionary<string, Setting> settings;

		public static string SlidesPath { get => GetValue(); set => SetValue(value); }
		public static string MusicPath { get => GetValue(); set => SetValue(value); }
		public static string VideosPath { get => GetValue(); set => SetValue(value); }
		public static string YouTubeDLPath { get => GetValue(); set => SetValue(value); }
		public static string FFMpegPath { get => GetValue(); set => SetValue(value); }
		public static int Port { get => int.Parse(GetValue()); set => SetValue(value.ToString()); }

		static string GetValue([CallerMemberName]string name = "")
		{
			if (!settings.ContainsKey(name))
				return null;
			return settings[name].Value;
		}

		static void SetValue(string value, [CallerMemberName]string name = "")
		{
			if (!settings.ContainsKey(name))
				settings[name] = new Setting { Name = name };
			settings[name].Value = value ?? "";
			Database.AddOrUpdateAsync(settings[name]).Wait();
		}

		static Settings()
		{
			settings = Database.GetAsync<Setting>().Result.ToDictionary(setting => setting.Name, StringComparer.OrdinalIgnoreCase);

			if (string.IsNullOrWhiteSpace(GetValue(nameof(SlidesPath))))
				SlidesPath = Path.Combine(Directory.GetCurrentDirectory(), nameof(SlidesPath));
			if (string.IsNullOrWhiteSpace(GetValue(nameof(MusicPath))))
				MusicPath = Path.Combine(Directory.GetCurrentDirectory(), nameof(MusicPath));
			if (string.IsNullOrWhiteSpace(GetValue(nameof(VideosPath))))
				VideosPath = Path.Combine(Directory.GetCurrentDirectory(), nameof(VideosPath));
			if (!int.TryParse(GetValue(nameof(Port)), out int result))
				Port = DefaultPort;

			Directory.CreateDirectory(SlidesPath);
			Directory.CreateDirectory(MusicPath);
			Directory.CreateDirectory(VideosPath);
		}
	}
}
