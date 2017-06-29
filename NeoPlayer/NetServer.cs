using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NeoPlayer
{
	public static class NetServer
	{
		static List<AsyncQueue<byte[]>> outputQueues = new List<AsyncQueue<byte[]>>();

		async public static void Run(int port)
		{
			var listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			while (true)
			{
				var client = await listener.AcceptTcpClientAsync();
				var queue = new AsyncQueue<byte[]>();
				Reader(client, queue);
				Writer(client, queue);
			}
		}

		async static Task<byte[]> Read(Stream stream, int size)
		{
			var buffer = new byte[size];
			var used = 0;
			while (used < size)
			{
				var block = await stream.ReadAsync(buffer, used, size - used);
				if (block == 0)
					throw new EndOfStreamException();
				used += block;
			}
			return buffer;
		}

		async static void Reader(TcpClient client, AsyncQueue<byte[]> queue)
		{
			try
			{
				var stream = client.GetStream();
				while (true)
				{
					var buffer = await Read(stream, sizeof(int));
					buffer = await Read(stream, BitConverter.ToInt32(buffer, 0));

					var str = Encoding.UTF8.GetString(buffer);
					var response = Encoding.UTF8.GetBytes($"You sent {str}");
					var length = response.Length;
					Array.Resize(ref response, length + 4);
					Array.Copy(response, 0, response, 4, length);
					Array.Copy(BitConverter.GetBytes(length), response, 4);
					queue.Enqueue(response);
				}
			}
			catch { }
			finally { client.Close(); }
			queue.SetFinished();
		}

		async static void Writer(TcpClient client, AsyncQueue<byte[]> queue)
		{
			outputQueues.Add(queue);
			try
			{
				var stream = client.GetStream();
				while (await queue.HasItemsAsync())
				{
					var buffer = queue.Dequeue();
					await stream.WriteAsync(buffer, 0, buffer.Length);
				}
			}
			catch { }
			finally { client.Close(); }
			outputQueues.Remove(queue);
		}

		public static void SendAll(byte[] buffer) => outputQueues.ForEach(queue => queue.Enqueue(buffer));
	}
}
