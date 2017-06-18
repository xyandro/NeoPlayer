using System;
using System.Collections.Generic;

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


		public string CurrentVideo => videos.Count == 0 ? null : videos[0];

		public bool IsQueued(string fileName) => videos.Contains(fileName);

		public void Enqueue(IEnumerable<string> fileNames, bool enqueue)
		{
			foreach (var fileName in fileNames)
			{
				var present = videos.Contains(fileName);
				if (present == enqueue)
					continue;

				if (enqueue)
					videos.Add(fileName);
				else
					videos.Remove(fileName);
				changed();
			}
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
