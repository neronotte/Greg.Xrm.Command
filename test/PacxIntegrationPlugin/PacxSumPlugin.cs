using System;
using Microsoft.Xrm.Sdk;

namespace PacxIntegration
{
	/// <summary>
	/// Dataverse Custom API plugin for nn_PacxSum.
	/// Reads two integer request parameters (Addend1, Addend2) via their uniquename keys
	/// and returns their sum as the 'Result' response property.
	/// </summary>
	public class PacxSumPlugin : IPlugin
	{
		// Dataverse uses the customapirequestparameter.uniquename as the InputParameters key
		private const string InAddend1 = "nn_PacxSum-in-Addend1";
		private const string InAddend2 = "nn_PacxSum-in-Addend2";
		private const string OutResult = "nn_PacxSum-out-Result";

		public void Execute(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
				throw new ArgumentNullException(nameof(serviceProvider));

			var context = (IPluginExecutionContext)
				serviceProvider.GetService(typeof(IPluginExecutionContext));

			int addend1 = GetInt(context, InAddend1);
			int addend2 = GetInt(context, InAddend2);

			context.OutputParameters[OutResult] = addend1 + addend2;
		}

		private static int GetInt(IPluginExecutionContext context, string key)
		{
			if (!context.InputParameters.Contains(key))
				throw new InvalidPluginExecutionException(
					$"Required input parameter '{key}' is missing.");

			return (int)context.InputParameters[key];
		}
	}
}
