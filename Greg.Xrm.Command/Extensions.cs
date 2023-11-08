using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Greg.Xrm.Command
{
	public static class Extensions
	{
		public static void RegisterCommandExecutors(this IServiceCollection services, Assembly assembly)
		{
			var genericCommandExecutorType = typeof(ICommandExecutor<>);
			assembly
				.GetTypes()
				.Where(t => !t.IsAbstract && !t.IsInterface)
				.Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericCommandExecutorType))
				.ToList()
				.ForEach(t =>{
					var specificCommandExecutorType = t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericCommandExecutorType);
					services.AddTransient(specificCommandExecutorType, t);
				});
		}
	}
}
