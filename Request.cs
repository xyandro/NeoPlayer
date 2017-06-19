namespace NeoMedia
{
	public class Request
	{
		public string URL { get; private set; }
		public string ETag { get; private set; }

		public Request(string url, string eTag)
		{
			URL = url;
			ETag = eTag;
		}
	}
}
