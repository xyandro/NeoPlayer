using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NeoMedia
{
	public static class Interceptor
	{
		public static void RunInterceptor(int inPort, string outHostName, int outPort, string fileName) => new Thread(() => RunListener(inPort, outHostName, outPort, fileName)).Start();

		static void RunListener(int inPort, string outHostName, int outPort, string fileName)
		{
			var output = File.CreateText(fileName);
			output.AutoFlush = true;

			var listener = new TcpListener(IPAddress.Any, inPort);
			listener.Start();
			var count = 0;
			while (true)
			{
				++count;
				var client = listener.AcceptTcpClient();
				var server = new TcpClient(outHostName, outPort);
				new Thread(() => RunInterceptor(client, server, output, $"{count}-1")).Start();
				new Thread(() => RunInterceptor(server, client, output, $"{count}-2")).Start();
			}
		}

		static void RunInterceptor(TcpClient client, TcpClient server, StreamWriter output, string prefix)
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
		}
	}
}
