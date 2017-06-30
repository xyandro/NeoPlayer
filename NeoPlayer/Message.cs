using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NeoPlayer
{
	public class Message
	{
		public byte[] Bytes { get; private set; } = new byte[4];
		public NetServer.NetServerCommand Command => (NetServer.NetServerCommand)BitConverter.ToInt32(Bytes, 4);
		int position = 8;

		public async static Task<Message> Read(Stream stream)
		{
			var buffer = new byte[4];
			var used = 0;
			var first = true;
			while (used < buffer.Length)
			{
				var block = await stream.ReadAsync(buffer, used, buffer.Length - used);
				if (block == 0)
					throw new EndOfStreamException();
				used += block;

				if ((first) && (used == buffer.Length))
				{
					first = false;
					Array.Resize(ref buffer, BitConverter.ToInt32(buffer, 0));
				}
			}
			return new Message(buffer);
		}

		public Message(byte[] bytes)
		{
			Bytes = bytes;
		}

		public Message(NetServer.NetServerCommand command)
		{
			Add((int)command);
		}

		public Message Add(byte[] bytes)
		{
			var index = Bytes.Length;
			var newSize = index + bytes.Length;
			if (Bytes.Length < newSize)
			{
				var data = Bytes;
				Array.Resize(ref data, newSize);
				Bytes = data;
				var size = BitConverter.GetBytes(Bytes.Length);
				Array.Copy(size, Bytes, 4);
			}

			Array.Copy(bytes, 0, Bytes, index, bytes.Length);
			return this;
		}

		public byte[] GetBytes(int count)
		{
			if (position + count > Bytes.Length)
				throw new IndexOutOfRangeException();

			var result = new byte[count];
			Array.Copy(Bytes, position, result, 0, count);
			position += count;
			return result;
		}

		public int GetInt32() => BitConverter.ToInt32(GetBytes(4), 0);

		public string GetString() => Encoding.UTF8.GetString(GetBytes(GetInt32()));

		public Message Add(int value)
		{
			Add(BitConverter.GetBytes(value));
			return this;
		}

		public Message Add(string value)
		{
			var bytes = Encoding.UTF8.GetBytes(value);
			Add(bytes.Length);
			Add(bytes);
			return this;
		}

		public byte[] GetBytes() => Bytes;
	}
}
