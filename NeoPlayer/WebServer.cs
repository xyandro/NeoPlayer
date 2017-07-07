using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NeoPlayer
{
	class WebServer
	{
		public async static void Run(int port)
		{
			var listener = new TcpListener(IPAddress.Loopback, port);
			listener.Start();
			while (true)
			{
				var client = await listener.AcceptTcpClientAsync();
				RunClient(client);
			}
		}

		static List<string> GetHeaders(byte[] data, int size)
		{
			var headerEnd = GetHeaderEnd(data, size);
			if (!headerEnd.HasValue)
				return null;

			var text = Encoding.ASCII.GetString(data, 0, headerEnd.Value);
			return Regex.Split(text, @"(?<=\r\n)").Where(line => line.EndsWith("\r\n")).Select(line => line.Remove(line.Length - 2)).ToList();
		}

		static int? GetHeaderEnd(byte[] data, int size)
		{
			for (var ctr = 0; ctr <= size - 4; ++ctr)
				if ((data[ctr + 0] == '\r') && (data[ctr + 1] == '\n') && (data[ctr + 2] == '\r') && (data[ctr + 3] == '\n'))
					return ctr + 4;
			return null;
		}

		async static Task HandleFetch(WebRequest webRequest, Stream localStream)
		{
			TcpClient remote = null;
			Stream remoteStream = null;
			string remoteHost = null;
			int remotePort = 0;
			bool newRemote = true;

			try
			{
				var url = webRequest.GetParameter("url");
				url = await YouTube.GetURLAsync(url);
				while (true)
				{
					var uri = new Uri(url);
					if ((newRemote) || (remote == null) || (uri.Host != remoteHost) || (uri.Port != remotePort))
					{
						newRemote = false;
						remote?.Close();

						remoteHost = uri.Host;
						remotePort = uri.Port;
						remote = new TcpClient();
						await remote.ConnectAsync(uri.Host, uri.Port);

						remoteStream = remote.GetStream() as Stream;
						if (uri.Scheme == "https")
						{
							remoteStream = new SslStream(remoteStream);
							(remoteStream as SslStream).AuthenticateAsClient(uri.Host);
						}
					}

					webRequest.RequestUri = uri;
					var buffer = Encoding.ASCII.GetBytes(webRequest.HeadersStr);
					await remoteStream.WriteAsync(buffer, 0, buffer.Length);

					var reply = new WebRequest();
					await reply.FetchHeadersAsync(remoteStream);
					if (reply.Close)
						newRemote = true;

					if (reply.Headers.Any(header => header.EndsWith(" 302 Found")))
					{
						url = reply.Headers.Where(header => header.StartsWith("Location: ")).Select(header => header.Substring("Location: ".Length)).Single();
						continue;
					}

					var queue = new AsyncQueue<byte[]>();
					queue.Enqueue(Encoding.ASCII.GetBytes(reply.HeadersStr));
					((Action)(async () =>
					{
						try
						{
							while (await queue.HasItemsAsync())
							{
								var data = queue.Dequeue();
								await localStream.WriteAsync(data, 0, data.Length);
							}
						}
						catch { }
					}))();

					try
					{
						while (reply.ContentLeft > 0)
							queue.Enqueue(await reply.ReadAsync(remoteStream));
					}
					finally { queue.SetFinished(); }

					break;
				}
			}
			finally
			{
				remote?.Close();
				remote = null;
			}
		}

		async static void RunClient(TcpClient local)
		{
			var localStream = local.GetStream() as Stream;
			try
			{
				while (local.Connected)
				{
					var webRequest = new WebRequest();
					await webRequest.FetchHeadersAsync(localStream);

					string localPath = webRequest.RequestUri.LocalPath;
					switch (localPath)
					{
						case "/fetch": await HandleFetch(webRequest, localStream); break;
					}
				}
			}
			catch { }
			local.Close();
		}
	}
}
