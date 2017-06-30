using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

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

		async static void Reader(TcpClient client, AsyncQueue<byte[]> queue)
		{
			try
			{
				var stream = client.GetStream();
				while (true)
				{
					var message = await Message.Read(stream);
					switch (message.Command)
					{
						case Message.MessageCommand.QueueVideo: QueueVideo(message); break;
						case Message.MessageCommand.GetQueue: queue.Enqueue(GetQueue()); break;
						case Message.MessageCommand.GetCool: queue.Enqueue(GetCool()); break;
					}
				}
			}
			catch { }
			finally { client.Close(); }
			queue.SetFinished();
		}

		static void QueueVideo(Message message) => NeoPlayerWindow.Current.EnqueueVideo(message.GetMediaData());

		public static byte[] GetQueue()
		{
			var message = new Message(Message.MessageCommand.GetQueue);
			message.Add(NeoPlayerWindow.Current.QueueVideos.ToList());
			return message.ToArray();
		}

		public static byte[] GetCool()
		{
			var message = new Message(Message.MessageCommand.GetCool);
			message.Add(Directory.EnumerateFiles(Settings.VideosPath).Select(fileName => new MediaData { Description = Path.GetFileNameWithoutExtension(fileName), URL = $"file:///{fileName}" }).ToList());
			return message.ToArray();
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
