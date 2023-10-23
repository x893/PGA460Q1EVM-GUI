using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Subro
{
	public class GenericComparers : List<GenericComparer>, IGenericComparer, IComparer, IEqualityComparer
	{
		public int Compare(object x, object y)
		{
			return this.Compare(x, y);
		}

		public new bool Equals(object x, object y)
		{
			return this.All((GenericComparer c) => c.Equals(x, y));
		}

		public int GetHashCode(object obj)
		{
			int result;
			if (obj == null)
			{
				result = 0;
			}
			else
			{
				result = obj.GetHashCode();
			}
			return result;
		}

		public IGenericComparer ThenBy(GenericComparer cmp)
		{
			base.Add(cmp);
			return this;
		}
	}
}
