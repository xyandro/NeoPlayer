using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NeoPlayer
{
	public class Message
	{
		public enum MessageCommand
		{
			None,
			QueueVideo,
			GetQueue,
			GetCool,
			GetYouTube,
		}

		public MessageCommand Command => (MessageCommand)BitConverter.ToInt32(ms.GetBuffer(), 4);
		readonly MemoryStream ms = new MemoryStream();

		public async static Task<Message> Read(Stream stream)
		{
			var message = new Message();
			await message.ReadStream(stream);
			return message;
		}

		Message()
		{
		}

		async Task ReadStream(Stream stream)
		{
			var first = true;
			var size = 4;
			var buffer = new byte[1024];
			while (ms.Length < size)
			{
				var block = await stream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, size - ms.Length));
				if (block == 0)
					throw new EndOfStreamException();
				ms.Write(buffer, 0, block);

				if ((first) && (ms.Length == size))
				{
					first = false;
					size = BitConverter.ToInt32(ms.GetBuffer(), 0);
				}
			}
			ms.Position = 8;
		}

		public Message(MessageCommand command)
		{
			Add(0);
			Add((int)command);
		}

		public void Add(byte[] value) => ms.Write(value, 0, value.Length);

		public void Add(int value) => Add(BitConverter.GetBytes(value));

		public void Add(string value)
		{
			var bytes = Encoding.UTF8.GetBytes(value);
			Add(bytes.Length);
			Add(bytes);
		}

		public void Add(MediaData value)
		{
			Add(value.Description);
			Add(value.URL);
		}

		public void Add(List<MediaData> values)
		{
			Add(values.Count);
			values.ForEach(value => Add(value));
		}

		public byte[] ToArray()
		{
			var result = ms.ToArray();
			Array.Copy(BitConverter.GetBytes(ms.Length), result, 4);
			return result;
		}

		public byte[] GetBytes(int count)
		{
			var result = new byte[count];
			ms.Read(result, 0, count);
			return result;
		}

		public int GetInt() => BitConverter.ToInt32(GetBytes(4), 0);

		public string GetString() => Encoding.UTF8.GetString(GetBytes(GetInt()));

		public MediaData GetMediaData() => new MediaData { Description = GetString(), URL = GetString() };
	}
}
