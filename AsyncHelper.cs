using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeoRemote
{
	static class AsyncHelper
	{
		const int DownloaderCount = 20;

		static public Task ThreadPoolRunAsync(Action action)
		{
			var tcs = new TaskCompletionSource<object>();
			ThreadPool.QueueUserWorkItem(state =>
			{
				try
				{
					action();
					tcs.SetResult(null);
				}
				catch (Exception ex) { tcs.SetException(ex); }
			});
			return tcs.Task;
		}

		async public static void RunTasks<TInput, TOutput>(AsyncQueue<TInput> input, Func<TInput, Task<TOutput>> func, AsyncQueue<TOutput> output, CancellationToken token)
		{
			var tasks = new HashSet<Task>();
			var isFinished = false;
			Task hasItemsTask = null;
			while (true)
			{
				if ((!isFinished) && (tasks.Count < DownloaderCount) && (!token.IsCancellationRequested))
				{
					hasItemsTask = input.HasItemsAsync(token);
					tasks.Add(hasItemsTask);
				}

				if (!tasks.Any())
					break;

				var finished = await Task.WhenAny(tasks.ToArray());
				tasks.Remove(finished);
				if (finished == hasItemsTask)
				{
					isFinished = !(finished as Task<bool>).Result;
					if (!isFinished)
						tasks.Add(func(input.Dequeue()));
				}
				else
					output.Enqueue((finished as Task<TOutput>).Result);
			}
			output.SetFinished();
		}

		public static void RunTasks<TInput>(AsyncQueue<TInput> input, Func<TInput, Task> func, CancellationToken token)
		{
			var output = new AsyncQueue<bool>();
			RunTasks(input, async item => { await func(item); return false; }, output, token);
		}
	}
}
