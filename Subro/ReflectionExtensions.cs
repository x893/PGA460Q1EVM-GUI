using System.Reflection;

namespace Subro
{
	public static class ReflectionExtensions
	{
		public static object GetValue(this MemberInfo mi, object o)
		{
			object result;
			if (mi is PropertyInfo)
				result = ((PropertyInfo)mi).GetValue(o, null);
			else if (mi is FieldInfo)
				result = ((FieldInfo)mi).GetValue(o);
			else
				result = null;
			return result;
		}


		public static bool GetIsWritable(this MemberInfo mi)
		{
			bool result;
			if (mi is FieldInfo)
				result = !((FieldInfo)mi).IsInitOnly;
			else
				result = ((PropertyInfo)mi).CanWrite;
			return result;
		}

		public static void SetValue(this MemberInfo mi, object obj, object value)
		{
			if (mi is PropertyInfo)
				((PropertyInfo)mi).SetValue(obj, value, null);
			else
				((FieldInfo)mi).SetValue(obj, value);
		}
	}
}
