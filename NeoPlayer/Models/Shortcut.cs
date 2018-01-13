namespace NeoPlayer.Models
{
	public class Shortcut
	{
		[PrimaryKey]
		public int ShortcutID { get; set; }
		public string Name { get; set; }
		public string Value { get; set; }
	}
}
