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


		public string CurrentVideo
		{
			get
			{
				lock (videos)
					return videos.Count == 0 ? null : videos[0];
			}
		}

		public HashSet<string> Queued
		{
			get
			{
				lock (videos)
					return new HashSet<string>(videos);
			}
		}

		public void Enqueue(IEnumerable<string> fileNames, bool enqueue)
		{
			var found = false;
			lock (videos)
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
					found = true;
				}
			}
			if (found)
				changed();
		}

		public void RemoveFirst()
		{
			lock (videos)
			{
				if (videos.Count == 0)
					return;
				videos.RemoveAt(0);
				changed();
			}
		}
	}
}
