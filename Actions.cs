using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoRemote
{
	public class Actions
	{
		ActionType currentAction = ActionType.Slideshow;
		public ActionType CurrentAction { get { return currentAction; } set { currentAction = value; changed(); } }

		string slidesQuery = "landscape";
		public string SlidesQuery { get { return slidesQuery; } set { slidesQuery = value; changed(); } }

		public int SlideDisplayTime { get; set; } = 60;
		public bool SlidesPaused { get; set; }
		public bool SlideMusicAutoPlay { get; set; } = false;

		readonly List<string> slides = new List<string>();
		readonly List<string> songs = new List<string>();
		readonly List<string> videos = new List<string>();
		readonly Action changed;

		public Actions(Action changed)
		{
			this.changed = changed;
		}

		int currentSlide = 0;

		public string CurrentSlide => slides.Any() ? slides[currentSlide % slides.Count] : null;
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

		public void EnqueueSlides(IEnumerable<string> fileNames, bool enqueue = true) => EnqueueItems(slides, fileNames, enqueue);
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

		public void CycleSlide(bool fromStart = true)
		{
			if (!slides.Any())
				return;

			currentSlide = Math.Max(0, Math.Min(currentSlide, slides.Count - 1));
			currentSlide += (fromStart ? 1 : -1);
			while (currentSlide < 0)
				currentSlide += slides.Count;
			while (currentSlide >= slides.Count)
				currentSlide -= slides.Count;
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

		public void ClearSlides() { ClearList(slides); currentSlide = 0; }
		public void ClearSongs() => ClearList(songs);
		public void ClearVideos() => ClearList(videos);
	}
}
