using System.ComponentModel;
using System.Windows.Forms;

namespace Subro.Controls
{
	public class ToolstripBindingSourceSearchBox : ToolStripSourceSearchBox<BindingSourceSearchBox>
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[DefaultValue(null)]
		public CurrencyManager CurrencyManager
		{
			get
			{
				return SearchBoxControl.BindingSource;
			}
			set
			{
				SearchBoxControl.BindingSource = value;
			}
		}
	}
}
