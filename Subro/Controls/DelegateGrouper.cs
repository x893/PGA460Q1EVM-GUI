using System;

namespace Subro.Controls
{
	public class DelegateGrouper<T> : GroupingInfo
	{
		public DelegateGrouper(Func<T, object> groupProvider, string name)
		{
			if (groupProvider == null)
				throw new ArgumentNullException();
			Name = name;
			if (name == null)
				Name = groupProvider.ToString();
			GroupProvider = groupProvider;
		}

		public override string ToString()
		{
			return Name;
		}

		public override object GetGroupValue(object Row)
		{
			return GroupProvider((T)((object)Row));
		}

		public readonly string Name;
		public readonly Func<T, object> GroupProvider;
	}
}
