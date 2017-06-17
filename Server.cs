using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace NeoMedia
{
	public static class Server
	{
		public static void Run(int port, Func<string, Result> service) => new Thread(() => RunListener(port, service)).Start();

		static void RunListener(int port, Func<string, Result> service)
		{
			var listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			while (true)
			{
				var client = listener.AcceptTcpClient();
				new Thread(() => RunClient(client, service)).Start();
			}
		}

		static string GetURL(NetworkStream stream)
		{
			var text = "";
			while (true)
			{
				try
				{
					var buffer = new byte[1024];
					var block = stream.Read(buffer, 0, buffer.Length);
					if (block == 0)
						return null;
					text += Encoding.UTF8.GetString(buffer, 0, block);
				}
				catch (Exception ex) when ((ex.InnerException as SocketException)?.ErrorCode == 10054) { return null; }
				catch { text = ""; }

				var lines = Regex.Split(text, @"(?<=\r\n)").Where(line => line.EndsWith("\r\n")).Select(line => line.Remove(line.Length - 2)).ToList();
				if (!lines.Any(line => line == ""))
					continue;

				text = "";

				var url = lines.FirstOrDefault(line => line.StartsWith("GET "))?.Remove(0, 4);
				if (url == null)
					continue;

				var httpVersionIndex = url.LastIndexOf(" HTTP/");
				if (httpVersionIndex != -1)
					url = url.Remove(httpVersionIndex);

				if (url.StartsWith("/"))
					url = url.Substring(1);

				if (url == "")
					url = "Index.html";

				return url;
			}
		}

		static void RunClient(TcpClient client, Func<string, Result> service)
		{
			var stream = client.GetStream();
			while (true)
			{
				try
				{
					var url = GetURL(stream);
					if (url == null)
						break;

					var result = default(Result);
					if (url.StartsWith("service/"))
						result = service(url);

					if (result == null)
						result = Result.CreateFromFile(url);

					result.Send(stream);
				}
				catch (Exception ex) when ((ex.InnerException as SocketException)?.ErrorCode == 10054) { break; }
				catch (Exception ex)
				{
					if (Settings.Debug)
						MessageBox.Show($"Error: {ex.Message}");
				}
			}
			client.Close();
		}
	}
}
