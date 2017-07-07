using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace NeoPlayer
{
	class WebRequest
	{
		public byte[] Data { get; set; }
		public int ContentLeft { get; set; }
		public bool Close { get; set; }

		List<string> headersField;
		public List<string> Headers
		{
			get => headersField;
			set
			{
				headersField = value;
				ContentLeft = Headers.Where(header => header.StartsWith("Content-Length: ")).Select(header => header.Substring("Content-Length: ".Length)).Select(header => int.Parse(header)).FirstOrDefault();
				Close = Headers.Any(header => header == "Connection: close");
			}
		}

		public string HeadersStr
		{
			get => string.Join("", Headers.Select(header => $"{header}\r\n"));
			set => Headers = Regex.Split(value, @"(?<=\r\n)").Where(line => line.EndsWith("\r\n")).Select(line => line.Remove(line.Length - 2)).ToList();
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
				var host = Headers.Where(header => header.StartsWith("Host: ")).Select(header => header.Substring("Host: ".Length)).FirstOrDefault();
				var pathAndQuery = Headers[0].Remove(Headers[0].LastIndexOf(' ')).Substring(Headers[0].IndexOf(' ') + 1);
				return new Uri($"http://{host}{pathAndQuery}");
			}
		}

		public string GetParameter(string name) => HttpUtility.ParseQueryString(RequestUri.Query)[name];

		public async Task FetchHeadersAsync(Stream stream)
		{
			var data = new byte[16384];
			var size = 0;
			while (true)
			{
				if (size == data.Length)
					throw new OutOfMemoryException("Ran out of buffer space");
				var block = await stream.ReadAsync(data, size, data.Length - size);
				if (block == 0)
					throw new EndOfStreamException();
				size += block;

				for (var ctr = 0; ctr <= size - 4; ++ctr)
					if ((data[ctr + 0] == '\r') && (data[ctr + 1] == '\n') && (data[ctr + 2] == '\r') && (data[ctr + 3] == '\n'))
					{
						var headerEnd = ctr + 4;
						HeadersStr = Encoding.ASCII.GetString(data, 0, headerEnd);
						if (size != headerEnd)
						{
							Array.Copy(data, headerEnd, data, 0, size - headerEnd);
							Array.Resize(ref data, size - headerEnd);
							Data = data;
						}
						return;
					}
			}
		}

		public async Task<byte[]> ReadAsync(Stream stream)
		{
			byte[] result;
			if (Data != null)
			{
				result = Data;
				Data = null;
			}
			else
			{
				result = new byte[1024];
				var block = await stream.ReadAsync(result, 0, result.Length);
				if (block == 0)
					throw new EndOfStreamException();
				Array.Resize(ref result, block);
			}
			ContentLeft -= result.Length;
			return result;
		}
	}
}
