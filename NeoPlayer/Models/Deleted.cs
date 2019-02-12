namespace NeoPlayer.Models
{
	public class Deleted
	{
		[PrimaryKey]
		public int DeletedID { get; set; }
		public string Identifier { get; set; }
	}
}
