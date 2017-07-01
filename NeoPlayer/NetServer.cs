using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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
						case Message.MessageCommand.GetYouTube: SearchYouTube(queue, message); break;
						case Message.MessageCommand.SetPosition: SetPosition(message); break;
						case Message.MessageCommand.Play: Play(); break;
						case Message.MessageCommand.Forward: Forward(); break;
						case Message.MessageCommand.MediaData: RequestMediaData(); break;
					}
				}
			}
			catch { }
			finally { client.Close(); }
			queue.SetFinished();
		}

		async static void QueueVideo(Message message)
		{
			var mediaData = message.GetMediaData();
			mediaData.URL = await YouTube.GetURL(mediaData.URL);
			if (!string.IsNullOrWhiteSpace(mediaData.URL))
				NeoPlayerWindow.Current.EnqueueVideo(mediaData);
		}

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

		async public static void SearchYouTube(AsyncQueue<byte[]> queue, Message input)
		{
			var search = input.GetString();

			var cts = new CancellationTokenSource();
			cts.CancelAfter(10000);
			var suggestions = await YouTube.GetSuggestions(search, cts.Token);

			var message = new Message(Message.MessageCommand.GetYouTube);
			message.Add(suggestions);
			queue.Enqueue(message.ToArray());
		}

		static void SetPosition(Message message) => NeoPlayerWindow.Current.SetPosition((int)message.GetInt(), message.GetBool());

		static void Play() => NeoPlayerWindow.Current.Play();

		static void Forward() => NeoPlayerWindow.Current.Forward();

		static void RequestMediaData() => NeoPlayerWindow.Current.QueueMediaDataUpdate();

		public static byte[] MediaData(bool playing, string title, int position, int maxPosition)
		{
			var message = new Message(Message.MessageCommand.MediaData);
			message.Add(playing);
			message.Add(title);
			message.Add(position);
			message.Add(maxPosition);
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
