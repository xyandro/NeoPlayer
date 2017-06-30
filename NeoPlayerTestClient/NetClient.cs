using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NeoPlayer;

namespace NeoPlayerTestClient
{
	public class NetClient
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
					await client.ConnectAsync("localhost", 7399);

					RunSender(client);

					RequestQueued();

					var stream = client.GetStream();

					while (true)
					{
						var buffer = await Read(stream, sizeof(int));
						buffer = await Read(stream, BitConverter.ToInt32(buffer, 0) - 4);

						using (var ms = new MemoryStream(buffer))
						using (var reader = new BinaryReader(ms))
						{
							var command = (NetServerCommand)reader.ReadInt32();
							switch (command)
							{
								case NetServerCommand.Queued: SetQueued(reader); break;
							}
						}
					}
				}
				catch { }
				finally { client.Close(); }
				await Task.Delay(1000);
			}
		}

		static string GetString(BinaryReader reader)
		{
			var length = reader.ReadInt32();
			var bytes = new byte[length];
			reader.Read(bytes, 0, bytes.Length);
			return Encoding.UTF8.GetString(bytes);
		}

		static void SetQueued(BinaryReader reader)
		{
			var count = reader.ReadInt32();
			var mediaData = new List<MediaData>();
			for (var ctr = 0; ctr < count; ++ctr)
			{
				var description = GetString(reader);
				var url = GetString(reader);
				mediaData.Add(new MediaData(description, url));
			}
			MainWindow.Current.SetQueued(mediaData);
		}

		static void RequestQueued() => queue.Enqueue(new Message(NetServerCommand.Queued).Bytes);

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