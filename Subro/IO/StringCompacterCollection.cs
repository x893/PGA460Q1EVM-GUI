using System.Collections.Generic;
using System.IO;

namespace Subro.IO
{
	public class StringCompacterCollection : ICustomSerializer
	{
		public int Count
		{
			get
			{
				return m_list.Count;
			}
		}

		public int Add(string s)
		{
			int num = m_list.IndexOf(s);
			int result;
			if (num == -1)
			{
				m_list.Add(s);
				m_compacter = null;
				result = m_list.Count - 1;
			}
			else
				result = num;
			return result;
		}

		public string this[int index]
		{
			get
			{
				return m_list[index];
			}
		}

		public bool Serialize(SimpleObjectSerializer serializer)
		{
			serializer.Writer.Write((ushort)Count);
			bool result;
			if (Count == 0)
			{
				result = true;
			}
			else
			{
				if (m_compacter == null)
				{
					List<char> list = new List<char>();
					foreach (string text in m_list)
						foreach (char item in text)
							if (!list.Contains(item))
								list.Add(item);
					m_compacter = new StringCompacter(list);
				}
				serializer.Writer.Write(new string(m_compacter.GetChars()));
				foreach (string text in m_list)
					serializer.Writer.Write(m_compacter.Serialize(text));
				result = true;
			}
			return result;
		}

		bool ICustomSerializer.Initialize(SimpleObjectSerializer s)
		{
			return true;
		}

		public bool SerializationHandled
		{
			get
			{
				return true;
			}
		}

		internal void Deserialize(BinaryReader reader)
		{
			m_list.Clear();
			m_compacter = null;
			int num = (int)reader.ReadUInt16();
			if (num > 0)
			{
				string chars = reader.ReadString();
				m_compacter = new StringCompacter(chars);
				for (int i = 0; i < num; i++)
					m_list.Add(m_compacter.Deserialize(reader));
			}
		}

		public bool Deserialize(SimpleObjectDeserializer deserializer)
		{
			Deserialize(deserializer.Reader);
			return true;
		}

		private List<string> m_list = new List<string>();
		private StringCompacter m_compacter;
	}
}
