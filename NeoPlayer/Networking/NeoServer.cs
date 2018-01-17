using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NeoPlayer.Misc;

namespace NeoPlayer.Networking
{
	public class NeoServer
	{
		public delegate void OnConnectDelegate(AsyncQueue<byte[]> queue);
		public delegate void OnMessageDelegate(Message message, AsyncQueue<byte[]> queue);
		public event OnConnectDelegate OnConnect;
		public event OnMessageDelegate OnMessage;

		readonly HashSet<AsyncQueue<byte[]>> neoRemoteQueues = new HashSet<AsyncQueue<byte[]>>();

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
			TcpListener listener;
			try
			{
				listener = new TcpListener(IPAddress.Any, port);
				listener.Start();
			}
			catch
			{
				MessageBox.Show($"Unable to connect to port {port}");
				Environment.Exit(0);
				return;
			}
			while (true)
			{
				var neoSocket = new NeoSocket(await listener.AcceptTcpClientAsync());
				var queue = new AsyncQueue<byte[]>();
				Reader(neoSocket, queue);
				Writer(neoSocket, queue);
			}
		}

		void SendQueue(AsyncQueue<byte[]> queue, Stream fileStream, string contentType)
		{
			var headers = new List<string>
			{
				"HTTP/1.1 200 OK",
				$"Date: {DateTime.UtcNow:r}",
				$"Content-Type: {contentType}",
				$"Content-Length: {fileStream.Length}",
				"Connection: close",
				"",
			};
			var buffer = Encoding.ASCII.GetBytes(string.Join("", headers.Select(header => $"{header}\r\n")));
			while (true)
			{
				queue.Enqueue(buffer);
				buffer = new byte[65536];
				var block = fileStream.Read(buffer, 0, buffer.Length);
				if (block == 0)
					break;
			}
		}

		void SendIndex(AsyncQueue<byte[]> queue)
		{
			var page = @"<html><head><title>NeoPlayer</title></head><body><a style='font-size: 3vh' href=""NeoRemote.apk"">Download NeoRemote</a></body></html>";
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(page)))
				SendQueue(queue, stream, "text/html");
		}

		void SendAPK(AsyncQueue<byte[]> queue)
		{
			using (var stream = typeof(NeoServer).Assembly.GetManifestResourceStream("NeoPlayer.Networking.NeoRemote.apk"))
				SendQueue(queue, stream, "application/octet-stream");
		}

		void SendFavicon(AsyncQueue<byte[]> queue)
		{
			using (var stream = typeof(NeoServer).Assembly.GetManifestResourceStream("NeoPlayer.Networking.favicon.ico"))
				SendQueue(queue, stream, "image/x-icon");
		}

		async void Reader(NeoSocket neoSocket, AsyncQueue<byte[]> queue)
		{
			try
			{
				while (neoSocket.Connected)
				{
					var webRequest = await WebRequest.GetAsync(neoSocket);

					string localPath = webRequest.RequestUri.LocalPath;
					switch (localPath)
					{
						case "/": SendIndex(queue); break;
						case "/NeoRemote.apk": SendAPK(queue); break;
						case "/favicon.ico": SendFavicon(queue); break;
						case "/RunNeoRemote": await RunNeoRemote(neoSocket, queue); break;
						default: throw new Exception("Invalid query");
					}
				}
			}
			catch { }
			neoSocket.Close();
			queue.SetFinished();
		}

		async Task RunNeoRemote(NeoSocket neoSocket, AsyncQueue<byte[]> queue)
		{
			neoRemoteQueues.Add(queue);
			try
			{
				OnConnect?.Invoke(queue);
				while (true)
				{
					var message = await Message.GetAsync(neoSocket);
					OnMessage?.Invoke(message, queue);
				}
			}
			finally { neoRemoteQueues.Remove(queue); }
		}

		async void Writer(NeoSocket neoSocket, AsyncQueue<byte[]> queue)
		{
			try
			{
				while (await queue.HasItemsAsync())
				{
					var buffer = queue.Dequeue();
					await neoSocket.WriteAsync(buffer, 0, buffer.Length);
				}
			}
			catch { queue.SetFinished(); }
			finally { neoSocket.Close(); }
		}

		public void SendToNeoRemotes(byte[] buffer) => neoRemoteQueues.ForEach(queue => queue.Enqueue(buffer));
	}
}
