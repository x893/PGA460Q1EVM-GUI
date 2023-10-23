using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Subro.Controls
{
	public class ToolstripDataGridSearchBox : ToolStripSourceSearchBox<DataGridSearchBox>
	{
		[DefaultValue(null)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DataGridView DataGridView
		{
			get
			{
				return base.SearchBoxControl.DataGridView;
			}
			set
			{
				base.SearchBoxControl.DataGridView = value;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[DefaultValue(null)]
		public DataGridSearchBox.ColumnSearchMode SearchModeColumn
		{
			get
			{
				return base.SearchBoxControl.SearchModeColumn;
			}
			set
			{
				base.SearchBoxControl.SearchModeColumn = value;
			}
		}
	}
}
