using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoRemote
{
	public class Actions
	{
		ActionType currentAction = ActionType.Slideshow;
		public ActionType CurrentAction { get { return currentAction; } set { currentAction = value; changed(); } }

		string slidesQuery = Settings.Debug ? "test" : "landscape";
		public string SlidesQuery { get { return slidesQuery; } set { slidesQuery = value; changed(); } }

		string slidesSize = "2mp";
		public string SlidesSize { get { return slidesSize; } set { slidesSize = value; changed(); } }

		public int SlideDisplayTime { get; set; } = 60;
		public bool SlidesPaused { get; set; }
		public bool musicAutoPlay { get; set; } = false;
		public bool MusicAutoPlay { get { return musicAutoPlay; } set { musicAutoPlay = value; changed(); } }

		readonly List<string> slides = new List<string>();
		readonly List<string> music = new List<string>();
		readonly List<string> videos = new List<string>();
		readonly Action changed;

		public Actions(Action changed)
		{
			this.changed = changed;
		}

		int currentSlide = 0;
		public string CurrentSlide => slides.Any() ? slides[currentSlide % slides.Count] : null;
		public string CurrentMusic => music.FirstOrDefault();
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
		public void EnqueueMusic(IEnumerable<string> fileNames, bool enqueue = true) => EnqueueItems(music, fileNames, enqueue);
		public void EnqueueVideos(IEnumerable<string> fileNames, bool enqueue = true) => EnqueueItems(videos, fileNames, enqueue);

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

		public void CycleMusic()
		{
			if (!music.Any())
				return;

			music.Add(music[0]);
			music.RemoveAt(0);

			changed();
		}

		public void CycleVideo()
		{
			if (!videos.Any())
				return;

			videos.RemoveAt(0);
			changed();
		}

		public void ClearSlides()
		{
			if (!slides.Any())
				return;

			slides.Clear();
			currentSlide = 0;
		}
	}
}
