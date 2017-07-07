using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace NeoPlayer
{
	public class NetServer
	{
		public delegate void OnMessageDelegate(Message message);
		public delegate void OnConnectDelegate(AsyncQueue<byte[]> queue);
		public event OnMessageDelegate OnMessage;
		public event OnConnectDelegate OnConnect;

		readonly List<AsyncQueue<byte[]>> outputQueues = new List<AsyncQueue<byte[]>>();

		public void Run(int port)
		{
			RunUdpListener(port);
			RunTcpListener(port);
		}

		async void RunUdpListener(int port)
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

		async void RunTcpListener(int port)
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

		async void Reader(TcpClient client, AsyncQueue<byte[]> queue)
		{
			try
			{
				OnConnect?.Invoke(queue);
				var stream = client.GetStream();
				while (true)
				{
					var message = await Message.Read(stream);
					OnMessage?.Invoke(message);
				}
			}
			catch { }
			finally { client.Close(); }
			queue.SetFinished();
		}

		//async public void SearchYouTube(AsyncQueue<byte[]> queue, Message input)
		//{
		//	var search = input.GetString();

		//	var cts = new CancellationTokenSource();
		//	cts.CancelAfter(10000);
		//	var suggestions = await YouTube.GetSuggestions(search, cts.Token);

		//	var message = new Message(Message.MessageCommand.GetYouTube);
		//	message.Add(suggestions);
		//	queue.Enqueue(message.ToArray());
		//}

		async void Writer(TcpClient client, AsyncQueue<byte[]> queue)
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

		public void SendAll(byte[] buffer) => outputQueues.ForEach(queue => queue.Enqueue(buffer));
	}
}
