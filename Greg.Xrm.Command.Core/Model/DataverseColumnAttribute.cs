using System.Reflection;

namespace Greg.Xrm.Command.Model
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple =false, Inherited =true)]
	public class DataverseColumnAttribute : Attribute
	{


		public static string[] GetFromClass<T>() where T : class
		{
			return GetFromClass(typeof(T));
		}

		public static string[] GetFromClass(Type type)
		{
			return type.GetProperties().Where(p => p.GetCustomAttribute<DataverseColumnAttribute>() != null).Select(p => p.Name).ToArray();
		}
	}
}
