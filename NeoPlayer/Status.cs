using System.Collections.Generic;

namespace NeoPlayer
{
	class Status
	{
		public class MusicData
		{
			public string Name { get; set; }
			public bool Queued { get; set; }
		}

		public int PlayerPosition { get; set; }
		public int PlayerMax { get; set; }
		public bool PlayerIsPlaying { get; set; }
		public string PlayerTitle { get; set; }

		public List<MusicData> Videos { get; set; }
		public string SlidesQuery { get; set; }
		public string SlidesSize { get; set; }
		public int SlideDisplayTime { get; set; }
		public bool SlidesPaused { get; set; }
	}
}
