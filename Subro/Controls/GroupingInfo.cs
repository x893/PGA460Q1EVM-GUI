using System;
using System.ComponentModel;

namespace Subro.Controls
{
	public abstract class GroupingInfo
	{
		public abstract object GetGroupValue(object Row);

		public virtual bool IsProperty(PropertyDescriptor p)
		{
			return p != null && this.IsProperty(p.Name);
		}

		public virtual bool IsProperty(string Name)
		{
			return Name == this.ToString();
		}

		public static implicit operator GroupingInfo(PropertyDescriptor p)
		{
			return new PropertyGrouper(p);
		}

		public virtual Type GroupValueType
		{
			get
			{
				return typeof(object);
			}
		}

		public virtual void SetDisplayValues(GroupDisplayEventArgs e)
		{
			object value = e.Value;
			e.DisplayValue = ((value == null) ? "<Null>" : value.ToString());
		}
	}
}
