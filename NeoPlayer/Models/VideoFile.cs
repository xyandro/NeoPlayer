namespace NeoPlayer.Models
{
	public class VideoFile
	{
		[PrimaryKey]
		public int VideoFileID { get; set; }
		public string Identifier { get; set; }
		public string Title { get; set; }
		public string FileName { get; set; }
		[Ignore]
		public string URL { get; set; }
	}
}
