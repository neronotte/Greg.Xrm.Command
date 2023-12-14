using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Model
{
    public class DependencyList : List<Dependency>
	{
		public DependencyList()
		{
		}

		public DependencyList(IEnumerable<Dependency> collection) : base(collection)
		{
		}

		public DependencyList(int capacity) : base(capacity)
		{
		}


		public IReadOnlyList<Dependency> OfType(ComponentType componentType)
		{
			return this.Where(x => x.dependentcomponenttype.Value == (int)componentType).ToArray();
		}


		public void WriteTo(IOutput output)
		{
			var dependencyGroups = this.GroupBy(x => x.dependentcomponenttype.Value)
				.OrderBy(x => x.First().DependentComponentTypeFormatted)
				.ToArray();

			foreach (var dependencyGroup in dependencyGroups)
			{
				var componentTypeName = dependencyGroup.First().DependentComponentTypeFormatted;


				output.WriteLine()
					.Write(componentTypeName, ConsoleColor.Cyan)
					.Write(" (typeCode: ", ConsoleColor.DarkGray)
					.Write(dependencyGroup.First().dependentcomponenttype.Value, ConsoleColor.DarkGray)
					.Write(", count: ", ConsoleColor.DarkGray)
					.Write(dependencyGroup.Count(), ConsoleColor.DarkGray)
					.Write(")", ConsoleColor.DarkGray)
					.WriteLine();

				foreach (var dependency in dependencyGroup.OrderBy(x => x.dependentcomponentobjectid))
				{
					output
						.Write(dependency.dependentcomponentobjectid, ConsoleColor.Yellow)
						.Write(" | ")
						.Write(dependency.DependentComponentLabel);

					if (!string.IsNullOrWhiteSpace(dependency.DependencyTypeFormatted))
					{
						output
							.Write(" (", ConsoleColor.DarkGray)
							.Write(dependency.DependencyTypeFormatted, ConsoleColor.DarkGray)
							.Write(")", ConsoleColor.DarkGray);
					}
					output.WriteLine();
				}
			}
		}
	}
}
