using System;
using System.Collections;

namespace Subro
{
	public interface IGenericComparer : IComparer, IEqualityComparer
	{
		IGenericComparer ThenBy(GenericComparer cmp);
	}
}
