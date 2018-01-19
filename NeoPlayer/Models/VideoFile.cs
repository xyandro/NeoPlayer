using System.Collections.Generic;

namespace NeoPlayer.Models
{
	public class VideoFile
	{
		[PrimaryKey]
		public int VideoFileID { get; set; }
		public string Identifier { get; set; }
		public string FileName { get; set; }
		[Ignore]
		public string Title { get => Tags[nameof(Title)]; set => Tags[nameof(Title)] = value; }
		[Ignore]
		public string DownloadDate { get => Tags[nameof(DownloadDate)]; set => Tags[nameof(DownloadDate)] = value; }
		[Ignore]
		public string URL { get; set; }
		[Ignore]
		public Dictionary<string, string> Tags { get; } = new Dictionary<string, string>();
	}
}
