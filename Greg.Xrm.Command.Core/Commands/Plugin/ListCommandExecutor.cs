using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Plugin;
using Microsoft.Xrm.Sdk.Query;
using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Plugin
{
	public class ListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository orgServiceRepo,
		ISolutionRepository solutionRepository,
		IAnsiConsole ansiConsole,
		IPluginAssemblyRepository assemblyRepo,
		IPluginPackageRepository packageRepo,
		IPluginTypeRepository typeRepo,
		ISdkMessageProcessingStepRepository stepRepo,
		ISdkMessageProcessingStepImageRepository imageRepo
	) : ICommandExecutor<ListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await orgServiceRepo.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			// Parse search term — trailing * → StartsWith, no star → Contains
			string searchTerm;
			ConditionOperator op;
			if (command.Name.EndsWith('*'))
			{
				searchTerm = command.Name[..^1];
				op = ConditionOperator.BeginsWith;
			}
			else
			{
				searchTerm = "%" + command.Name + "%";
				op = ConditionOperator.Like;
			}

			// Determine which levels to search
			bool searchAll = command.Level is null;
			bool searchPackage  = searchAll || command.Level == SearchLevel.Package;
			bool searchAssembly = searchAll || command.Level == SearchLevel.Assembly;
			bool searchType     = searchAll || command.Level == SearchLevel.Type;
			bool searchStep     = searchAll || command.Level == SearchLevel.Step;

			output.Write("Searching plugin registrations...");

			var packageSearchTask  = searchPackage  ? packageRepo.SearchByNameAsync(crm, searchTerm, op, cancellationToken)  : Task.FromResult(Array.Empty<PluginPackage>());
			var assemblySearchTask = searchAssembly ? assemblyRepo.SearchByNameAsync(crm, searchTerm, op, cancellationToken) : Task.FromResult(Array.Empty<PluginAssembly>());
			var typeSearchTask     = searchType     ? typeRepo.SearchByNameAsync(crm, searchTerm, op, cancellationToken)     : Task.FromResult(Array.Empty<PluginType>());
			var stepSearchTask     = searchStep     ? stepRepo.SearchByNameAsync(crm, searchTerm, op, includeInternalStages: false, cancellationToken) : Task.FromResult(Array.Empty<SdkMessageProcessingStep>());

			await Task.WhenAll(packageSearchTask, assemblySearchTask, typeSearchTask, stepSearchTask);

			var matchedPackages   = packageSearchTask.Result;
			var matchedAssemblies = assemblySearchTask.Result;
			var matchedTypes      = typeSearchTask.Result;
			var matchedSteps      = stepSearchTask.Result;

			output.WriteLine("Done", ConsoleColor.Green);

			// Which IDs matched at each level (used for pruning)
			var matchedPackageIds  = matchedPackages.Select(p => p.Id).ToHashSet();
			var matchedAssemblyIds = matchedAssemblies.Select(a => a.Id).ToHashSet();
			var matchedTypeIds     = matchedTypes.Select(t => t.Id).ToHashSet();
			var matchedStepIds     = matchedSteps.Select(s => s.Id).ToHashSet();

			// Collect all assembly IDs that need to be loaded for hierarchy building
			var assemblyIdsInScope = new HashSet<Guid>();
			assemblyIdsInScope.UnionWith(matchedAssemblies.Select(a => a.Id));
			assemblyIdsInScope.UnionWith(matchedTypes.Select(t => t.pluginassemblyid?.Id).OfType<Guid>());
			assemblyIdsInScope.UnionWith(matchedSteps.Select(s => s.assemblyidaliased).Where(id => id != Guid.Empty));

			// For matched packages, include all assemblies belonging to them
			if (matchedPackages.Length > 0)
			{
				var pkgAssemblyResults = await Task.WhenAll(
					matchedPackages.Select(p => assemblyRepo.GetByPackageIdAsync(crm, p.Id, cancellationToken)));
				foreach (var batch in pkgAssemblyResults)
					assemblyIdsInScope.UnionWith(batch.Select(a => a.Id));
			}

			if (assemblyIdsInScope.Count == 0)
			{
				output.WriteLine("No plugin registrations found matching the search criteria.", ConsoleColor.Yellow);
				return CommandResult.Success();
			}

			// Cache of assembly records already retrieved during search
			var cachedAssemblies = matchedAssemblies.ToDictionary(a => a.Id);

			// If a solution filter is specified, restrict to assemblies belonging to that solution
			if (!string.IsNullOrWhiteSpace(command.SolutionName))
			{
				output.Write($"Filtering by solution '{command.SolutionName}'...");
				var solution = await solutionRepository.GetByUniqueNameAsync(crm, command.SolutionName);
				if (solution == null)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail($"Solution '{command.SolutionName}' not found.");
				}

				var solutionAssemblies = await assemblyRepo.GetBySolutionIdAsync(crm, solution.Id, cancellationToken);
				var solutionAssemblyIds = solutionAssemblies.Select(a => a.Id).ToHashSet();
				assemblyIdsInScope.IntersectWith(solutionAssemblyIds);

				// Merge solution assembly records into cache so we don't reload them later
				foreach (var a in solutionAssemblies) cachedAssemblies[a.Id] = a;

				output.WriteLine("Done", ConsoleColor.Green);

				if (assemblyIdsInScope.Count == 0)
				{
					output.WriteLine("No plugin registrations found matching the search criteria in the specified solution.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}
			}

			output.Write($"Loading hierarchy for {assemblyIdsInScope.Count} assembly(ies)...");

			// Load any assembly records not yet in cache
			var missingIds = assemblyIdsInScope.Where(id => !cachedAssemblies.ContainsKey(id)).ToArray();
			if (missingIds.Length > 0)
			{
				var loaded = await assemblyRepo.GetByGuidsAsync(crm, missingIds, cancellationToken);
				foreach (var a in loaded) cachedAssemblies[a.Id] = a;
			}

			// For each assembly, load types + steps in parallel, then images
			var assembliesInScope = assemblyIdsInScope
				.Where(id => cachedAssemblies.ContainsKey(id))
				.Select(id => cachedAssemblies[id])
				.OrderBy(a => a.name)
				.ToArray();

			var typesByAssembly  = new Dictionary<Guid, PluginType[]>();
			var stepsByAssembly  = new Dictionary<Guid, SdkMessageProcessingStep[]>();
			var imagesByStep     = new Dictionary<Guid, SdkMessageProcessingStepImage[]>();

			await Task.WhenAll(assembliesInScope.Select(async asm =>
			{
				var types = await typeRepo.GetByAssemblyId(crm, asm.Id, cancellationToken);
				var steps = await stepRepo.GetByAssemblyIdAsync(crm, asm.Id, includeInternalStages: false, cancellationToken);
				lock (typesByAssembly) typesByAssembly[asm.Id] = types;
				lock (stepsByAssembly) stepsByAssembly[asm.Id] = steps;
			}));

			var allStepIds = stepsByAssembly.Values.SelectMany(s => s).Select(s => s.Id).ToArray();
			var allImages  = await imageRepo.GetByStepIdsAsync(crm, allStepIds);
			foreach (var img in allImages)
			{
				var stepId = img.sdkmessageprocessingstepid?.Id ?? Guid.Empty;
				if (stepId == Guid.Empty) continue;
				lock (imagesByStep)
				{
					if (!imagesByStep.TryGetValue(stepId, out var list))
						imagesByStep[stepId] = list = [];
					imagesByStep[stepId] = [.. list, img];
				}
			}

			output.WriteLine("Done", ConsoleColor.Green);
			output.WriteLine();

			// Collect package IDs referenced by assemblies, load package details
			var packageIdSet = assembliesInScope
				.Select(a => a.packageid?.Id)
				.OfType<Guid>()
				.ToHashSet();
			var cachedPackages = matchedPackages.ToDictionary(p => p.Id);
			var missingPkgIds = packageIdSet.Where(id => !cachedPackages.ContainsKey(id)).ToArray();
			if (missingPkgIds.Length > 0)
			{
				var loaded = await packageRepo.GetByGuidsAsync(crm, missingPkgIds, cancellationToken);
				foreach (var p in loaded) cachedPackages[p.Id] = p;
			}

			// Group assemblies: those with a package vs standalone
			var assembliesByPackage = assembliesInScope
				.Where(a => a.packageid?.Id != null)
				.GroupBy(a => a.packageid!.Id)
				.ToDictionary(g => g.Key, g => g.ToArray());

			var standAloneAssemblies = assembliesInScope
				.Where(a => a.packageid?.Id == null)
				.ToArray();

			// Visibility helpers (bottom-up pruning)
			// Images are always shown for any visible step — no name-based filtering on images.
			bool IsStepVisible(SdkMessageProcessingStep step, bool parentMatched) =>
				parentMatched || matchedStepIds.Contains(step.Id);

			bool IsTypeVisible(PluginType type, bool parentMatched)
			{
				if (parentMatched || matchedTypeIds.Contains(type.Id)) return true;
				var steps = stepsByAssembly.GetValueOrDefault(type.pluginassemblyid?.Id ?? Guid.Empty)
					?.Where(s => s.plugintypeid?.Id == type.Id) ?? [];
				return steps.Any(s => IsStepVisible(s, false));
			}

			bool IsAssemblyVisible(PluginAssembly asm, bool parentMatched)
			{
				if (parentMatched || matchedAssemblyIds.Contains(asm.Id)) return true;
				var types = typesByAssembly.GetValueOrDefault(asm.Id) ?? [];
				return types.Any(t => IsTypeVisible(t, false));
			}

			// Build and render Spectre tree
			var tree = new Tree(string.Empty);

			// Render a sub-tree for a single assembly
			void AddAssemblyNode(IHasTreeNodes parent, PluginAssembly asm, bool parentMatched)
			{
				if (!IsAssemblyVisible(asm, parentMatched)) return;

				bool asmMatched = parentMatched || matchedAssemblyIds.Contains(asm.Id);
				var asmNode = parent.AddNode($"[darkorange]{Markup.Escape($"{asm.name} - ID:{asm.Id:n}")}[/]");

				var types = typesByAssembly.GetValueOrDefault(asm.Id) ?? [];
				foreach (var type in types.OrderBy(t => t.name))
				{
					if (!IsTypeVisible(type, asmMatched)) continue;

					bool typeMatched = asmMatched || matchedTypeIds.Contains(type.Id);
					var typeNode = asmNode.AddNode($"[Green3]{Markup.Escape($"{type.name} - ID:{type.Id:n}")}[/]");

					var stepsForType = stepsByAssembly.GetValueOrDefault(asm.Id)?
						.Where(s => s.plugintypeid?.Id == type.Id)
						.OrderBy(s => s.name)
						.ToArray() ?? [];

					foreach (var step in stepsForType)
					{
						if (!IsStepVisible(step, typeMatched)) continue;

						var stepLabel = BuildStepLabel(step, type);
						var stepNode = typeNode.AddNode($"[LightGoldenrod2_2]{Markup.Escape(stepLabel)}[/]");

						// All images for the step are always shown
						var images = imagesByStep.GetValueOrDefault(step.Id) ?? [];
						foreach (var img in images.OrderBy(i => i.name))
						{
							var imageLabel = $"{img.name} ({GetImageTypeName(img.imagetype?.Value)}) - ID:{img.Id:n}";
							stepNode.AddNode($"[Gray46]{Markup.Escape(imageLabel)}[/]");
						}
					}
				}
			}

			// Packages
			foreach (var (pkgId, pkgAssemblies) in assembliesByPackage.OrderBy(kv => cachedPackages.TryGetValue(kv.Key, out var p) ? p.name : ""))
			{
				bool pkgMatched = matchedPackageIds.Contains(pkgId);
				if (!pkgMatched && !pkgAssemblies.Any(a => IsAssemblyVisible(a, false))) continue;

				var pkgName = cachedPackages.TryGetValue(pkgId, out var pkg) ? pkg.name : pkgId.ToString();
				var pkgNode = tree.AddNode($"[blue]{Markup.Escape(pkgName)}[/]");

				foreach (var asm in pkgAssemblies.OrderBy(a => a.name))
					AddAssemblyNode(pkgNode, asm, pkgMatched);
			}

			// Standalone assemblies (no package)
			foreach (var asm in standAloneAssemblies.OrderBy(a => a.name))
				AddAssemblyNode(tree, asm, false);

			ansiConsole.Write(tree);
			return CommandResult.Success();
		}

		private static string BuildStepLabel(SdkMessageProcessingStep step, PluginType type)
		{
			var typeName = type.name + ": ";
			var stepName = step.name;
			if (stepName.StartsWith(typeName, StringComparison.OrdinalIgnoreCase))
				stepName = stepName[typeName.Length..];

			var message = step.messagename;

			var entity  = step.primaryobjecttypecode;
			var stage   = GetStageName(step.stage?.Value);
			var suffix  = string.IsNullOrEmpty(entity) || entity == "none"
				? $"{message} ({stage})"
				: $"{message} of {entity} ({stage})";

			if (suffix.StartsWith(stepName, StringComparison.OrdinalIgnoreCase))
				return suffix + " - ID:" + step.Id.ToString("n");

			return $"{stepName} — {suffix} - ID:{step.Id:n}";
		}

		private static string GetStageName(int? stageValue) => stageValue switch
		{
			10 => "Pre-Validation",
			20 => "Pre-Operation",
			30 => "Post-Operation",
			40 => "Post-Operation, Async",
			_  => "Unknown"
		};

		private static string GetImageTypeName(int? imageType) => imageType switch
		{
			0 => "PreImage",
			1 => "PostImage",
			2 => "Both",
			_ => "Unknown"
		};
	}
}
