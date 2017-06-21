using System.IO;
using System.Xml.Linq;

namespace NeoRemote
{
	public static class Settings
	{
		public static bool Debug
		{
			get
			{
#if DEBUG
				return true;
#else
				return false;
#endif
			}
		}

		static readonly string SettingsFile = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "Settings.xml");

		static string slidesPath;
		public static string SlidesPath { get { return slidesPath; } set { slidesPath = value; WriteXML(); } }

		static string musicPath;
		public static string MusicPath { get { return musicPath; } set { musicPath = value; WriteXML(); } }

		static string videosPath;
		public static string VideosPath { get { return videosPath; } set { videosPath = value; WriteXML(); } }

		static Settings()
		{
			slidesPath = musicPath = videosPath = Directory.GetCurrentDirectory();
			try
			{
				var xml = XElement.Load(SettingsFile);
				slidesPath = xml.Element(nameof(SlidesPath))?.Value ?? slidesPath;
				musicPath = xml.Element(nameof(MusicPath))?.Value ?? musicPath;
				videosPath = xml.Element(nameof(VideosPath))?.Value ?? videosPath;
			}
			catch { }
		}

		static void WriteXML()
		{
			var xml = new XElement("Settings",
				new XElement(nameof(SlidesPath), SlidesPath),
				new XElement(nameof(MusicPath), MusicPath),
				new XElement(nameof(VideosPath), VideosPath)
			);
			xml.Save(SettingsFile);
		}
	}
}
