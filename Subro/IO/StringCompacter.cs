using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Subro.IO
{
	public class StringCompacter
	{
		public StringCompacter()
		{
			m_chars = new List<char>();
			for (char c = 'A'; c < '['; c += '\u0001')
			{
				m_chars.Add(c);
				m_chars.Add(char.ToLower(c));
			}
			m_chars.AddRange("._+, <>");
			setbase();
		}

		public StringCompacter(IEnumerable<char> chars)
		{
			m_chars = new List<char>(chars);
			setbase();
		}

		public StringCompacter(params char[] chars)
		{
			m_chars = new List<char>(chars);
			setbase();
		}

		private void setbase()
		{
			m_Base = (int)Math.Ceiling(Math.Log((double)(m_chars.Count + 1), 2.0));
		}

		public char[] GetChars()
		{
			return m_chars.ToArray();
		}

		public byte[] Serialize(string s)
		{
			BitArray bitArray = new BitArray(s.Length * m_Base);
			int num = 0;
			foreach (char item in s)
			{
				int num2 = 1;
				int num3 = m_chars.IndexOf(item) + 1;
				if (num3 == 0)
					throw new ArgumentException();

				for (int j = 0; j < m_Base; j++)
				{
					if ((num3 & num2) > 0)
						bitArray[num] = true;
					num2 <<= 1;
					num++;
				}
			}
			int num4 = (int)Math.Ceiling((double)(bitArray.Length + m_Base) / 8.0);
			byte[] array = new byte[num4];
			bitArray.CopyTo(array, 0);
			return array;
		}

		public string Deserialize(BinaryReader b)
		{
			return Deserialize(Enumerate(b));
		}

		private IEnumerable<byte> Enumerate(BinaryReader b)
		{
			Stream s = b.BaseStream;
			long len = s.Length;
			while (s.Position < len)
				yield return b.ReadByte();
			yield break;
		}

		public string Deserialize(IEnumerable<byte> data)
		{
			if (m_sb == null)
				m_sb = new StringBuilder();
			else
				m_sb.Length = 0;

			IEnumerator<byte> enumerator = data.GetEnumerator();
			int num = 256;
			byte b = 0;
			for (;;)
			{
				int num2 = 0;
				for (int i = 0; i < m_Base; i++)
				{
					if (num > 128)
					{
						num = 1;
						if (!enumerator.MoveNext())
							break;
						b = enumerator.Current;
					}
					if ((b & num) > 0)
						num2 |= 1 << i;
					num <<= 1;
				}
				if (num2 == 0)
					break;
				m_sb.Append(m_chars[num2 - 1]);
			}
			return m_sb.ToString();
		}

		private List<char> m_chars;
		private int m_Base;
		private StringBuilder m_sb;
	}
}
