using System;

namespace Subro.Controls
{
	public class StartLetterGrouper : StringGroupWrapper
	{
		public StartLetterGrouper(GroupingInfo Grouper) : this(Grouper, 1)
		{
		}

		public StartLetterGrouper(GroupingInfo grouper, int letters) : base(grouper)
		{
			Letters = letters;
		}

		protected override string GetValue(string s)
		{
			string result;
			if (s.Length < Letters)
				result = s;
			else
				result = s.Substring(0, Letters);
			return result;
		}

		public readonly int Letters;
	}
}
