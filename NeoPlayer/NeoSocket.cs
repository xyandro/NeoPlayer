using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NeoPlayer
{
	public class NeoSocket
	{
		private byte[] cache;

		readonly TcpClient client;
		readonly Stream stream;
		public NeoSocket(TcpClient client)
		{
			this.client = client;
			stream = client.GetStream();
		}

		public NeoSocket(Stream stream)
		{
			this.stream = stream;
		}

		public bool Connected => client?.Connected ?? true;

		public void PutBack(byte[] buffer, int offset = 0, int? size = null)
		{
			var useSize = size ?? buffer.Length - offset;
			if (useSize == 0)
				return;
			var curSize = cache?.Length ?? 0;
			Array.Resize(ref cache, curSize + useSize);
			Array.Copy(buffer, offset, cache, curSize, useSize);
		}

		public async Task<int> ReadAsync(byte[] buffer, int offset = 0, int? size = null)
		{
			var useSize = size ?? buffer.Length - offset;

			if (cache != null)
			{
				useSize = Math.Min(useSize, cache.Length);
				Array.Copy(cache, 0, buffer, offset, useSize);
				Array.Copy(cache, useSize, cache, 0, cache.Length - useSize);
				Array.Resize(ref cache, cache.Length - useSize);
				if (cache.Length == 0)
					cache = null;
				return useSize;
			}

			var block = await stream.ReadAsync(buffer, offset, useSize);
			if (block == 0)
				throw new EndOfStreamException();
			return block;
		}

		public async Task<byte[]> ReadBlockAsync()
		{
			if (cache != null)
			{
				var saved = cache;
				cache = null;
				return saved;
			}

			var result = new byte[1024];
			var block = await ReadAsync(result);
			Array.Resize(ref result, block);
			return result;
		}

		public async Task WriteAsync(byte[] buffer, int offset, int? size) => await stream.WriteAsync(buffer, offset, size ?? buffer.Length - offset);

		public void Close()
		{
			client?.Close();
			stream.Close();
		}
	}
}
