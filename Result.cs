using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;

namespace NeoMedia
{
	public class Result
	{
		bool compressed = false;

		public HttpStatusCode Code { get; private set; } = HttpStatusCode.OK;
		public ContentTypeData ContentType { get; private set; }
		public byte[] Data { get; private set; }

		public void Clear()
		{
			Code = HttpStatusCode.OK;
			ContentType = null;
			Data = null;
			compressed = false;
		}

		static byte[] GetData(string name)
		{
			if (Settings.CheckDisk)
			{
				var fileName = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(typeof(Server).Assembly.Location))), "Site", name);
				if (!File.Exists(fileName))
					return null;
				return File.ReadAllBytes(fileName);
			}
			else
			{
				var resourceName = $"{nameof(NeoMedia)}.Site.{name.Replace("/", ".")}";
				using (var stream = typeof(Server).Assembly.GetManifestResourceStream(resourceName))
				{
					if (stream == null)
						return null;
					var data = new byte[stream.Length];
					stream.Read(data, 0, data.Length);
					return data;
				}
			}
		}

		public static Result CreateFromFile(string name)
		{
			var result = new Result();

			var ext = Path.GetExtension(name).ToLowerInvariant();
			result.ContentType = ContentTypeData.GetContentTypeDataByExtension(ext);
			if (result.ContentType == null)
			{
				result = CreateFromFile("InvalidType.html");
				result.Code = HttpStatusCode.UnsupportedMediaType;
				return result;
			}

			result.Data = GetData(name);

			if (result.Data == null)
			{
				result = CreateFromFile("404.html");
				result.Code = HttpStatusCode.NotFound;
				return result;
			}

			result.CheckCompress();

			return result;
		}

		public static Result CreateFromText(string text, string ext = ".jsn")
		{
			var result = new Result();

			result.ContentType = ContentTypeData.GetContentTypeDataByExtension(ext);
			if (result.ContentType == null)
			{
				result = CreateFromFile("InvalidType.html");
				result.Code = HttpStatusCode.UnsupportedMediaType;
				return result;
			}

			result.Data = Encoding.UTF8.GetBytes(text);
			result.CheckCompress();

			return result;
		}

		public static Result Empty => new Result
		{
			ContentType = ContentTypeData.GetContentTypeDataByExtension(".txt"),
			Data = new byte[0],
		};

		void CheckCompress()
		{
			if (ContentType?.Compress != true)
				return;

			using (var ms = new MemoryStream())
			{
				using (var gz = new GZipStream(ms, CompressionLevel.Optimal, true))
					gz.Write(Data, 0, Data.Length);
				Data = ms.ToArray();
			}
			compressed = true;
		}

		public void Send(Stream stream)
		{
			var response = new List<string>();
			response.Add($"HTTP/1.1 {(int)Code} {Code}");
			response.Add($"Content-Type: {ContentType}");
			response.Add($"Content-Length: {Data.Length}");
			if (compressed)
				response.Add("Content-Encoding: gzip");
			response.Add(""); // Blank line signals end of text
			var output = Encoding.UTF8.GetBytes(string.Join("", response.Select(str => $"{str}\r\n")));

			stream.Write(output, 0, output.Length);
			stream.Write(Data, 0, Data.Length);
		}
	}
}
