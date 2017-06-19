using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoMedia
{
	public class Actions
	{
		ActionType actionType = ActionType.Videos;
		public ActionType CurrentAction { get { return actionType; } set { actionType = value; changed(); } }

		readonly List<string> images = new List<string>();
		readonly List<string> songs = new List<string>();
		readonly List<string> videos = new List<string>();
		readonly Action changed;

		public Actions(Action changed)
		{
			this.changed = changed;
		}


		public string CurrentImage => images.FirstOrDefault();
		public string CurrentSong => songs.FirstOrDefault();
		public string CurrentVideo => videos.FirstOrDefault();

		public bool VideoIsQueued(string video) => videos.Contains(video);

		void EnqueueItems(List<string> list, IEnumerable<string> items, bool enqueue)
		{
			var found = false;
			foreach (var fileName in items)
			{
				var present = list.Contains(fileName);
				if (present == enqueue)
					continue;

				if (enqueue)
					list.Add(fileName);
				else
					list.Remove(fileName);
				found = true;
			}
			if (found)
				changed();
		}

		public void EnqueueImages(IEnumerable<string> fileNames, bool enqueue = true) => EnqueueItems(images, fileNames, enqueue);
		public void EnqueueSongs(IEnumerable<string> fileNames, bool enqueue = true) => EnqueueItems(songs, fileNames, enqueue);
		public void EnqueueVideos(IEnumerable<string> fileNames, bool enqueue = true) => EnqueueItems(videos, fileNames, enqueue);

		void CycleList(List<string> list, bool addToEnd)
		{
			if (!list.Any())
				return;

			var first = list[0];
			list.RemoveAt(0);
			if (addToEnd)
				list.Add(first);
			changed();
		}

		public void CycleImage() => CycleList(images, true);
		public void CycleSong() => CycleList(songs, true);
		public void CycleVideo() => CycleList(videos, false);
	}
}
