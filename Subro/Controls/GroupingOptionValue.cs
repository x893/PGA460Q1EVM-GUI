using System;

namespace Subro.Controls
{
	[Serializable]
	public abstract class GroupingOptionValue
	{
		protected GroupingOptionValue(GroupingOption o)
		{
			Option = o;
		}

		public abstract bool IsDefault { get; }

		public abstract object GetValue();

		public abstract object GetDefaultValue();

		public abstract void Reset();

		internal abstract void CopyValue(GroupingOptionValue o);

		public abstract Type ValueType { get; }

		public abstract void SetValue(object value);
		public abstract bool Equals(GroupingOptionValue v);

		internal GroupingOptions Owner;
		public readonly GroupingOption Option;
	}
}
