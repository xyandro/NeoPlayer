namespace NeoPlayer
{
	public class MediaData
	{
		public string Description { get; private set; }
		public string URL { get; private set; }

		public MediaData(string description, string url)
		{
			Description = description;
			URL = url;
		}
	}
}
