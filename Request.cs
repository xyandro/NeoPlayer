using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NeoPlayer
{
	public class Request
	{
		public string URL { get; private set; }
		public HashSet<string> ETags { get; private set; }
		public Dictionary<string, List<string>> Parameters { get; private set; }

		public Request(string url, HashSet<string> eTags)
		{
			URL = url;
			ETags = eTags;

			var queryIndex = URL.IndexOf('?');
			var query = queryIndex == -1 ? "" : URL.Substring(queryIndex + 1);
			URL = queryIndex == -1 ? URL : URL.Remove(queryIndex);
			var parsed = HttpUtility.ParseQueryString(query);
			Parameters = parsed.AllKeys.ToDictionary(key => key, key => parsed.GetValues(key).ToList());
		}
	}
}
