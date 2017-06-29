using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using NeoPlayer;

namespace NeoPlayerTestClient
{
	public class SocketClient
	{
		static AsyncQueue<byte[]> queue = new AsyncQueue<byte[]>();

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

		public static async void RunSocket()
		{
			while (true)
			{
				var client = new TcpClient();
				try
				{
					await client.ConnectAsync("localhost", 7398);

					RunSender(client);

					queue.Enqueue(new byte[] { 41, 0, 0, 0, 84, 104, 105, 115, 32, 105, 115, 32, 109, 101, 32, 115, 101, 110, 100, 105, 110, 103, 32, 97, 32, 115, 116, 114, 105, 110, 103, 32, 116, 111, 32, 109, 121, 32, 115, 101, 114, 118, 101, 114, 46 });

					var stream = client.GetStream();

					while (true)
					{
						var buffer = await Read(stream, sizeof(int));
						buffer = await Read(stream, BitConverter.ToInt32(buffer, 0));
						buffer = buffer;
					}
				}
				catch { }
				finally { client.Close(); }
				await Task.Delay(1000);
			}
		}

		public static async void RunSender(TcpClient client)
		{
			queue = new AsyncQueue<byte[]>();
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

			queue = null;
		}
	}
}