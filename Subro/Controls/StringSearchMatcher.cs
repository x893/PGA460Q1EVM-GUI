using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Subro.Controls
{
	public class StringSearchMatcher
	{
		public StringSearchMatcher()
		{
		}

		public StringSearchMatcher(SearchBoxMode mode)
		{
			Mode = mode;
		}

		public StringSearchMatcher(SearchBoxMode mode, string searchValue) : this(mode)
		{
			SearchText = searchValue;
		}

		public SearchBoxMode Mode
		{
			get
			{
				return m_mode;
			}
			set
			{
				if (m_mode != value)
				{
					m_mode = value;
					m_rx = null;
					m_fn = null;
				}
			}
		}

		public bool AlwaysSearchInnerText
		{
			get
			{
				return m_searchinner;
			}
			set
			{
				if (m_searchinner != value)
				{
					m_searchinner = value;
					m_rx = null;
					m_fn = null;
				}
			}
		}

		public string SearchText
		{
			get
			{
				return m_txt;
			}
			set
			{
				m_txt = value;
				m_rx = null;
				m_len = ((m_txt == null) ? 0 : m_txt.Length);
				if (m_mode == SearchBoxMode.Lookup_Wildcards)
				{
					m_fn = new Func<string, bool>(WildCardSearch);
				}
			}
		}

		public override string ToString()
		{
			return Mode + " for " + m_txt;
		}

		public int TextLength
		{
			get
			{
				return m_len;
			}
		}

		public bool Matches(string s)
		{
			if (m_fn == null)
			{
				setSearchMatcher();
			}
			return m_fn(s);
		}

		public bool Matches(object o)
		{
			bool result;
			if (o == null)
			{
				result = (m_txt == null);
			}
			else
			{
				result = Matches(o.ToString());
			}
			return result;
		}

		public Func<string, bool> SearchDelegate
		{
			get
			{
				if (m_fn == null)
				{
					setSearchMatcher();
				}
				return m_fn;
			}
		}

		private void setSearchMatcher()
		{
			if (m_mode == SearchBoxMode.Lookup)
			{
				if (m_searchinner)
				{
					m_fn = new Func<string, bool>(ContainsSearch);
				}
				else
				{
					m_fn = new Func<string, bool>(StartSearch);
				}
			}
			else if (m_mode == SearchBoxMode.Lookup_Wildcards)
			{
				m_fn = new Func<string, bool>(WildCardSearch);
			}
			else
			{
				m_fn = new Func<string, bool>(RegExSearch);
			}
		}

		private bool ContainsSearch(string s)
		{
			return s.IndexOf(m_txt, StringComparison.OrdinalIgnoreCase) != -1;
		}

		private bool StartSearch(string s)
		{
			return s.StartsWith(m_txt, StringComparison.OrdinalIgnoreCase);
		}

		private bool WildCardSearch(string s)
		{
			if (m_rx == null)
			{
				string text = m_txt;
				if (!m_searchinner && !text.Contains('*') && !text.Contains('?'))
				{
					m_fn = new Func<string, bool>(StartSearch);
					return StartSearch(s);
				}
				string pattern = (m_searchinner ? null : "^") + Regex.Escape(text).Replace("\\?", ".").Replace("\\*", ".*");
				m_rx = new Regex(pattern, RegexOptions.IgnoreCase);
			}
			return m_rx.IsMatch(s ?? string.Empty);
		}

		private bool RegExSearch(string s)
		{
			if (m_rx == null)
			{
				if (m_searchinner)
				{
					m_rx = new Regex(m_txt, RegexOptions.IgnoreCase);
				}
				else
				{
					m_rx = new Regex("^" + m_txt, RegexOptions.IgnoreCase);
				}
			}
			return m_rx.IsMatch(s);
		}

		private Func<string, bool> m_fn;
		private SearchBoxMode m_mode = SearchBoxMode.Lookup_Wildcards;
		private bool m_searchinner;
		private string m_txt;
		private int m_len;
		private Regex m_rx;
	}
}
