using System;
using System.ComponentModel;

namespace Subro.Controls
{
	public class PropertyGrouper : GroupingInfo
	{
		public PropertyGrouper(PropertyDescriptor property)
		{
			if (property == null)
				throw new ArgumentNullException();
			Property = property;
		}

		public override bool IsProperty(PropertyDescriptor p)
		{
			return p == Property || (p != null && p.Name == Property.Name);
		}

		public override object GetGroupValue(object Row)
		{
			return Property.GetValue(Row);
		}

		public override string ToString()
		{
			return Property.Name;
		}

		public override Type GroupValueType
		{
			get
			{
				return Property.PropertyType;
			}
		}

		public readonly PropertyDescriptor Property;
	}
}
