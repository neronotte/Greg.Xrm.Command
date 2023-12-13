using System.Linq.Expressions;

namespace Greg.Xrm.Command
{
	/// <summary>
	/// Kudos to http://joelabrahamsson.com/getting-property-and-method-names-using-static-reflection-in-c/
	/// </summary>
	public static class StaticReflection
	{
		public static string GetMemberName<T, TProp>(
			this T instance,
			Expression<Func<T, TProp>> expression)
		{
			return GetMemberName(expression);
		}

		public static string GetMemberName<T, TProp>(
			Expression<Func<T, TProp>> expression)
		{
			if (expression == null)
			{
				throw new ArgumentException(
					"The expression cannot be null.");
			}

			return GetMemberName(expression.Body);
		}

		public static string GetMemberName<T>(
			this T instance,
			Expression<Action<T>> expression)
		{
			return GetMemberName(expression);
		}

		public static string GetMemberName<T>(
			Expression<Action<T>> expression)
		{
			if (expression == null)
			{
				throw new ArgumentException(
					"The expression cannot be null.");
			}

			return GetMemberName(expression.Body);
		}

		private static string GetMemberName(
			Expression expression)
		{
			if (expression == null)
			{
				throw new ArgumentException(
					"The expression cannot be null.");
			}

			if (expression is MemberExpression)
			{
				// Reference type property or field
				var memberExpression =
					(MemberExpression)expression;
				return memberExpression.Member.Name;
			}

			if (expression is MethodCallExpression)
			{
				// Reference type method
				var methodCallExpression =
					(MethodCallExpression)expression;
				return methodCallExpression.Method.Name;
			}

			if (expression is UnaryExpression)
			{
				// Property, field of method returning value type
				var unaryExpression = (UnaryExpression)expression;
				return GetMemberName(unaryExpression);
			}

			throw new ArgumentException("Invalid expression");
		}

		private static string GetMemberName(
			UnaryExpression unaryExpression)
		{
			if (unaryExpression.Operand is MethodCallExpression)
			{
				var methodExpression =
					(MethodCallExpression)unaryExpression.Operand;
				return methodExpression.Method.Name;
			}

			return ((MemberExpression)unaryExpression.Operand)
				.Member.Name;
		}
	}
}
