using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoRemote
{
	public class AsyncQueue<T>
	{
		readonly Queue<T> queue = new Queue<T>();
		bool finished = false;
		TaskCompletionSource<bool> tcs = null;
		CancellationTokenRegistration ctr;

		async public Task<bool> HasItemsAsync(CancellationToken token)
		{
			if (queue.Count != 0)
				return true;
			if (finished)
				return false;

			tcs = new TaskCompletionSource<bool>();
			ctr = token.Register(() =>
			{
				finished = true;
				SetTaskResult(false);
			});
			return await tcs.Task;
		}

		public void Enqueue(T item)
		{
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

			ctr.Dispose();
			var stored = tcs;
			tcs = null;
			stored.SetResult(result);
		}
	}
}
