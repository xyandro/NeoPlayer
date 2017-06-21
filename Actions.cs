using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoRemote
{
	public class Actions
	{
		ActionType currentAction = ActionType.Videos;
		public ActionType CurrentAction { get { return currentAction; } set { currentAction = value; changed(); } }

		string imageQuery = "landscape";
		public string ImageQuery { get { return imageQuery; } set { imageQuery = value; changed(); } }

		int slideshowDisplayTime = 60;
		public int SlideshowDisplayTime { get { return slideshowDisplayTime; } set { slideshowDisplayTime = value; changed(); } }

		readonly List<string> images = new List<string>();
		readonly List<string> songs = new List<string>();
		readonly List<string> videos = new List<string>();
		readonly Action changed;

		public Actions(Action changed)
		{
			this.changed = changed;
		}

		int currentImage = 0;

		public string CurrentImage => images.Any() ? images[currentImage % images.Count] : null;
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

		public void CycleImage(bool fromStart = true)
		{
			if (!images.Any())
				return;

			currentImage = Math.Max(0, Math.Min(currentImage, images.Count - 1));
			currentImage += (fromStart ? 1 : -1);
			while (currentImage < 0)
				currentImage += images.Count;
			while (currentImage >= images.Count)
				currentImage -= images.Count;
			changed();
		}
		public void CycleSong() => CycleList(songs, true);
		public void CycleVideo() => CycleList(videos, false);

		void ClearList(List<string> list)
		{
			if (!list.Any())
				return;
			list.Clear();
			changed();
		}

		public void ClearImages() { ClearList(images); currentImage = 0; }
		public void ClearSongs() => ClearList(songs);
		public void ClearVideos() => ClearList(videos);
	}
}
