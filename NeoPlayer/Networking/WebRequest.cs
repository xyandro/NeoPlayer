using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using NeoPlayer.Misc;

namespace NeoPlayer.Networking
{
	class WebRequest
	{
		public int ContentLeft { get; private set; }

		List<string> headersField;
		public List<string> Headers
		{
			get => headersField;
			set
			{
				headersField = value;
				ContentLeft = Headers.Where(header => header.StartsWith("Content-Length: ")).Select(header => header.Substring("Content-Length: ".Length)).Select(header => int.Parse(header)).FirstOrDefault();
			}
		}

		public string HeadersStr
		{
			get => string.Join("", Headers.Select(header => $"{header}\r\n"));
			private set => Headers = Regex.Split(value, @"(?<=\r\n)").Where(line => line.EndsWith("\r\n")).Select(line => line.Remove(line.Length - 2)).ToList();
		}

		private WebRequest() { }

		public static async Task<WebRequest> GetAsync(NeoSocket neoSocket)
		{
			var data = new byte[16384];
			var size = 0;

			while (true)
			{
				if (size == data.Length)
					throw new OutOfMemoryException("Ran out of buffer space");
				var block = await neoSocket.ReadAsync(data, size);
				size += block;

				for (var ctr = 0; ctr <= size - 4; ++ctr)
					if ((data[ctr + 0] == '\r') && (data[ctr + 1] == '\n') && (data[ctr + 2] == '\r') && (data[ctr + 3] == '\n'))
					{
						var headerEnd = ctr + 4;
						var headersStr = Encoding.ASCII.GetString(data, 0, headerEnd);
						neoSocket.PutBack(data, headerEnd, size - headerEnd);

						return new WebRequest { HeadersStr = headersStr };
					}
			}
		}

		public Uri RequestUri
		{
			set
			{
				var host = $"Host: {value.Host}{(value.IsDefaultPort ? "" : $":{value.Port}")}";
				var hostIndex = Headers.IndexOf(header => header.StartsWith("Host: ")).DefaultIfEmpty(-1).First();
				if (hostIndex == -1)
					Headers.Insert(1, host);
				else
					Headers[hostIndex] = host;

				var action = Headers[0].Remove(Headers[0].IndexOf(' '));
				var httpVersion = Headers[0].Substring(Headers[0].LastIndexOf(' ') + 1);
				Headers[0] = $"{action} {value.PathAndQuery} {httpVersion}";
			}

			get
			{
				var host = Headers.Where(header => header.StartsWith("Host: ")).Select(header => header.Substring("Host: ".Length)).DefaultIfEmpty("localhost").First();
				var pathAndQuery = Headers[0].Remove(Headers[0].LastIndexOf(' ')).Substring(Headers[0].IndexOf(' ') + 1);
				return new Uri($"http://{host}{pathAndQuery}");
			}
		}

		public string GetParameter(string name) => HttpUtility.ParseQueryString(RequestUri.Query)[name];

		public async Task<byte[]> ReadBlockAsync(NeoSocket neoSocket)
		{
			var result = await neoSocket.ReadBlockAsync();
			ContentLeft -= result.Length;
			return result;
		}
	}
}
