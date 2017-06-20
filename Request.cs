using System.Collections.Generic;

namespace NeoRemote
{
	public class Request
	{
		public string URL { get; private set; }
		public HashSet<string> ETags { get; private set; }

		public Request(string url, HashSet<string> eTags)
		{
			URL = url;
			ETags = eTags;
		}
	}
}
