using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace NeoMedia
{
	public class Server
	{
		void Test()
		{
			var output = File.CreateText(@"C:\Dev\NeoMedia\Data.txt");
			output.AutoFlush = true;
			var listener = new TcpListener(IPAddress.Any, 5555);
			listener.Start();
			var count = 0;
			while (true)
			{
				++count;
				var client = listener.AcceptTcpClient();
				var server = new TcpClient("localhost", 1234);
				new Thread(() => RunInterceptor(client, server, output, $"{count}-1")).Start();
				new Thread(() => RunInterceptor(server, client, output, $"{count}-2")).Start();
			}
		}

		void RunInterceptor(TcpClient client, TcpClient server, StreamWriter output, string prefix)
		{
			var buffer = new byte[1048576];
			var clientStream = client.GetStream();
			var serverStream = server.GetStream();
			while (client.Connected)
			{
				var block = clientStream.Read(buffer, 0, buffer.Length);
				lock (output)
					output.WriteLine($"{prefix}: {Convert.ToBase64String(buffer, 0, block)}");
				serverStream.Write(buffer, 0, block);
			}
			client = client;
		}

		public Server(int port)
		{
			new Thread(Test).Start();
			new Thread(() => RunListener(port)).Start();
		}

		void RunListener(int port)
		{
			var listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			while (true)
			{
				var client = listener.AcceptTcpClient();
				new Thread(() => RunClient(client)).Start();
			}
		}

		void RunClient(TcpClient client)
		{
			try
			{
				var stream = client.GetStream();
				var text = "";
				var buffer = new byte[1024];
				while (true)
				{
					var block = stream.Read(buffer, 0, buffer.Length);
					text += Encoding.UTF8.GetString(buffer, 0, block);

					var lines = Regex.Split(text, @"(?<=\r\n)").Where(line => line.EndsWith("\r\n")).Select(line => line.Remove(line.Length - 2)).ToList();
					if (!lines.Any(line => line == ""))
						continue;

					var url = lines.FirstOrDefault(line => line.StartsWith("GET "))?.Remove(0, 4);
					if (url == null)
						continue;

					var httpVersionIndex = url.LastIndexOf(" HTTP/");
					if (httpVersionIndex != -1)
						url = url.Remove(httpVersionIndex);

					if (url == "/")
						url = "/Index.html";

					var file = $@"C:\TFS\SWENG\TabletPick\Releases\TabletPick 1.0.30\Main\TabletPick{url.Replace("/", "\\")}";

					var data = File.ReadAllBytes(file);

					var hash = BitConverter.ToString(SHA1.Create().ComputeHash(data)).Replace("-", "").ToLowerInvariant();

					using (var ms = new MemoryStream())
					{
						using (var gz = new GZipStream(ms, CompressionLevel.Optimal, true))
							gz.Write(data, 0, data.Length);
						data = ms.ToArray();
					}

					var response = new string[]
					{
						"HTTP/1.1 200 OK",
						"Content-Type: text/html",
						"Content-Encoding: gzip",
						$"Last-Modified: {DateTime.UtcNow.ToString("r")}",
						"Accept-Ranges: bytes",
						$"ETag: \"{hash}\"",
						"Vary: Accept-Encoding",
						"Server: Microsoft-IIS/10.0",
						$"Date: {DateTime.UtcNow.ToString("r")}",
						$"Content-Length: {data.Length}",
					};

					var output = Encoding.UTF8.GetBytes(string.Join("\r\n", response) + "\r\n\r\n");

					stream.Write(output, 0, output.Length);
					stream.Write(data, 0, data.Length);
				}
			}
			catch { }
			client.Close();
		}
	}
}
