using System.IO;

namespace NeoPlayer.Models
{
	public class VideoFile
	{
		private const int BLOCKSIZE = 65536;

		[PrimaryKey]
		public int VideoFileID { get; set; }
		public string Identifier { get; set; }
		public string Title { get; set; }
		[Ignore]
		public string URL { get; set; }

		public string GetSanitizedTitle() => string.Concat(Title.Split(Path.GetInvalidFileNameChars()));

	}
}
