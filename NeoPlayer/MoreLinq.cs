using System;
using System.Collections.Generic;

namespace NeoPlayer
{
	static class MoreLinq
	{
		public static void ForEach<TInput>(this IEnumerable<TInput> items, Action<TInput> action)
		{
			foreach (var item in items)
				action(item);
		}

		public static IEnumerable<int> IndexOf<TInput>(this IEnumerable<TInput> items, Predicate<TInput> predicate)
		{
			var index = 0;
			foreach (var item in items)
			{
				if (predicate(item))
					yield return index;
				++index;
			}
		}
	}
}
