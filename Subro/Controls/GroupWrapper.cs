using System;
using System.ComponentModel;

namespace Subro.Controls
{
	public abstract class GroupWrapper : GroupingInfo
	{
		public GroupWrapper(GroupingInfo grouper) : this(grouper, true)
		{
		}

		public GroupWrapper(GroupingInfo grouper, bool RemovePreviousWrappers)
		{
			if (grouper == null)
				throw new ArgumentNullException();

			if (RemovePreviousWrappers)
				while (grouper is GroupWrapper)
					grouper = ((GroupWrapper)grouper).Grouper;

			Grouper = grouper;
		}

		public override string ToString()
		{
			return Grouper.ToString();
		}

		public override bool IsProperty(PropertyDescriptor p)
		{
			return Grouper.IsProperty(p);
		}

		public override object GetGroupValue(object Row)
		{
			return GetValue(Grouper.GetGroupValue(Row));
		}

		public override Type GroupValueType
		{
			get
			{
				return Grouper.GroupValueType;
			}
		}

		protected abstract object GetValue(object GroupValue);
		public readonly GroupingInfo Grouper;
	}
}
