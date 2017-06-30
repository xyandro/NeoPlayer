using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace NeoPlayer
{
	public static class NetServer
	{
		public enum NetServerCommand
		{
			None,
			QueueVideo,
			GetQueue,
			GetCool,
		}

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
						case NetServerCommand.QueueVideo: QueueVideo(message); break;
						case NetServerCommand.GetQueue: queue.Enqueue(GetQueue()); break;
						case NetServerCommand.GetCool: queue.Enqueue(GetCool()); break;
					}
				}
			}
			catch { }
			finally { client.Close(); }
			queue.SetFinished();
		}

		static void QueueVideo(Message message)
		{
			var description = message.GetString();
			var url = message.GetString();
			var mediaData = new MediaData(description, url);
			NeoPlayerWindow.Current.EnqueueVideo(mediaData);
		}

		public static byte[] GetQueue()
		{
			var message = new Message(NetServerCommand.GetQueue);
			var videos = NeoPlayerWindow.Current.QueueVideos.ToList();
			message.Add(videos.Count);
			foreach (var video in videos)
			{
				message.Add(video.Description);
				message.Add(video.URL);
			}

			return message.GetBytes();
		}

		public static byte[] GetCool()
		{
			var message = new Message(NetServerCommand.GetCool);
			var videos = NeoPlayerWindow.Current.CoolVideos.ToList();
			message.Add(videos.Count);
			foreach (var video in videos)
			{
				message.Add(video.Description);
				message.Add(video.URL);
			}

			return message.GetBytes();
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
