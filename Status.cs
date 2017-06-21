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

		public int Position { get; set; }
		public int Max { get; set; }
		public bool Playing { get; set; }
		public string CurrentSong { get; set; }
		public List<SongData> Videos { get; set; }
		public string ImageQuery { get; set; }
		public int SlideshowDelay { get; set; }
	}
}
