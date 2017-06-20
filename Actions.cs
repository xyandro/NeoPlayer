using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoRemote
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

		void CycleList(List<string> list, bool readd, bool fromStart = true)
		{
			if (!list.Any())
				return;

			var takeIndex = fromStart ? 0 : list.Count - 1;
			var addIndex = fromStart ? list.Count - 1 : 0;

			var item = list[takeIndex];
			list.RemoveAt(takeIndex);
			if (readd)
				list.Insert(addIndex, item);

			changed();
		}

		public void CycleImage(bool fromStart = true) => CycleList(images, true, fromStart);
		public void CycleSong() => CycleList(songs, true);
		public void CycleVideo() => CycleList(videos, false);
	}
}
