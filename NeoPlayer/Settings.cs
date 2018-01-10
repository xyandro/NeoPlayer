using System.IO;
using System.Xml.Linq;

namespace NeoPlayer
{
	public static class Settings
	{
		const int DefaultPort = 7399;

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

		static string youTubeDLPath;
		public static string YouTubeDLPath { get { return youTubeDLPath; } set { youTubeDLPath = value; WriteXML(); } }

		static string ffMpegPath;
		public static string FFMpegPath { get { return ffMpegPath; } set { ffMpegPath = value; WriteXML(); } }

		static int port;
		public static int Port { get { return port; } set { port = value; WriteXML(); } }

		static Settings()
		{
			slidesPath = musicPath = videosPath = youTubeDLPath = ffMpegPath = Directory.GetCurrentDirectory();
			port = DefaultPort;
			try
			{
				var xml = XElement.Load(SettingsFile);
				slidesPath = xml.Element(nameof(SlidesPath))?.Value ?? slidesPath;
				musicPath = xml.Element(nameof(MusicPath))?.Value ?? musicPath;
				videosPath = xml.Element(nameof(VideosPath))?.Value ?? videosPath;
				youTubeDLPath = xml.Element(nameof(YouTubeDLPath))?.Value ?? youTubeDLPath;
				ffMpegPath = xml.Element(nameof(FFMpegPath))?.Value ?? ffMpegPath;
				if (!int.TryParse(xml.Element(nameof(Port))?.Value ?? port.ToString(), out port))
					port = DefaultPort;
			}
			catch { }
		}

		static void WriteXML()
		{
			var xml = new XElement("Settings",
				new XElement(nameof(SlidesPath), SlidesPath),
				new XElement(nameof(MusicPath), MusicPath),
				new XElement(nameof(VideosPath), VideosPath),
				new XElement(nameof(YouTubeDLPath), YouTubeDLPath),
				new XElement(nameof(FFMpegPath), FFMpegPath),
				new XElement(nameof(Port), Port)
			);
			xml.Save(SettingsFile);
		}
	}
}
