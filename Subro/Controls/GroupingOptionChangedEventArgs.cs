using System;

namespace Subro.Controls
{
	public class GroupingOptionChangedEventArgs : EventArgs
	{
		public GroupingOptionChangedEventArgs(GroupingOption option)
		{
			Option = option;
		}

		public readonly GroupingOption Option;
	}
}
