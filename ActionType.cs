using System;

namespace NeoRemote
{
	[Flags]
	public enum ActionType
	{
		SlideshowImages = 1,
		SlideshowSongs = 2,
		Slideshow = SlideshowImages | SlideshowSongs,
		Videos = 4,
	}
}
