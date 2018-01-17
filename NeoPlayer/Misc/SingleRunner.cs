using System;
using System.Windows.Threading;

namespace NeoPlayer.Misc
{
	public class SingleRunner
	{
		DispatcherTimer timer = null;
		Action action;

		public SingleRunner(Action action)
		{
			this.action = action;
		}

		public void Signal()
		{
			if (timer != null)
				return;

			timer = new DispatcherTimer();
			timer.Tick += (s, e) =>
			{
				timer.Stop();
				timer = null;

				action();
			};
			timer.Start();
		}
	}
}
