using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NeoPlayer.Models;

namespace NeoPlayer.Networking
{
	public class Message
	{
		readonly MemoryStream ms = new MemoryStream();

		public async static Task<Message> GetAsync(NeoSocket neoSocket)
		{
			var message = new Message();
			var ms = message.ms;

			ms.SetLength(0);
			var first = true;
			var size = 4;
			var buffer = new byte[1024];
			while (ms.Length < size)
			{
				var block = await neoSocket.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, size - ms.Length));
				ms.Write(buffer, 0, block);

				if ((first) && (ms.Length == size))
				{
					first = false;
					size = BitConverter.ToInt32(ms.GetBuffer(), 0);
				}
			}
			ms.Position = 4;

			return message;
		}

		public Message()
		{
			Add(0);
		}

		public void Add(byte[] value) => ms.Write(value, 0, value.Length);

		public void Add(bool playing) => Add(new byte[] { (byte)(playing ? 1 : 0) });

		public void Add(int value) => Add(BitConverter.GetBytes(value));

		public void Add(List<int> values)
		{
			Add(values.Count);
			values.ForEach(value => Add(value));
		}

		public void Add(long value) => Add(BitConverter.GetBytes(value));

		public void Add(string value)
		{
			if (value == null)
			{
				Add(-1);
				return;
			}
			var bytes = Encoding.UTF8.GetBytes(value);
			Add(bytes.Length);
			Add(bytes);
		}

		public void Add(VideoFile value)
		{
			Add(value.VideoFileID);
			Add(value.Title);
			Add(value.Tags.Count);
			foreach (var pair in value.Tags)
			{
				Add(pair.Key);
				Add(pair.Value);
			}
		}

		public void Add(List<VideoFile> values)
		{
			Add(values.Count);
			values.ForEach(value => Add(value));
		}

		public void Add(DownloadData value)
		{
			Add(value.Title);
			Add(value.Progress);
		}

		public void Add(List<DownloadData> values)
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

		internal void Add(object value)
		{
			if (value is byte[])
				Add(value as byte[]);
			else if (value is bool)
				Add((bool)value);
			else if (value is int)
				Add((int)value);
			else if (value is long)
				Add((long)value);
			else if (value is string)
				Add(value as string);
			else if (value is VideoFile)
				Add(value as VideoFile);
			else if (value is List<VideoFile>)
				Add(value as List<VideoFile>);
			else if (value is DownloadData)
				Add(value as DownloadData);
			else if (value is List<DownloadData>)
				Add(value as List<DownloadData>);
			else if (value is List<int>)
				Add(value as List<int>);
			else
				throw new Exception($"Unknown type: {value?.GetType().Name ?? "<NULL>"}");
		}

		public bool GetBool() => GetBytes(1)[0] != 0;

		public int GetInt() => BitConverter.ToInt32(GetBytes(4), 0);

		public string GetString() => Encoding.UTF8.GetString(GetBytes(GetInt()));

		public EditTags GetEditTags()
		{
			var editTags = new EditTags { VideoFileIDs = new List<int>(), Tags = new Dictionary<string, string>() };

			var count = GetInt();
			for (var ctr = 0; ctr < count; ++ctr)
				editTags.VideoFileIDs.Add(GetInt());

			count = GetInt();
			for (var ctr = 0; ctr < count; ++ctr)
				editTags.Tags[GetString()] = GetString();

			return editTags;
		}
	}
}
