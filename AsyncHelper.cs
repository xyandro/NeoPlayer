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

		async public static Task<List<TOutput>> RunTasks<TInput, TOutput>(IEnumerable<TInput> input, Func<TInput, Task<TOutput>> func, CancellationToken token)
		{
			var tasks = new HashSet<Task<TOutput>>();
			var results = new List<TOutput>();
			var enumerator = input.GetEnumerator();
			while (true)
			{
				while ((tasks.Count < DownloaderCount) && (!token.IsCancellationRequested) && (enumerator.MoveNext()))
					tasks.Add(func(enumerator.Current));

				if (!tasks.Any())
					break;

				var finished = await Task.WhenAny(tasks.ToArray());
				results.Add(finished.Result);
				tasks.Remove(finished);
			}
			return results;
		}

		async public static Task RunTasks<TInput>(IEnumerable<TInput> input, Func<TInput, Task> func, CancellationToken token) => await RunTasks(input, async item => { await func(item); return false; }, token);
	}
}
