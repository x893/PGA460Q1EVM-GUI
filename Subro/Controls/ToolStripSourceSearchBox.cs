using System;
using System.ComponentModel;

namespace Subro.Controls
{
	public class ToolStripSourceSearchBox<CT> : ToolstripSearchBox<CT> where CT : SourceSearchBox, new()
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[DefaultValue(null)]
		public string PropertyName
		{
			get
			{
				CT searchBoxControl = base.SearchBoxControl;
				return searchBoxControl.PropertyName;
			}
			set
			{
				CT searchBoxControl = base.SearchBoxControl;
				searchBoxControl.PropertyName = value;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[DefaultValue(null)]
		public SourceSearchBox.Column SearchProperty
		{
			get
			{
				CT searchBoxControl = base.SearchBoxControl;
				return searchBoxControl.SearchProperty;
			}
			set
			{
				CT searchBoxControl = base.SearchBoxControl;
				searchBoxControl.SearchProperty = value;
			}
		}
	}
}
