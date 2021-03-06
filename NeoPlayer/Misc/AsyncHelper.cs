﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeoPlayer.Misc
{
	static class AsyncHelper
	{
		const int Concurrency = 20;

		static public Task<TOutput> ThreadPoolRunAsync<TOutput>(Func<TOutput> action)
		{
			var tcs = new TaskCompletionSource<TOutput>();
			ThreadPool.QueueUserWorkItem(state =>
			{
				try { tcs.SetResult(action()); }
				catch (Exception ex) { tcs.SetException(ex); }
			});
			return tcs.Task;
		}

		async static public Task ThreadPoolRunAsync(Action action) => await ThreadPoolRunAsync(() => { action(); return false; });

		async public static void RunTasks<TInput, TOutput>(AsyncQueue<TInput> input, Func<TInput, Task<TOutput>> func, AsyncQueue<TOutput> output, CancellationToken token)
		{
			var tasks = new HashSet<Task>();
			var isFinished = false;
			Task hasItemsTask = null;
			while (true)
			{
				if ((hasItemsTask == null) && (!isFinished) && (tasks.Count < Concurrency) && (!token.IsCancellationRequested))
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
					hasItemsTask = null;
				}
				else
					output.Enqueue((finished as Task<TOutput>).Result);
			}
			output.SetFinished();
		}

		async public static Task RunTasks<TInput>(AsyncQueue<TInput> input, Func<TInput, Task> func, CancellationToken token)
		{
			var output = new AsyncQueue<bool>();
			RunTasks(input, async item => { await func(item); return false; }, output, token);
			while (await output.HasItemsAsync(token))
				output.Dequeue();
		}
	}
}
