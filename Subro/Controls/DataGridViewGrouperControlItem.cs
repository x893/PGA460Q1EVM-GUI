using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Subro.Controls
{
	public class DataGridViewGrouperControlItem : ToolStripControlHost
	{
		public DataGridViewGrouperControlItem() : base(new DataGridViewGrouperControl())
		{
			DataGridViewGrouperControl.MinimumSize = new Size(150, 20);
		}

		public DataGridViewGrouperControl DataGridViewGrouperControl
		{
			get
			{
				return (DataGridViewGrouperControl)base.Control;
			}
		}

		[DefaultValue(null)]
		public DataGridView DataGridView
		{
			get
			{
				return DataGridViewGrouperControl.DataGridView;
			}
			set
			{
				DataGridViewGrouperControl.DataGridView = value;
			}
		}
	}
}
