using System;
using System.Collections.Generic;

namespace NeoPlayer.Misc
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

		public static IEnumerable<TInput> DistinctBy<TInput, TField>(this IEnumerable<TInput> items, Func<TInput, TField> selector)
		{
			var seen = new HashSet<TField>();
			foreach (var item in items)
			{
				var value = selector(item);
				if (seen.Contains(value))
					continue;
				seen.Add(value);
				yield return item;
			}
		}
	}
}
