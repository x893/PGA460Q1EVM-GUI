using System;
using System.Collections;

namespace Subro
{
	internal static class CompareFunctions
	{
		private static IComparer GetGenericComparer(Type From, Type To)
		{
			return (IComparer)CompareFunctions.GetGeneric(From, To, new Type[]
			{
				typeof(IComparable<>)
			});
		}

		private static IEqualityComparer GetGenericEqualityComparer(Type From, Type To)
		{
			return (IEqualityComparer)CompareFunctions.GetGeneric(From, To, new Type[]
			{
				typeof(IEquatable<>),
				typeof(IComparable<>)
			});
		}

		private static Type GetInnerType(Type type)
		{
			Type result;
			if (type.IsGenericType && typeof(Nullable<>) == type.GetGenericTypeDefinition())
			{
				result = type.GetGenericArguments()[0];
			}
			else
			{
				result = type;
			}
			return result;
		}

		private static bool hasbase(Type type)
		{
			return type.BaseType != null && type.BaseType != typeof(object);
		}

		private static object GetGeneric(Type From, Type To, params Type[] GenericBaseTypes)
		{
			while (true)
			{
				foreach (Type type in GenericBaseTypes)
				{
					Type baseType = To;
					while (baseType != null)
					{
						if (type.MakeGenericType(new Type[] { baseType }).IsAssignableFrom(From))
						{
							if (type == typeof(IEquatable<>))
								return Activator.CreateInstance(typeof(StrongEquatable<,>).MakeGenericType(new Type[] { From, baseType }));
							return Activator.CreateInstance(typeof(StrongCompare<,>).MakeGenericType(new Type[] { From, baseType }));
						}
						Type innerType = GetInnerType(baseType);
						if (innerType == baseType)
							baseType = baseType.BaseType;
						else
							baseType = innerType;
					}
				}
				if (hasbase(From))
					From = From.BaseType;
				else
					return null;
			}
		}

		internal static IComparer GetComparer(Type From, Type To)
		{
			IComparer result;
			if (From == To && From == typeof(string))
				result = new CompareFunctions.StringComparer();
			else
			{
				From = CompareFunctions.GetInnerType(From);
				IComparer genericComparer = CompareFunctions.GetGenericComparer(From, To);
				if (genericComparer != null)
					result = genericComparer;
				else if (typeof(IComparable).IsAssignableFrom(From))
				{
					result = (IComparer)Activator.CreateInstance(typeof(CompareFunctions.NonGenericCompare<>).MakeGenericType(new Type[]
					{
						From
					}));
				}
				else
					result = new CompareFunctions.StringComparer();
			}
			return result;
		}

		internal static IEqualityComparer GetEqualityComparer(Type From, Type To)
		{
			IEqualityComparer result;
			if (From == To && From == typeof(string))
				result = new CompareFunctions.StringComparer();
			else
			{
				From = CompareFunctions.GetInnerType(From);
				IEqualityComparer genericEqualityComparer = CompareFunctions.GetGenericEqualityComparer(From, To);
				if (genericEqualityComparer != null)
					result = genericEqualityComparer;
				else
					result = new CompareFunctions.DefaultEquals();
			}
			return result;
		}

		private class DefaultEquals : IEqualityComparer
		{
			public new bool Equals(object x, object y)
			{
				return x.Equals(y);
			}

			public int GetHashCode(object o)
			{
				return o.GetHashCode();
			}
		}

		private class StrongEquatable<F, T> : IEqualityComparer where F : IEquatable<T>
		{
			public new bool Equals(object x, object y)
			{
				F f = (F)((object)x);
				return f.Equals((T)((object)y));
			}

			public int GetHashCode(object o)
			{
				return o.GetHashCode();
			}
		}

		private class StrongCompare<F, T> : IComparer, IEqualityComparer where F : IComparable<T>
		{
			public int Compare(object x, object y)
			{
				F f = (F)((object)x);
				return f.CompareTo((T)((object)y));
			}

			public new bool Equals(object x, object y)
			{
				return this.Compare(x, y) == 0;
			}

			public int GetHashCode(object o)
			{
				return o.GetHashCode();
			}
		}

		private class NonGenericCompare<T> : IComparer where T : IComparable
		{
			public int Compare(object x, object y)
			{
				T t = (T)((object)x);
				return t.CompareTo(y);
			}
		}

		private class StringComparer : IComparer, IEqualityComparer
		{
			public int Compare(object x, object y)
			{
				return string.Compare(x.ToString(), y.ToString());
			}

			public new bool Equals(object x, object y)
			{
				return string.Equals(x.ToString(), y.ToString());
			}

			public int GetHashCode(object o)
			{
				return o.GetHashCode();
			}
		}
	}
}
