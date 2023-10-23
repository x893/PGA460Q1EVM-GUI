using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Subro.IO
{
	public class SimpleObjectSerializer : SimpleObjectSerializationBase, IContentWriter
	{
		private SimpleObjectSerializer(object Value, SimpleObjectFieldSerializationMode FieldMode, SimpleObjectSerializationBase.DefinitionList Types) : base(Value, Types)
		{
			FieldModeEx = FieldMode;
			if (!base.IsEmpty)
			{
				if (IsArray)
				{
					Children = GetArrayValues().ToArray<IContentWriter>();
				}
				else if (TypeCodeEx == TypeCode.Object)
				{
					CustomSerializer = (ObjectEx as ICustomSerializer);
					if (CustomSerializer == null || !CustomSerializer.Initialize(this))
					{
						Children = GetFields().ToArray<FieldReference>();
					}
				}
				else if (TypeCodeEx == TypeCode.String)
				{
					m_index = defs.Strings.Add((string)Value);
				}
			}
		}

		private IEnumerable<IContentWriter> GetArrayValues()
		{
			Array arr = (Array)ObjectEx;
			for (int i = 0; i < arr.Length; i++)
			{
				object o = arr.GetValue(i);
				yield return GetSubValue(o);
			}
			yield break;
		}

		private IContentWriter GetSubValue(object o)
		{
			IContentWriter result;
			if (o == null)
			{
				if (defs.NullValueSerializer == null)
				{
					defs.NullValueSerializer = new SimpleObjectSerializer(null, FieldModeEx, defs);
				}
				result = defs.NullValueSerializer;
			}
			else
			{
				SimpleObjectSerializationBase.RegisteredObject registeredObject = defs.Objects.FirstOrDefault((SimpleObjectSerializationBase.RegisteredObject r) => r.Object == o);
				if (registeredObject == null)
					result = new SimpleObjectSerializer(o, FieldModeEx, defs);
				else
				{
					result = new PreviousObjectWriter { ObjectEx = (SimpleObjectSerializer)registeredObject.Serializer };
				}
			}
			return result;
		}

		private IEnumerable<FieldReference> GetFields()
		{
			MemberValue[] vars = (from mi in GetVariables(TypeRef.Type)
								  select new MemberValue { Member = mi } into m
								  where ShouldSerialize(m.Member, ref m.Value)
								  select m
				).ToArray<MemberValue>();
			foreach (MemberValue mi2 in vars)
			{
				IContentWriter o = GetSubValue(mi2.Value);
				yield return new FieldReference
				{
					FieldIndex = defs.Strings.Add(mi2.Member.Name),
					SerializerEx = o
				};
			}
			yield break;
		}

		public BinaryWriter Writer
		{
			get
			{
				return m_writer;
			}
		}

		public void SerializeTo(Stream s)
		{
			BinaryWriter binaryWriter = new BinaryWriter(s);
			SerializeTo(binaryWriter);
		}

		private void writeindex(int i)
		{
			m_writer.Write((ushort)i);
		}

		void IContentWriter.WriteContents(BinaryWriter w)
		{
			WriteContents(w);
		}

		private void WriteContents(BinaryWriter w)
		{
			m_writer = w;
			if (base.IsEmpty)
			{
				WriteEmpty();
			}
			else
			{
				byte b = (byte)TypeCodeEx;
				if (IsArray)
				{
					b |= 128;
				}
				w.Write(b);
				if (TypeRef != null)
				{
					writeindex(TypeRef.Index);
				}
				if (CustomSerializer != null)
				{
					if (CustomSerializer.Serialize(this))
					{
						return;
					}
				}
				if (Children != null)
				{
					if (IsArray)
						w.Write(Children.Length);
					else
						w.Write((ushort)Children.Length);
					foreach (IContentWriter contentWriter in Children)
						contentWriter.WriteContents(w);
				}
				else if (TypeCodeEx != TypeCode.DBNull)
					WriteValue();
			}
		}

		public void SerializeTo(BinaryWriter w)
		{
			m_writer = w;
			defs.Serialize(this);
			WriteContents(w);
		}

		private void WriteEmpty()
		{
			m_writer.Write(0);
		}

		public void WriteSubValue(object value)
		{
			if (value == null)
				WriteEmpty();
			else
				GetSubValue(value).WriteContents(m_writer);
		}

		private void WriteValue()
		{
			if (TypeCodeEx == TypeCode.DateTime)
				m_writer.Write(((DateTime)ObjectEx).Ticks);
			else if (TypeCodeEx == TypeCode.String)
				m_writer.Write((ushort)m_index);
			else if (TypeCodeEx == TypeCode.Int32)
				m_writer.Write((int)ObjectEx);
			else if (TypeCodeEx == TypeCode.Int64)
				m_writer.Write((long)ObjectEx);
			else if (TypeCodeEx == TypeCode.UInt32)
				m_writer.Write((uint)ObjectEx);
			else if (TypeCodeEx == TypeCode.UInt64)
				m_writer.Write((ulong)ObjectEx);
			else if (TypeCodeEx == TypeCode.Double)
				m_writer.Write((double)ObjectEx);
			else if (TypeCodeEx == TypeCode.Single)
				m_writer.Write((float)ObjectEx);
			else if (TypeCodeEx == TypeCode.Byte)
				m_writer.Write((byte)ObjectEx);
			else if (TypeCodeEx == TypeCode.Boolean)
				m_writer.Write((bool)ObjectEx);
			else
			{
				Type type = Type.GetType("System." + TypeCodeEx);
				MethodInfo method = m_writer.GetType().GetMethod("Write", new Type[] { type });
				method.Invoke(m_writer, new object[] { ObjectEx });
			}
		}

		protected bool ShouldSerialize(MemberInfo m, ref object val)
		{
			bool result;
			if (!m.GetIsWritable())
				result = false;
			else if (m.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length > 0)
				result = false;
			else
			{
				MethodInfo method = m.DeclaringType.GetMethod("ShouldSerialize" + m.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
				if (method != null && !(bool)method.Invoke(ObjectEx, null))
					result = false;
				else
				{
					val = m.GetValue(ObjectEx);
					if (val is Pointer || val is IntPtr)
						result = false;
					else
					{
						DefaultValueAttribute defaultValueAttribute = (DefaultValueAttribute)m.GetCustomAttributes(typeof(DefaultValueAttribute), true).FirstOrDefault<object>();
						result = (defaultValueAttribute == null || !object.Equals(val, defaultValueAttribute.Value));
					}
				}
			}
			return result;
		}

		protected virtual IEnumerable<MemberInfo> GetVariables(Type type)
		{
			if (FieldModeEx == SimpleObjectFieldSerializationMode.Fields)
			{
				while (type != typeof(object))
				{
					foreach (FieldInfo i in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
					{
						yield return i;
					}
					type = type.BaseType;
				}
			}
			else
			{
				foreach (FieldInfo j in type.GetFields())
					yield return j;
				foreach (PropertyInfo k in type.GetProperties())
					yield return k;
			}
			yield break;
		}

		public readonly SimpleObjectFieldSerializationMode FieldModeEx;
		public readonly IContentWriter[] Children;
		public readonly ICustomSerializer CustomSerializer;

		protected readonly int m_index;
		private BinaryWriter m_writer;

		private class PreviousObjectWriter : IContentWriter
		{
			public void WriteContents(BinaryWriter w)
			{
				w.Write(66);
				w.Write((ushort)ObjectEx.ObjectIndex);
			}

			public SimpleObjectSerializer ObjectEx;
		}

		private class FieldReference : IContentWriter
		{
			public void WriteContents(BinaryWriter w)
			{
				w.Write((ushort)FieldIndex);
				SerializerEx.WriteContents(w);
			}

			public IContentWriter SerializerEx;
			public int FieldIndex;
		}

		private class MemberValue
		{
			public MemberInfo Member;
			public object Value;
		}
	}
}
