using System.IO;
using System.Xml.Linq;

namespace NeoMedia
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

		static string videosPath;
		public static string VideosPath { get { return videosPath; } set { videosPath = value; WriteXML(); } }

		static string slideShowSongsPath;
		public static string SlideShowSongsPath { get { return slideShowSongsPath; } set { slideShowSongsPath = value; WriteXML(); } }

		static Settings()
		{
			try
			{
				var xml = XElement.Load(SettingsFile);
				VideosPath = xml.Element(nameof(VideosPath))?.Value;
				SlideShowSongsPath = xml.Element(nameof(SlideShowSongsPath))?.Value;
			}
			catch { }
		}

		static void WriteXML()
		{
			var xml = new XElement("Settings",
				new XElement(nameof(VideosPath), VideosPath),
				new XElement(nameof(SlideShowSongsPath), SlideShowSongsPath)
			);
			xml.Save(SettingsFile);
		}
	}
}
