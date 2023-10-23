using System;
using System.Collections.Generic;
using System.Linq;

namespace Subro.Controls
{
	public static class EnumFunctions
	{
		public static IEnumerable<T> GetValues<T>() where T : IComparable, IFormattable, IConvertible
		{
			return EnumFunctions.GetValues<T>((T x) => x);
		}

		public static IEnumerable<T> GetValues<T>(Func<T, T> pred) where T : IComparable, IFormattable, IConvertible
		{
			foreach (object obj in Enum.GetValues(typeof(T)))
			{
				T item = (T)((object)obj);
				yield return pred(item);
			}
			yield break;
		}

		[Obsolete("Framework 4.0 contains a native HasFlag function :D")]
		public static bool HasFlag<T>(this T value, T flag) where T : IComparable, IFormattable, IConvertible
		{
			int num = flag.ToInt32(null);
			return (value.ToInt32(null) & num) == num;
		}

		public static T Parse<T>(string Value) where T : IComparable, IFormattable, IConvertible
		{
			T result;
			if (string.IsNullOrEmpty(Value))
			{
				result = default(T);
			}
			else
			{
				result = (T)((object)Enum.Parse(typeof(T), Value, true));
			}
			return result;
		}

		public static IEnumerable<T> Select<T>(this T e, Func<T, T> pred) where T : IComparable, IFormattable, IConvertible
		{
			foreach (T item in e.Split<T>())
			{
				yield return pred(item);
			}
			yield break;
		}

		public static IEnumerable<T> Split<T>(this T enumeration) where T : IComparable, IFormattable, IConvertible
		{
			int val = Convert.ToInt32(enumeration);
			foreach (T item in EnumFunctions.GetValues<T>())
			{
				T t = item;
				int i = t.ToInt32(null);
				if (i > 0 & (i & val) == i)
				{
					yield return item;
				}
			}
			yield break;
		}

		public static IEnumerable<T> Split<T>(this T enumeration, bool CompactMaskedValues) where T : IComparable, IFormattable, IConvertible
		{
			IEnumerable<T> enumerable = enumeration.Split<T>();
			IEnumerable<T> result;
			if (!CompactMaskedValues)
			{
				result = enumerable;
			}
			else
			{
				List<T> list = (from r in enumerable
				orderby r.ToInt32(null) descending
				select r).ToList<T>();
				for (int i = 0; i < list.Count; i++)
				{
					T t = list[i];
					int num = t.ToInt32(null);
					for (int j = i + 1; j < list.Count; j++)
					{
						int num2 = num;
						t = list[j];
						int num3 = num2 & t.ToInt32(null);
						t = list[j];
						if (num3 == t.ToInt32(null))
						{
							list.RemoveAt(j--);
						}
					}
				}
				result = list;
			}
			return result;
		}

		public static IEnumerable<T> GetValues<T>(this T e, Func<T, bool> pred) where T : IComparable, IFormattable, IConvertible
		{
			foreach (T item in e.Split<T>())
			{
				if (pred(item))
				{
					yield return item;
				}
			}
			yield break;
		}
	}
}
