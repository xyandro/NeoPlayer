using System.Collections.Generic;

namespace NeoPlayer.Models
{
	public class EditTags
	{
		public List<int> VideoFileIDs { get; set; }
		public Dictionary<string, string> Tags { get; set; }
	}
}
