using System;
using System.IO;
using System.Reflection;

namespace Subro.IO
{
	public class SimpleObjectDeserializer : SimpleObjectSerializationBase
	{
		private SimpleObjectDeserializer(BinaryReader b, SimpleObjectSerializationBase.DefinitionList types) : base(b, types)
		{
		}

		private Type GetObjectType()
		{
			Type type;
			if (this.TypeRef == null)
			{
				type = Type.GetType("System." + this.TypeCodeEx);
			}
			else
			{
				type = this.TypeRef.Type;
			}
			return type;
		}

		public BinaryReader Reader
		{
			get
			{
				return this.b;
			}
		}

		protected override object Restore(BinaryReader reader)
		{
			this.b = reader;
			object result;
			if (this.IsArray)
			{
				result = this.RestoreArray();
			}
			else if (this.TypeCodeEx == TypeCode.Object)
			{
				result = this.restoreobject();
			}
			else if (this.TypeCodeEx == TypeCode.DateTime)
			{
				result = new DateTime(this.b.ReadInt64());
			}
			else if (this.TypeCodeEx == TypeCode.Int32)
			{
				result = this.b.ReadInt32();
			}
			else if (this.TypeCodeEx == TypeCode.String)
			{
				result = this.defs.Strings[(int)this.b.ReadUInt16()];
			}
			else if (this.TypeCodeEx == TypeCode.DBNull)
			{
				result = DBNull.Value;
			}
			else
			{
				MethodInfo method = this.b.GetType().GetMethod("Read" + this.TypeCodeEx.ToString());
				result = method.Invoke(this.b, null);
			}
			return result;
		}

		public Array RestoreArray()
		{
			int num = this.b.ReadInt32();
			Array array = Array.CreateInstance(this.GetObjectType(), num);
			this.defs.Register(array, this);
			for (int i = 0; i < num; i++)
			{
				object subValue = this.GetSubValue();
				array.SetValue(subValue, i);
			}
			return array;
		}

		public object GetSubValue()
		{
			return new SimpleObjectDeserializer(this.b, this.defs).ObjectEx;
		}

		private object restoreobject()
		{
			ConstructorInfo constructor = this.TypeRef.Constructor;
			if (constructor == null)
			{
				throw new Exception("Cannot create an instance of " + this.TypeRef.Type.FullName);
			}
			object obj = constructor.Invoke(null);
			this.defs.Register(obj, this);
			ICustomSerializer customSerializer = obj as ICustomSerializer;
			if (customSerializer != null)
			{
				if (customSerializer.Deserialize(this))
				{
					return obj;
				}
			}
			ushort num = this.b.ReadUInt16();
			if (num > 0)
			{
				for (int i = 0; i < (int)num; i++)
				{
					int index = (int)this.b.ReadUInt16();
					string field = this.defs.Strings[index];
					MemberInfo member = this.GetMember(this.TypeRef.Type, field);
					object subValue = this.GetSubValue();
					if (!(member == null))
					{
						member.SetValue(obj, subValue);
					}
				}
			}
			return obj;
		}

		private MemberInfo GetMember(Type type, string field)
		{
			MemberInfo[] member = type.GetMember(field, MemberTypes.Field | MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			MemberInfo result;
			if (member.Length == 0)
			{
				if (type == typeof(object))
				{
					result = null;
				}
				else
				{
					result = this.GetMember(type.BaseType, field);
				}
			}
			else
			{
				result = member[0];
			}
			return result;
		}

		private BinaryReader b;
	}
}
