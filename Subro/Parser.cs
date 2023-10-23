using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Subro
{
	internal static class Parser
	{
		public static string GetFieldName<RecordType, T>(Expression<Func<RecordType, T>> Field)
		{
			return GetFieldName(Field);
		}

		private static IEnumerable<MemberExpression> GetMembers(params Expression[] expr)
		{
			foreach (Expression e in expr)
			{
				Expression exp = e;
				if (exp is LambdaExpression)
					exp = (exp as LambdaExpression).Body;
				if (exp.NodeType == ExpressionType.Convert)
					exp = (exp as UnaryExpression).Operand;
				if (exp is MemberExpression)
					yield return (MemberExpression)exp;
				else if (exp is NewExpression)
				{
					foreach (MemberExpression me in from ne in ((NewExpression)exp).Arguments
					from m in GetMembers(new Expression[]
					{
						ne
					})
					select m)
					{
						yield return me;
					}
				}
			}
			yield break;
		}
	}
}
