using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Subro.Controls
{
	public class ToolstripSearchBox<CT> : ToolStripControlHost, ISupportInitialize where CT : SearchBoxBase, new()
	{
		public ToolstripSearchBox() : base(Activator.CreateInstance<CT>())
		{
			CT searchBoxControl = this.SearchBoxControl;
			searchBoxControl.MinimumSize = new Size(150, 20);
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public CT SearchBoxControl
		{
			get
			{
				return (CT)((object)base.Control);
			}
		}

		public override string Text
		{
			get
			{
				CT searchBoxControl = this.SearchBoxControl;
				return searchBoxControl.Text;
			}
			set
			{
				CT searchBoxControl = this.SearchBoxControl;
				searchBoxControl.Text = value;
			}
		}

		[DefaultValue(SearchBoxMode.Lookup_Wildcards)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public SearchBoxMode Mode
		{
			get
			{
				CT searchBoxControl = this.SearchBoxControl;
				return searchBoxControl.Mode;
			}
			set
			{
				CT searchBoxControl = this.SearchBoxControl;
				searchBoxControl.Mode = value;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[DefaultValue(false)]
		public bool AlwaysSearchInnerText
		{
			get
			{
				CT searchBoxControl = this.SearchBoxControl;
				return searchBoxControl.AlwaysSearchInnerText;
			}
			set
			{
				CT searchBoxControl = this.SearchBoxControl;
				searchBoxControl.AlwaysSearchInnerText = value;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[DefaultValue(false)]
		public bool ShowOptionsButtons
		{
			get
			{
				CT searchBoxControl = this.SearchBoxControl;
				return searchBoxControl.ShowOptionsButton;
			}
			set
			{
				CT searchBoxControl = this.SearchBoxControl;
				searchBoxControl.ShowOptionsButton = value;
			}
		}

		public void BeginInit()
		{
			CT searchBoxControl = this.SearchBoxControl;
			searchBoxControl.BeginInit();
		}

		public void EndInit()
		{
			CT searchBoxControl = this.SearchBoxControl;
			searchBoxControl.EndInit();
		}
	}
}
