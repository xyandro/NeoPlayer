using System;

namespace NeoPlayer.Misc
{
	static class Helpers
	{
		public static bool Debug
		{
			get
			{
#if DEBUG
				return true;
#else
				return false;
#endif
			}
		}

		static public T Copy<T>(T oldObj)
		{
			var newObj = (T)Activator.CreateInstance(typeof(T));
			foreach (var property in typeof(T).GetProperties())
				property.SetValue(newObj, property.GetValue(oldObj));
			return newObj;
		}

		static public bool Match<T>(T obj1, T obj2)
		{
			if ((obj1 == null) != (obj2 == null))
				return false;
			if (obj1 == null)
				return true;
			foreach (var property in typeof(T).GetProperties())
			{
				var value1 = property.GetValue(obj1);
				var value2 = property.GetValue(obj2);
				if (!value1.Equals(value2))
					return false;
			}
			return true;
		}
	}
}
