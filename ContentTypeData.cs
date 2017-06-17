using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoMedia
{
	public class ContentTypeData
	{
		static readonly List<ContentTypeData> ContentTypeDatas = new List<ContentTypeData>
		{
			new ContentTypeData("text/css; charset=utf-8", true, ".css"),
			new ContentTypeData("text/html; charset=utf-8", true, ".htm", ".html"),
			new ContentTypeData("image/x-icon", false, ".ico"),
			new ContentTypeData("image/jpeg", false, ".jpg"),
			new ContentTypeData("application/javascript; charset=utf-8", true, ".js"),
			new ContentTypeData("application/json; charset=utf-8", true, ".jsn", ".json"),
			new ContentTypeData("image/png", false, ".png"),
			new ContentTypeData("text/plain; charset=utf-8", true, ".txt"),
		};

		public static ContentTypeData GetContentTypeDataByExtension(string ext) => ContentTypeDatas.FirstOrDefault(data => data.Extensions.Contains(ext, StringComparer.InvariantCultureIgnoreCase));

		public HashSet<string> Extensions { get; }
		public string ContentType { get; }
		public bool Compress { get; }

		public ContentTypeData(string contentType, bool compress, params string[] extensions)
		{
			Extensions = new HashSet<string>(extensions);
			ContentType = contentType;
			Compress = compress;
		}

		public override string ToString() => ContentType;
	}
}
