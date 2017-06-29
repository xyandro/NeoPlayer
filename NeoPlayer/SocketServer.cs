using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace NeoPlayer
{
	public static class SocketServer
	{
		async public static void Run(int port)
		{
			var listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			while (true)
			{
				var client = await listener.AcceptTcpClientAsync();
				RunClient(client);
			}
		}

		async static Task<byte[]> Read(Stream stream, int size)
		{
			var buffer = new byte[size];
			var used = 0;
			while (used < size)
			{
				var block = await stream.ReadAsync(buffer, used, size - used);
				if (block == 0)
					throw new EndOfStreamException();
				used += block;
			}
			return buffer;
		}

		async static void RunClient(TcpClient client)
		{
			try
			{
				var stream = client.GetStream();
				while (true)
				{
					var buffer = await Read(stream, sizeof(int));
					var size = BitConverter.ToInt32(buffer, 0);
					buffer = await Read(stream, size);

					var reply = new byte[] { 71, 0, 0, 0, 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 117, 112, 101, 114, 32, 97, 119, 101, 115, 111, 109, 101, 32, 97, 109, 97, 122, 105, 110, 103, 32, 115, 116, 114, 105, 110, 103, 32, 116, 104, 97, 116, 32, 73, 32, 106, 117, 115, 116, 32, 109, 97, 100, 101, 32, 117, 112, 46, 32, 78, 101, 97, 116, 44, 32, 104, 117, 104, 63 };
					stream.Write(reply, 0, reply.Length);
				}
			}
			catch { }
			finally { client.Close(); }
		}
	}
}
