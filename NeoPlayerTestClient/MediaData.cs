namespace NeoPlayerTestClient
{
	public class MediaData
	{
		public string Description { get; private set; }
		public string URL { get; private set; }
		public bool IsQueued { get; private set; }

		public MediaData(string description, string url, bool isQueued)
		{
			Description = description;
			URL = url;
			IsQueued = isQueued;
		}
	}
}
