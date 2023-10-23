using System;

namespace Subro.Controls
{
	public class StringGroupWrapper : GroupWrapper
	{
		public StringGroupWrapper(GroupingInfo Grouper) : base(Grouper)
		{
		}

		protected override object GetValue(object groupValue)
		{
			object result;
			if (groupValue == null)
				result = null;
			else
				result = GetValue(groupValue.ToString());
			return result;
		}

		public override Type GroupValueType
		{
			get
			{
				return typeof(string);
			}
		}

		protected virtual string GetValue(string s)
		{
			return s;
		}
	}
}
