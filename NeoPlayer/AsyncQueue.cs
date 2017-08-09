using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoPlayer
{
	public class AsyncQueue<T>
	{
		readonly Queue<T> queue = new Queue<T>();
		bool finished = false;
		TaskCompletionSource<bool> tcs = null;
		CancellationTokenRegistration? ctr;

		async public Task<bool> HasItemsAsync(CancellationToken? token = null)
		{
			if (queue.Count != 0)
				return true;
			if (finished)
				return false;

			tcs = new TaskCompletionSource<bool>();
			ctr = null;
			if (token.HasValue)
			{
				ctr = token.Value.Register(() =>
				{
					finished = true;
					SetTaskResult(false);
				});
			}
			return await tcs.Task;
		}

		public void Enqueue(T item)
		{
			if (finished)
				throw new Exception("Cannot enqueue in finished queue");
			queue.Enqueue(item);
			SetTaskResult(true);
		}

		public T Dequeue() => queue.Dequeue();

		public void SetFinished()
		{
			finished = true;
			SetTaskResult(false);
		}

		void SetTaskResult(bool result)
		{
			if (tcs == null)
				return;

			ctr?.Dispose();
			ctr = null;
			var stored = tcs;
			tcs = null;
			stored.SetResult(result);
		}
	}
}
