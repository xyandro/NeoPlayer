using System.Collections.Generic;

namespace NeoRemote
{
	class Status
	{
		public class SongData
		{
			public string Name { get; set; }
			public bool Queued { get; set; }
		}

		public int PlayerPosition { get; set; }
		public int PlayerMax { get; set; }
		public bool PlayerIsPlaying { get; set; }
		public string PlayerCurrentSong { get; set; }

		public List<SongData> Videos { get; set; }
		public string SlidesQuery { get; set; }
		public int SlideDisplayTime { get; set; }
		public bool SlidesPaused { get; set; }
	}
}
