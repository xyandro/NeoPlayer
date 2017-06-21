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

		static string slideShowSongsPath;
		public static string SlideShowSongsPath { get { return slideShowSongsPath; } set { slideShowSongsPath = value; WriteXML(); } }

		static string videosPath;
		public static string VideosPath { get { return videosPath; } set { videosPath = value; WriteXML(); } }

		static Settings()
		{
			slidesPath = slideShowSongsPath = videosPath = Directory.GetCurrentDirectory();
			try
			{
				var xml = XElement.Load(SettingsFile);
				slidesPath = xml.Element(nameof(SlidesPath))?.Value ?? slidesPath;
				slideShowSongsPath = xml.Element(nameof(SlideShowSongsPath))?.Value ?? slideShowSongsPath;
				videosPath = xml.Element(nameof(VideosPath))?.Value ?? videosPath;
			}
			catch { }
		}

		static void WriteXML()
		{
			var xml = new XElement("Settings",
				new XElement(nameof(SlidesPath), SlidesPath),
				new XElement(nameof(SlideShowSongsPath), SlideShowSongsPath),
				new XElement(nameof(VideosPath), VideosPath)
			);
			xml.Save(SettingsFile);
		}
	}
}
