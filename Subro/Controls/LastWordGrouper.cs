using System;

namespace Subro.Controls
{
	public class LastWordGrouper : StringGroupWrapper
	{
		public LastWordGrouper(GroupingInfo Grouper) : base(Grouper)
		{
		}

		protected override string GetValue(string s)
		{
			int num = s.LastIndexOfAny(FirstWordGrouper.EndOfWordChars);
			string result;
			if (num == -1)
			{
				result = s;
			}
			else
			{
				result = s.Substring(num + 1);
			}
			return result;
		}
	}
}
