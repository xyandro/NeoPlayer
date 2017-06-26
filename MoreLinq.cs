using System;
using System.Collections.Generic;

namespace NeoRemote
{
	static class MoreLinq
	{
		public static void ForEach<TInput>(this IEnumerable<TInput> items, Action<TInput> action)
		{
			foreach (var item in items)
				action(item);
		}
	}
}
