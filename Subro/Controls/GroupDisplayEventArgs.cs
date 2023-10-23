using System;
using System.ComponentModel;
using System.Drawing;

namespace Subro.Controls
{
	public class GroupDisplayEventArgs : CancelEventArgs
	{
		public GroupDisplayEventArgs(GroupRow Row, GroupingInfo Info)
		{
			Group = Row;
			GroupingInfo = Info;
		}

		public object Value
		{
			get
			{
				return Group.Value;
			}
		}

		public string DisplayValue { get; set; }
		public string Header { get; set; }
		public string Summary { get; set; }
		public Color BackColor { get; set; }
		public Color ForeColor { get; set; }
		public Font Font { get; set; }
		public bool Selected { get; internal set; }

		public override string ToString()
		{
			string result;
			if (Summary == null)
				result = DisplayValue;
			else
				result = string.Format("{0}   {1}", DisplayValue, Summary);
			return result;
		}

		public GroupRow Row
		{
			get
			{
				return Group;
			}
		}

		public readonly GroupRow Group;
		public readonly GroupingInfo GroupingInfo;
	}
}
