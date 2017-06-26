using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NeoPlayer
{
	static class JSON
	{
		static readonly Regex stringEscapeRegex = new Regex(@"[\\""\u0000-\u001f]");

		public static string ToJSON(object input)
		{
			if (input == null)
				return "null";

			if (input is bool)
				return input.ToString().ToLowerInvariant();

			if (input is int)
				return $"{input}";

			if (input is string)
				return $@"""{stringEscapeRegex.Replace(input as string, match => $@"\u{(int)match.Value[0]:x4}")}""";

			if (input is IEnumerable<object>)
				return $"[{string.Join(",", (input as IEnumerable<object>).Select(value => ToJSON(value)))}]";

			var type = input.GetType();
			if (type.IsClass)
				return $"{{{string.Join(",", type.GetProperties().Select(prop => $@"""{prop.Name}"":{ToJSON(prop.GetValue(input))}"))}}}";

			throw new Exception("Invalid input");
		}

		public static Response GetResponse(object data) => Response.CreateFromText(ToJSON(data), ".jsn");
	}
}
