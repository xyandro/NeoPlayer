using System;
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

		public static void Run(int port)
		{
			RunUdpListener(port);
			RunTcpListener(port);
		}

		async static void RunUdpListener(int port)
		{
			var client = new UdpClient();
			client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			client.ExclusiveAddressUse = false;
			client.Client.Bind(new IPEndPoint(IPAddress.Any, port));

			while (true)
			{
				var data = await client.ReceiveAsync();
				if ((data.Buffer.Length == 4) && (BitConverter.ToUInt32(data.Buffer, 0) == 0xfeedbeef))
					await client.SendAsync(data.Buffer, data.Buffer.Length, data.RemoteEndPoint);
			}
		}

		async static void RunTcpListener(int port)
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
						case Message.MessageCommand.GetMediaData: RequestMediaData(); break;
						case Message.MessageCommand.GetVolume: queue.Enqueue(GetVolume()); break;
						case Message.MessageCommand.SetVolume: SetVolume(message); break;
						case Message.MessageCommand.GetSlidesData: queue.Enqueue(GetSlidesData()); break;
						case Message.MessageCommand.SetSlidesData: SetSlidesData(message); break;
						case Message.MessageCommand.PauseSlides: PauseSlides(); break;
						case Message.MessageCommand.SetSlideDisplayTime: SetSlideDisplayTime(message); break;
						case Message.MessageCommand.CycleSlide: CycleSlide(message); break;
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

		public static byte[] GetVolume()
		{
			var message = new Message(Message.MessageCommand.GetVolume);
			message.Add(NeoPlayerWindow.Current.Volume);
			return message.ToArray();
		}

		static void SetVolume(Message message)
		{
			var volume = message.GetInt();
			var relative = message.GetBool();
			NeoPlayerWindow.Current.Volume = (relative ? NeoPlayerWindow.Current.Volume : 0) + volume;
		}

		public static byte[] GetSlidesData()
		{
			var message = new Message(Message.MessageCommand.GetSlidesData);
			message.Add(NeoPlayerWindow.Current.SlidesQuery);
			message.Add(NeoPlayerWindow.Current.SlidesSize);
			message.Add(NeoPlayerWindow.Current.SlideDisplayTime);
			message.Add(NeoPlayerWindow.Current.SlidesPaused);
			return message.ToArray();
		}

		static void SetSlidesData(Message message)
		{
			NeoPlayerWindow.Current.SlidesQuery = message.GetString();
			NeoPlayerWindow.Current.SlidesSize = message.GetString();
		}

		static void PauseSlides() => NeoPlayerWindow.Current.SlidesPaused = !NeoPlayerWindow.Current.SlidesPaused;

		static void SetSlideDisplayTime(Message message) => NeoPlayerWindow.Current.SlideDisplayTime = message.GetInt();

		static void CycleSlide(Message message) => NeoPlayerWindow.Current.CycleSlide(message.GetBool());

		public static byte[] MediaData(bool playing, string title, int position, int maxPosition)
		{
			var message = new Message(Message.MessageCommand.GetMediaData);
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
