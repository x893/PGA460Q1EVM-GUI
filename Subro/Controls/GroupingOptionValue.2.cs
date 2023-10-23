using System;
using System.Collections.Generic;

namespace Subro.Controls
{
	[Serializable]
	internal class GroupingOptionValue<T> : GroupingOptionValue
	{
		public GroupingOptionValue(T Default, GroupingOption o) : base(o)
		{
			DefaultValue = Default;
			m_value = DefaultValue;
		}

		public T Value
		{
			get
			{
				return m_value;
			}
			set
			{
				if (!Equals(value, m_value))
				{
					m_value = value;
					Owner.NotifyChanged(Option);
				}
			}
		}

		private bool Equals(T t1, T t2)
		{
			if (m_eq == null)
				m_eq = EqualityComparer<T>.Default;
			return m_eq.Equals(t1, t2);
		}

		public override bool Equals(GroupingOptionValue v)
		{
			bool result;
			if (v == null)
			{
				result = false;
			}
			else
			{
				object obj = v.GetValue();
				result = (obj is T && Equals(m_value, (T)((object)obj)));
			}
			return result;
		}

		public override bool IsDefault
		{
			get
			{
				return Equals(m_value, DefaultValue);
			}
		}

		public override Type ValueType
		{
			get
			{
				return typeof(T);
			}
		}

		public override object GetValue()
		{
			return m_value;
		}

		public override void SetValue(object value)
		{
			Value = (T)((object)value);
		}

		public override object GetDefaultValue()
		{
			return DefaultValue;
		}

		internal override void CopyValue(GroupingOptionValue o)
		{
			Value = ((GroupingOptionValue<T>)o).m_value;
		}

		public override void Reset()
		{
			Value = DefaultValue;
		}

		[NonSerialized]
		public T DefaultValue;

		private T m_value;

		[NonSerialized]
		private EqualityComparer<T> m_eq;
	}
}
