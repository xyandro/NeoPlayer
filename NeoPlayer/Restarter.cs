using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace NeoPlayer
{
	static class Restarter
	{
		static public void Start(int port)
		{
			new Thread(() => RestarterThread(port)).Start();
		}

		static void RestarterThread(int port)
		{
			var listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			while (true)
			{
				using (var client = listener.AcceptTcpClient())
					try
					{
						using (var stream = client.GetStream())
						using (var reader = new BinaryReader(stream))
						{
							var input = reader.ReadInt32();
							if (input == 0x0badf00d)
								Restart();
						}
					}
					catch { }
					finally { client.Close(); }
			}
		}

		static void Restart()
		{
			var start = Assembly.GetEntryAssembly().Location;
			var newFile = $@"{Path.GetDirectoryName(start)}\{Path.GetFileNameWithoutExtension(start)}-restart{Path.GetExtension(start)}";
			File.Copy(start, newFile, true);
			Process.Start(newFile, $@"-restart {Process.GetCurrentProcess().Id} ""{start}""");
			Environment.Exit(0);
		}

		public static void CheckRestart(string[] args)
		{
			if ((args.Length != 3) || (args[0] != "-restart"))
				return;

			Process.GetProcessById(int.Parse(args[1]))?.WaitForExit();
			Process.Start(args[2]);
			Environment.Exit(0);
		}
	}
}
