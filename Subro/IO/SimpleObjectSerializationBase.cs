using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Subro.IO
{
	public abstract class SimpleObjectSerializationBase
	{
		private SimpleObjectSerializationBase(SimpleObjectSerializationBase.DefinitionList defs)
		{
			this.defs = defs;
		}

		protected SimpleObjectSerializationBase(object Value, SimpleObjectSerializationBase.DefinitionList defs) : this(defs)
		{
			this.ObjectEx = Value;
			if (Value != null)
			{
				Type type = Value.GetType();
				if (type.IsArray)
				{
					this.IsArray = true;
					type = type.GetElementType();
				}
				this.TypeCodeEx = Type.GetTypeCode(type);
				if (this.TypeCodeEx == TypeCode.Object)
				{
					this.TypeRef = defs.RegisterType(type);
				}
				if (this.NeedRegister)
				{
					defs.Register(this.ObjectEx, this);
				}
			}
		}

		protected bool NeedRegister
		{
			get
			{
				return this.IsArray || this.TypeCodeEx == TypeCode.Object;
			}
		}

		protected SimpleObjectSerializationBase(BinaryReader b, SimpleObjectSerializationBase.DefinitionList defs) : this(defs)
		{
			byte b2 = b.ReadByte();
			for (;;)
			{
				if (b2 == 64)
				{
					defs.Strings.Deserialize(b);
				}
				else
				{
					if (b2 != 65)
					{
						break;
					}
					defs.DeserializeTypes(b);
				}
				b2 = b.ReadByte();
			}
			if (b2 == 66)
			{
				this.ObjectIndex = (int)b.ReadUInt16();
				SimpleObjectSerializationBase.RegisteredObject registeredObject = defs.Objects[this.ObjectIndex];
				this.ObjectEx = registeredObject.Object;
				this.TypeCodeEx = registeredObject.Serializer.TypeCodeEx;
				this.IsArray = registeredObject.Serializer.IsArray;
			}
			else
			{
				if ((b2 & 128) > 0)
				{
					this.IsArray = true;
					b2 ^= 128;
				}
				if (b2 != 0)
				{
					this.TypeCodeEx = (TypeCode)b2;
					if (this.TypeCodeEx == TypeCode.Object)
					{
						ushort i = b.ReadUInt16();
						this.TypeRef = defs.GetType((int)i);
					}
					this.ObjectEx = this.Restore(b);
				}
			}
		}

		protected virtual object Restore(BinaryReader b)
		{
			return null;
		}

		public override string ToString()
		{
			string result;
			if (this.ObjectEx == null)
			{
				result = null;
			}
			else
			{
				result = this.ObjectEx.ToString();
			}
			return result;
		}

		public bool IsEmpty
		{
			get
			{
				return this.TypeCodeEx == TypeCode.Empty;
			}
		}

		protected const byte CommandArraySpecifier = 128;
		protected const byte TypeCodeCompressedStringCollection = 64;
		protected const byte TypeCodeTypeCollection = 65;
		protected const byte TypeCodePreviousObject = 66;
		protected const BindingFlags Fieldflags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		public readonly TypeCode TypeCodeEx;
		public readonly object ObjectEx;
		public readonly bool IsArray;
		protected readonly SimpleObjectSerializationBase.DefinitionList defs;
		protected readonly SimpleObjectSerializationBase.TypeReference TypeRef;
		protected int ObjectIndex;

		protected class RegisteredObject
		{
			public object Object;
			public SimpleObjectSerializationBase Serializer;
			public int Index;
		}

		protected class DefinitionList
		{
			public SimpleObjectSerializationBase.TypeReference RegisterType(Type type)
			{
				SimpleObjectSerializationBase.TypeReference typeReference = this.types.FirstOrDefault((SimpleObjectSerializationBase.TypeReference t) => t.Type == type);
				if (typeReference == null)
				{
					typeReference = new SimpleObjectSerializationBase.TypeReference(type, this.Strings.Add(type.FullName + ", " + type.Assembly.GetName().Name), this.types.Count);
					this.types.Add(typeReference);
				}
				return typeReference;
			}

			public SimpleObjectSerializationBase.RegisteredObject Register(object obj, SimpleObjectSerializationBase o)
			{
				SimpleObjectSerializationBase.RegisteredObject registeredObject = new SimpleObjectSerializationBase.RegisteredObject
				{
					Object = obj,
					Serializer = o,
					Index = this.Objects.Count
				};
				this.Objects.Add(registeredObject);
				o.ObjectIndex = registeredObject.Index;
				return registeredObject;
			}

			public SimpleObjectSerializationBase.TypeReference GetType(int i)
			{
				return this.types[i];
			}

			internal void Serialize(SimpleObjectSerializer s)
			{
				BinaryWriter writer = s.Writer;
				if (this.Strings.Count > 0)
				{
					writer.Write(64);
					this.Strings.Serialize(s);
				}
				if (this.types.Count > 0)
				{
					writer.Write(65);
					writer.Write((ushort)this.types.Count);
					for (int i = 0; i < this.types.Count; i++)
					{
						writer.Write((ushort)this.types[i].StringIndex);
					}
				}
			}

			public void DeserializeTypes(BinaryReader b)
			{
				this.types.Clear();
				int num = (int)b.ReadUInt16();
				for (int i = 0; i < num; i++)
				{
					int num2 = (int)b.ReadUInt16();
					string text = this.Strings[num2];
					Type type = null;
					try
					{
						type = Type.GetType(text, false);
					}
					catch
					{
					}
					if (type == null)
					{
						if (this.clean(ref text))
						{
							try
							{
								type = Type.GetType(text, false);
							}
							catch
							{
							}
						}
						if (type == null)
						{
							throw new TypeLoadException("Could not determine type for " + text + ". Does the executing assembly contain the specified assembly?");
						}
					}
					this.types.Add(new SimpleObjectSerializationBase.TypeReference(type, num2, i));
				}
			}

			private bool clean(ref string name)
			{
				string text = Regex.Replace(name, ",\\s*Version=[0-9\\.]+", string.Empty);
				bool result;
				if (text == name)
				{
					result = false;
				}
				else
				{
					name = text;
					result = true;
				}
				return result;
			}

			private List<SimpleObjectSerializationBase.TypeReference> types = new List<SimpleObjectSerializationBase.TypeReference>();
			public readonly StringCompacterCollection Strings = new StringCompacterCollection();
			public readonly List<SimpleObjectSerializationBase.RegisteredObject> Objects = new List<SimpleObjectSerializationBase.RegisteredObject>();
			public SimpleObjectSerializer NullValueSerializer;
		}

		public class TypeReference
		{
			public TypeReference(Type Type, int StringIndex, int Index)
			{
				this.Type = Type;
				this.StringIndex = StringIndex;
				this.Index = Index;
			}

			public ConstructorInfo Constructor
			{
				get
				{
					if (this.ci == null)
					{
						this.ci = this.Type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
					}
					return this.ci;
				}
			}

			public readonly Type Type;
			public readonly int StringIndex;
			public readonly int Index;
			private ConstructorInfo ci;
		}
	}
}
