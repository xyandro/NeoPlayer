using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace NeoRemote
{
	public static class Interceptor
	{
		async public static void Run(int inPort, string outHostName, int outPort, string fileName)
		{
			var output = File.CreateText(fileName);
			output.AutoFlush = true;

			var listener = new TcpListener(IPAddress.Any, inPort);
			listener.Start();
			var count = 0;
			while (true)
			{
				++count;
				var client = await listener.AcceptTcpClientAsync();
				var server = new TcpClient();
				await server.ConnectAsync(outHostName, outPort);
				RunInterceptor(client, server, output, $"{count}-1");
				RunInterceptor(server, client, output, $"{count}-2");
			}
		}

		async static void RunInterceptor(TcpClient client, TcpClient server, StreamWriter output, string prefix)
		{
			var clientStream = client.GetStream();
			var serverStream = server.GetStream();
			try
			{
				var buffer = new byte[1048576];
				while (client.Connected)
				{
					var block = await clientStream.ReadAsync(buffer, 0, buffer.Length);
					if (block == 0)
						continue;
					output.WriteLine($"{prefix}: {Convert.ToBase64String(buffer, 0, block)}");
					await serverStream.WriteAsync(buffer, 0, block);
				}
			}
			catch { }
			serverStream.Close();
			server.Close();
			clientStream.Close();
			client.Close();
		}
	}
}
