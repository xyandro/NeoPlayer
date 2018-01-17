namespace NeoPlayer.Models
{
	public class Setting
    {
		[PrimaryKey]
		public int SettingID { get; set; }
		public string Name { get; set; }
		public string Value { get; set; }
	}
}
