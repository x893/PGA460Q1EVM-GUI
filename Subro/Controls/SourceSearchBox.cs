using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Subro.Controls
{
	public abstract class SourceSearchBox : SearchBoxBase
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[DefaultValue(null)]
		[Editor(typeof(Editor), typeof(UITypeEditor))]
		public Column SearchProperty
		{
			get
			{
				return m_col;
			}
			set
			{
				if (m_col != value)
				{
					m_col = value;
					m_propname = ((value == null) ? null : value.Name);
					m_pos.X = ((m_col == null) ? 0 : m_col.Index.Value);
					base.NotifyStateChanged(true);
				}
			}
		}

		[DefaultValue(null)]
		public string PropertyName
		{
			get
			{
				return m_propname;
			}
			set
			{
				if (!(m_propname == value))
				{
					m_propname = value;
					if (!base.IsInitializing)
					{
						if (m_needsetprops)
						{
							setprops();
						}
						else if (value == null || Columns == null)
						{
							base.NotifyStateChanged(false);
						}
						else
						{
							SearchProperty = getcol(value, true);
						}
					}
				}
			}
		}

		private Column getcol(string name, bool Throw)
		{
			Column column = m_props.FirstOrDefault((Column c) => c.Name == name);
			if (column == null && Throw)
			{
				throw new ArgumentException(name + " is not a valid property");
			}
			return column;
		}

		protected void SourceChanged()
		{
			m_needsetprops = true;
			base.NotifyStateChanged(true);
		}

		protected override void OnEndInit()
		{
			base.OnEndInit();
			if (m_propname != null)
			{
				PropertyName = m_propname;
			}
		}

		protected override bool search(StringSearchMatcher search)
		{
			if (m_needsetprops)
			{
				setprops();
			}
			return base.search(search);
		}

		public Column[] Columns
		{
			get
			{
				if (m_needsetprops)
				{
					if (base.IsInitializing)
					{
						return null;
					}
					setprops();
				}
				return m_props;
			}
		}

		protected void ResetColumns()
		{
			m_needsetprops = true;
		}

		private void setprops()
		{
			m_needsetprops = false;
			IEnumerable<Column> columns = GetColumns();
			if (columns == null)
			{
				m_props = null;
			}
			else
			{
				m_props = GetColumns().ToArray<Column>();
				if (m_props.Length == 0)
				{
					m_props = null;
				}
				else
				{
					for (int i = 0; i < m_props.Length; i++)
						if (m_props[i].Index == null)
							m_props[i].Index = new int?(i);
				}
				if (m_propname != null)
				{
					m_col = getcol(m_propname, false);
					if (m_col == null)
					{
						m_propname = null;
						m_pos.X = 0;
					}
				}
			}
		}

		protected abstract IEnumerable<Column> GetColumns();

		protected sealed override void ResetStartPosition(object stored)
		{
			if (stored == null)
			{
				m_pos = Point.Empty;
				if (m_col != null)
					m_pos.X = m_col.Index.Value;
			}
			else
				m_pos = (Point)stored;

			if (Columns != null && m_col != null)
				m_curcol = Array.IndexOf<Column>(m_props, m_col);
			else
				m_curcol = 0;
		}

		public Point Position
		{
			get
			{
				return m_pos;
			}
			set
			{
				m_pos = value;
			}
		}

		protected sealed override void SetFoundPosition()
		{
			try
			{
				SetPosition(m_pos.X, m_pos.Y);
			}
			catch (Exception ex)
			{
				ShowException(ex);
			}
		}

		protected abstract void SetPosition(int col, int row);

		protected override object GetCurrent()
		{
			return m_props[m_curcol].GetValue(m_pos.Y);
		}

		protected abstract int RecordCount { get; }

		protected override bool IncreasePosition()
		{
			if (m_col == null)
			{
				if (++m_curcol < m_props.Length)
				{
					m_pos.X = m_props[m_curcol].Index.Value;
					return true;
				}
				m_curcol = 0;
				m_pos.X = m_props[0].Index.Value;
			}
			return (m_pos.Y = m_pos.Y + 1) < RecordCount;
		}

		protected override object GetPosition()
		{
			return m_pos;
		}

		protected override bool Supports(SearchBoxMode Mode)
		{
			if (Mode != SearchBoxMode.Lookup)
			{
				if (Mode == SearchBoxMode.Filter)
					return CanFilter;
			}
			return true;
		}

		protected abstract bool CanFilter { get; }

		protected override bool CheckIsReady()
		{
			return Columns != null && (m_col != null || base.Mode != SearchBoxMode.Filter);
		}

		protected abstract Point GetCurrentPosition();

		protected void filter(IBindingListView source, StringSearchMatcher search)
		{
			if (m_propname == null || search.TextLength == 0)
			{
				source.RemoveFilter();
				if (m_propname == null)
					base.TextBox.Clear();
			}
			else
			{
				source.Filter = string.Concat(new string[]
				{
					m_propname,
					" like ",
					base.AlwaysSearchInnerText ? "*" : null,
					Text,
					"*"
				});
			}
			Point currentPosition = GetCurrentPosition();
			if (currentPosition.X != m_pos.X && m_pos.X != -1 && RecordCount > 0 && currentPosition.Y != -1)
			{
				SetPosition(m_pos.X, currentPosition.Y);
			}
		}

		protected void NotifyPositionChanged()
		{
			if (!base.IsBusy && base.Mode != SearchBoxMode.Filter)
			{
				ResetStartPosition(null);
				Text = null;
			}
		}

		private Column m_col;
		private string m_propname;
		private Column[] m_props;
		private bool m_needsetprops = true;
		private Point m_pos;
		private int m_curcol;

		public class Column
		{
			public Column(string name, Func<int, object> getValue)
			{
				Name = name;
				GetValue = getValue;
			}

			public string Header
			{
				get
				{
					string name;
					if (m_header == null)
					{
						name = Name;
					}
					else
					{
						name = m_header;
					}
					return name;
				}
				set
				{
					m_header = value;
				}
			}

			public override string ToString()
			{
				return Header;
			}

			public readonly string Name;
			public readonly Func<int, object> GetValue;
			public int? Index;
			private string m_header;
		}

		private class Editor : UITypeEditor
		{
			public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
			{
				return UITypeEditorEditStyle.DropDown;
			}

			public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
			{
				SourceSearchBox b = (SourceSearchBox)context.Instance;
				if (b != null && b.Columns != null)
				{
					ListBox lb = new ListBox();
					foreach (Column column in b.m_props)
					{
						int selectedIndex = lb.Items.Add(column);
						if (b.m_col == column || column.Name == b.m_propname)
						{
							lb.SelectedIndex = selectedIndex;
						}
					}
					IWindowsFormsEditorService iw = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
					lb.Click += delegate(object param0, EventArgs param1)
					{
						b.PropertyName = (string)lb.SelectedItem;
						iw.CloseDropDown();
					};
					iw.DropDownControl(lb);
				}
				return base.EditValue(context, provider, value);
			}
		}
	}
}
