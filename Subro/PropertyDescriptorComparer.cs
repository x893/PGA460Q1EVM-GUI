using System.ComponentModel;

namespace Subro
{
	public class PropertyDescriptorComparer : GenericComparer
	{
		public PropertyDescriptorComparer(PropertyDescriptor Prop, bool Descending) : base(Prop.PropertyType)
		{
			this.Prop = Prop;
			base.Descending = Descending;
		}

		public readonly PropertyDescriptor Prop;
	}
}
