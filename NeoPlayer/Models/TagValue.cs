namespace NeoPlayer.Models
{
	public class TagValue
	{
		[PrimaryKey]
		public int TagValueID { get; set; }
		public int VideoFileID { get; set; }
		public int TagID { get; set; }
		public string Value { get; set; }
	}
}
