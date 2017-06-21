using System;

namespace NeoRemote
{
	[Flags]
	public enum ActionType
	{
		Slides = 1,
		SlideshowSongs = 2,
		Slideshow = Slides | SlideshowSongs,
		Videos = 4,
	}
}
