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

		public static bool CheckDisk
		{
			get
			{
				return true;
			}
		}

		static readonly string SettingsFile = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "Settings.xml");

		static string moviesPath;
		public static string MoviesPath { get { return moviesPath; } set { moviesPath = value; WriteXML(); } }

		static string slideShowSongsPath;
		public static string SlideShowSongsPath { get { return slideShowSongsPath; } set { slideShowSongsPath = value; WriteXML(); } }

		static Settings()
		{
			try
			{
				var xml = XElement.Load(SettingsFile);
				MoviesPath = xml.Element(nameof(MoviesPath))?.Value;
				SlideShowSongsPath = xml.Element(nameof(SlideShowSongsPath))?.Value;
			}
			catch { }
		}

		static void WriteXML()
		{
			var xml = new XElement("Settings",
				new XElement(nameof(MoviesPath), MoviesPath),
				new XElement(nameof(SlideShowSongsPath), SlideShowSongsPath)
			);
			xml.Save(SettingsFile);
		}
	}
}
