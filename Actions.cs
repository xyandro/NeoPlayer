using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoMedia
{
	public class Actions
	{
		readonly List<string> videos = new List<string>();
		readonly Action changed;

		public Actions(Action changed)
		{
			this.changed = changed;
		}


		public string CurrentVideo => videos.FirstOrDefault();

		public bool IsQueued(string video) => videos.Contains(video);

		public void Enqueue(IEnumerable<string> fileNames, bool enqueue)
		{
			var found = false;
			foreach (var fileName in fileNames)
			{
				var present = videos.Contains(fileName);
				if (present == enqueue)
					continue;

				if (enqueue)
					videos.Add(fileName);
				else
					videos.Remove(fileName);
				found = true;
			}
			if (found)
				changed();
		}

		public void RemoveFirst()
		{
			if (videos.Count == 0)
				return;
			videos.RemoveAt(0);
			changed();
		}
	}
}
