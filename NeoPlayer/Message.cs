using System;
using System.Text;

namespace NeoPlayer
{
	public class Message
	{
		public byte[] Bytes { get; private set; } = new byte[4];

		public Message(NetServer.NetServerCommand command)
		{
			Add((int)command);
		}

		public Message Add(byte[] bytes, int? position = null)
		{
			var index = position ?? Bytes.Length;
			var newSize = Math.Max(Bytes.Length, index + bytes.Length);
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

		public Message Add(int value, int? position = null)
		{
			Add(BitConverter.GetBytes(value), position);
			return this;
		}

		public Message Add(string value, int? position = null)
		{
			var index = position ?? Bytes.Length;
			var bytes = Encoding.UTF8.GetBytes(value);
			Add(bytes.Length, index);
			Add(bytes, index + 4);
			return this;
		}

		public byte[] GetBytes() => Bytes;
	}
}
