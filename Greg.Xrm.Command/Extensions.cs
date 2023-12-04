using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Greg.Xrm.Command
{
	public static class Extensions
	{
		public static void RegisterCommandExecutors(this IServiceCollection services, Assembly assembly)
		{
			var genericCommandExecutorType = typeof(ICommandExecutor<>);
#pragma warning disable S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension
			assembly
				.GetTypes()
				.Where(t => !t.IsAbstract && !t.IsInterface)
				.Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericCommandExecutorType))
				.ToList()
				.ForEach(t =>{
					var specificCommandExecutorType = t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericCommandExecutorType);
					services.AddTransient(specificCommandExecutorType, t);
				});
#pragma warning restore S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension
		}
	}
}
