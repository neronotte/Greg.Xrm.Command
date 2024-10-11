using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using System.Diagnostics;

namespace Greg.Xrm.Command.Model
{
	public class TemporarySolution : ITemporarySolution
	{
		private readonly IOrganizationServiceAsync2 crm;
		private readonly IOutput output;
		private readonly Solution solution;
		private bool disposedValue;

		public TemporarySolution(IOrganizationServiceAsync2 crm, IOutput output, Solution solution)
		{
			this.crm = crm;
			this.output = output;
			this.solution = solution;
		}

		public async Task AddComponentAsync(Guid componentId, ComponentType componentType)
		{
			if (solution.IsDeleted)
				throw new InvalidOperationException("Operation cannot be performed, the solution is deleted!");

			output.Write($"Adding {componentType}:{componentId} to solution...");
			try
			{
				var request = new AddSolutionComponentRequest
				{
					SolutionUniqueName = solution.uniquename,
					ComponentId = componentId,
					ComponentType = (int)componentType
				};

				await crm.ExecuteAsync(request);
				output.WriteLine("DONE", ConsoleColor.Green);
			}
			catch
			{
				output.WriteLine("ERROR", ConsoleColor.Red);
				throw;
			}
		}


		public async Task<byte[]> DownloadAsync()
		{
			if (solution.IsDeleted)
				throw new InvalidOperationException("Operation cannot be performed, the solution is deleted!");


			output.Write($"Downloading solution {this.solution.uniquename}...");
			var sw = Stopwatch.StartNew();
			try
			{
				var request = new ExportSolutionRequest
				{
					SolutionName = solution.uniquename,
					Managed = false,
				};

				var response = (ExportSolutionResponse)await crm.ExecuteAsync(request);
				sw.Stop();
				output.WriteLine("DONE in " + sw.Elapsed, ConsoleColor.Green);
				return response.ExportSolutionFile;
			}
			catch
			{
				sw.Stop();
				output.WriteLine("ERROR", ConsoleColor.Red);
				throw;
			}
		}

		public async Task UploadAndPublishAsync(byte[] zipFile, string tableName)
		{
			if (solution.IsDeleted)
				throw new InvalidOperationException("Operation cannot be performed, the solution is deleted!");
			var sw = Stopwatch.StartNew();
			try
			{
				output.Write($"Uploading solution {this.solution.uniquename}...");
				var request = new ImportSolutionRequest
				{
					CustomizationFile = zipFile,
					OverwriteUnmanagedCustomizations = true,	
				};

				await crm.ExecuteAsync(request);
				sw.Stop();
				output.WriteLine("DONE in " + sw.Elapsed, ConsoleColor.Green);



				output.Write($"Publishing customizations...");
				sw.Restart();

				var builder = new PublishXmlBuilder();
				builder.AddTable(tableName);
				var request2 = builder.Build();

				await crm.ExecuteAsync(request2);
				sw.Stop();
				output.WriteLine("DONE in " + sw.Elapsed, ConsoleColor.Green);
			}
			catch
			{
				sw.Stop();
				output.WriteLine("ERROR", ConsoleColor.Red);
				throw;
			}
		}


		public async Task DeleteAsync()
		{
			if (solution.IsDeleted)
				return;

			try
			{
				output.Write("Deleting holding solution...");
				await solution.DeleteAsync(crm);
				output.WriteLine("DONE", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine($"ERROR: {ex.Message}", ConsoleColor.Red);
				output.WriteLine("Please remove the holding solution manually from the environment", ConsoleColor.DarkYellow);
			}
		}


		public void Delete()
		{
			if (solution.IsDeleted)
				return;

			try
			{
				output.Write("Deleting holding solution...");
				solution.Delete(crm);
				output.WriteLine("DONE", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine($"ERROR: {ex.Message}", ConsoleColor.Red);
				output.WriteLine("Please remove the holding solution manually from the environment", ConsoleColor.DarkYellow);
			}
		}

		#region IDisposable implementation

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					this.Delete();
				}

				disposedValue = true;
			}
		}

		~TemporarySolution()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion

		public override string ToString()
		{
			return this.solution.uniquename;
		}
	}
}
