using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Subro.Controls
{
	public class BindingSourceSearchBox : SourceSearchBox
	{
		protected override void SetPosition(int col, int row)
		{
			m_source.Position = row;
		}

		protected override Point GetCurrentPosition()
		{
			return (m_source == null) ? Point.Empty : new Point(0, m_source.Position);
		}

		protected override int RecordCount
		{
			get
			{
				return m_source.Count;
			}
		}

		protected override bool CanFilter
		{
			get
			{
				IBindingListView bindingListView = m_source.List as IBindingListView;
				return bindingListView != null && bindingListView.SupportsFiltering;
			}
		}

		protected override void filter(StringSearchMatcher search)
		{
			base.filter(m_source.List as IBindingListView, search);
		}

		protected override void OnDisposed()
		{
			BindingSource = null;
		}

		[DefaultValue(null)]
		public CurrencyManager BindingSource
		{
			get
			{
				return m_source;
			}
			set
			{
				if (m_source != value)
				{
					if (m_source != null)
					{
						m_source.PositionChanged -= source_PositionChanged;
						m_source.ListChanged -= source_ListChanged;
					}
					m_source = value;
					if (m_source != null)
					{
						m_source.PositionChanged += source_PositionChanged;
						m_source.ListChanged += source_ListChanged;
					}
					base.SourceChanged();
				}
			}
		}

		private void source_ListChanged(object sender, ListChangedEventArgs e)
		{
			switch (e.ListChangedType)
			{
			case ListChangedType.PropertyDescriptorAdded:
			case ListChangedType.PropertyDescriptorDeleted:
			case ListChangedType.PropertyDescriptorChanged:
				base.SourceChanged();
				break;
			}
		}

		protected override IEnumerable<SourceSearchBox.Column> GetColumns()
		{
			IEnumerable<SourceSearchBox.Column> result;
			if (m_source == null || m_source.List == null)
				result = null;
			else
			{
				result = from PropertyDescriptor pd in m_source.GetItemProperties()
				select new SourceSearchBox.Column(pd.Name, (int i) => pd.GetValue(m_source.List[i]))
				{
					Header = pd.DisplayName
				};
			}
			return result;
		}

		private void source_PositionChanged(object sender, EventArgs e)
		{
			base.NotifyPositionChanged();
		}

		private CurrencyManager m_source;
	}
}
