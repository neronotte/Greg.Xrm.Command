using System;
using Microsoft.Xrm.Sdk;

namespace PacxIntegration
{
	/// <summary>
	/// Dataverse Custom API plugin for nn_PacxSumThree.
	/// Reads three integer request parameters (Addend1, Addend2, Addend3)
	/// and returns their sum as the 'Sum' response property.
	/// </summary>
	public class SumThreePlugin : IPlugin
	{
		private const string ApiName    = "nn_PacxSumThree";
		private const string InAddend1  = ApiName + "-in-Addend1";
		private const string InAddend2  = ApiName + "-in-Addend2";
		private const string InAddend3  = ApiName + "-in-Addend3";
		private const string OutSum     = ApiName + "-out-Sum";

		public void Execute(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
				throw new ArgumentNullException(nameof(serviceProvider));

			var context = (IPluginExecutionContext)
				serviceProvider.GetService(typeof(IPluginExecutionContext));

			int addend1 = GetInt(context, InAddend1);
			int addend2 = GetInt(context, InAddend2);
			int addend3 = GetInt(context, InAddend3);

			context.OutputParameters[OutSum] = addend1 + addend2 + addend3;
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
