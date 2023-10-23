using System;
using System.Collections;

namespace Subro
{
	public class GenericComparer : IGenericComparer, IComparer, IEqualityComparer
	{
		public GenericComparer(Type Type)
		{
			this.Type = Type;
		}

		public Type Type
		{
			get
			{
				return this.type;
			}
			set
			{
				if (!(this.type == value))
				{
					if (value == null)
						throw new ArgumentNullException();
					this.type = value;
					this.reset();
				}
			}
		}

		public Type TargetType
		{
			get
			{
				Type result;
				if (this.targettype == null)
					result = this.type;
				else
					result = this.targettype;
				return result;
			}
			set
			{
				if (!(this.TargetType == value))
				{
					this.targettype = value;
					this.reset();
				}
			}
		}

		private void reset()
		{
			this.comp = null;
			this.eq = null;
		}

		public bool Descending
		{
			get
			{
				return this.factor < 0;
			}
			set
			{
				this.factor = (value ? -1 : 1);
			}
		}

		public int Compare(object x, object y)
		{
			int result;
			if (x == y)
				result = 0;
			else if (x == null)
				result = -this.factor;
			else if (y == null)
				result = this.factor;
			else
			{
				if (this.type == null)
					this.Type = x.GetType();
				if (this.comp == null)
					this.comp = CompareFunctions.GetComparer(this.type, this.TargetType);
				result = this.factor * this.comp.Compare(x, y);
			}
			return result;
		}

		public new bool Equals(object x, object y)
		{
			bool result;
			if (x == y)
				result = true;
			else if (x == null || y == null)
				result = false;
			else
			{
				if (this.type == null)
					this.Type = x.GetType();
				if (this.eq == null)
					this.eq = CompareFunctions.GetEqualityComparer(this.type, this.TargetType);
				result = this.eq.Equals(x, y);
			}
			return result;
		}

		public int GetHashCode(object obj)
		{
			int result;
			if (obj == null)
				result = 0;
			else
				result = obj.GetHashCode();
			return result;
		}

		public IGenericComparer ThenBy(GenericComparer cmp)
		{
			return new GenericComparers
			{
				cmp
			};
		}

		private Type type;
		private Type targettype;
		private IComparer comp;
		private IEqualityComparer eq;
		private int factor = 1;
	}
}
