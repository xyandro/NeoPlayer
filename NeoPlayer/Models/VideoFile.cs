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
		public bool AudioOnly
		{
			get
			{
				if ((!Tags.ContainsKey(nameof(AudioOnly))) || (string.IsNullOrEmpty(Tags[nameof(AudioOnly)])))
					return false;
				if (!bool.TryParse(Tags[nameof(AudioOnly)], out var result))
					return false;
				return result;
			}
			set => Tags[nameof(AudioOnly)] = value.ToString();
		}
		[Ignore]
		public Dictionary<string, string> Tags { get; } = new Dictionary<string, string>();
	}
}
