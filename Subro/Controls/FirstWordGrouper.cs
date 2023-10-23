using System;

namespace Subro.Controls
{
	public class FirstWordGrouper : StringGroupWrapper
	{
		public FirstWordGrouper(GroupingInfo Grouper) : base(Grouper)
		{
		}

		protected override string GetValue(string s)
		{
			int num = s.IndexOfAny(FirstWordGrouper.EndOfWordChars);
			string result;
			if (num == -1)
			{
				result = s;
			}
			else
			{
				result = s.Substring(0, num);
			}
			return result;
		}

		internal static char[] EndOfWordChars = new char[]
		{
			' ',
			'\r',
			'\n',
			'\t'
		};
	}
}
