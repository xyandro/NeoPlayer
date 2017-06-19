using System;
using System.Collections.Generic;
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
		public static void Run(int port, Func<string, Response> service) => new Thread(() => RunListener(port, service)).Start();

		static void RunListener(int port, Func<string, Response> service)
		{
			var listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			while (true)
			{
				var client = listener.AcceptTcpClient();
				new Thread(() => RunClient(client, service)).Start();
			}
		}

		static Request GetRequest(NetworkStream stream)
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
				catch { return null; }

				var lines = Regex.Split(text, @"(?<=\r\n)").Where(line => line.EndsWith("\r\n")).Select(line => line.Remove(line.Length - 2)).ToList();
				if (!lines.Any(line => line == ""))
					continue;

				var url = lines.FirstOrDefault(line => line.StartsWith("GET "))?.Remove(0, "GET ".Length);
				if (url == null)
					continue;

				var httpVersionIndex = url.LastIndexOf(" HTTP/");
				if (httpVersionIndex != -1)
					url = url.Remove(httpVersionIndex);

				if (url.StartsWith("/"))
					url = url.Substring(1);

				if (url == "")
					url = "Index.html";

				var eTagsHeader = lines.FirstOrDefault(line => line.StartsWith("If-None-Match: "))?.Remove(0, "If-None-Match: ".Length) ?? "";
				var match = Regex.Match(eTagsHeader, @"^(?:[^""]*""([^""]*)""[^""]*)*$");
				var eTags = match.Success ? new HashSet<string>(match.Groups[1].Captures.Cast<Capture>().Select(capture => capture.Value).Where(str => !string.IsNullOrWhiteSpace(str))) : default(HashSet<string>);

				return new Request(url, eTags);
			}
		}

		static void RunClient(TcpClient client, Func<string, Response> service)
		{
			var stream = client.GetStream();
			while (true)
			{
				try
				{
					var request = GetRequest(stream);
					if (request == null)
						break;

					var result = default(Response);
					if (request.URL.StartsWith("service/"))
						result = service(request.URL);

					if (result == null)
						result = Response.CreateFromFile(request.URL, request.ETags);

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
