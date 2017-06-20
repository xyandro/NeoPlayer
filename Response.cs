using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NeoRemote
{
	public class Response
	{
		bool compressed = false;
		string eTag = null;

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

		public static Response CreateFromFile(string name, HashSet<string> eTags)
		{
			var result = new Response();

			var ext = Path.GetExtension(name).ToLowerInvariant();
			result.ContentType = ContentTypeData.GetContentTypeDataByExtension(ext);
			if (result.ContentType == null)
			{
				result = CreateFromFile("InvalidType.html", null);
				result.Code = HttpStatusCode.UnsupportedMediaType;
				return result;
			}

			var fileName = Path.GetDirectoryName(typeof(Server).Assembly.Location);
			if (Settings.Debug)
				fileName = Path.GetDirectoryName(Path.GetDirectoryName(fileName));
			fileName = Path.Combine(fileName, "Site", name);

			var fileInfo = new FileInfo(fileName);
			if (!fileInfo.Exists)
			{
				result = CreateFromFile("404.html", null);
				result.Code = HttpStatusCode.NotFound;
				return result;
			}

			result.eTag = $"{fileInfo.LastWriteTimeUtc.Ticks}-{fileInfo.Length}";
			if (eTags?.Contains(result.eTag) == true)
			{
				result.Code = HttpStatusCode.NotModified;
				return result;
			}

			result.Data = File.ReadAllBytes(fileName);

			result.CheckCompress();

			return result;
		}

		public static Response CreateFromText(string text, string ext = ".jsn")
		{
			var result = new Response();

			result.ContentType = ContentTypeData.GetContentTypeDataByExtension(ext);
			if (result.ContentType == null)
			{
				result = CreateFromFile("InvalidType.html", null);
				result.Code = HttpStatusCode.UnsupportedMediaType;
				return result;
			}

			result.Data = Encoding.UTF8.GetBytes(text);
			result.CheckCompress();

			return result;
		}

		public static Response Empty => new Response
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

		async public Task Send(Stream stream)
		{
			var response = new List<string>();
			response.Add($"HTTP/1.1 {(int)Code} {Code}");
			response.Add($"Content-Type: {ContentType}");
			if (Data != null)
				response.Add($"Content-Length: {Data.Length}");
			if (compressed)
				response.Add("Content-Encoding: gzip");
			if (eTag != null)
				response.Add($@"ETag: ""{eTag}""");
			response.Add($"Date: {DateTime.UtcNow:r}");
			response.Add(""); // Blank line signals end of text
			var output = Encoding.UTF8.GetBytes(string.Join("", response.Select(str => $"{str}\r\n")));

			await stream.WriteAsync(output, 0, output.Length);
			if (Data != null)
				await stream.WriteAsync(Data, 0, Data.Length);
		}
	}
}
