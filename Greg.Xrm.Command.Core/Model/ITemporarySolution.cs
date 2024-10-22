namespace Greg.Xrm.Command.Model
{
    /// <summary>
    /// Represents a temporary solution used to store components during a command execution.
    /// It should get removed when the command execution is completed.
    /// Implements the IDisposable pattern to be used with "using" construct.
    /// </summary>
    public interface ITemporarySolution : IDisposable
    {
        /// <summary>
        /// Adds a component to the solution.
        /// </summary>
        /// <param name="componentId">The unique identifier of the component to add to the solution.</param>
        /// <param name="componentType">The type of component</param>
        /// <returns>A task</returns>
        Task AddComponentAsync(Guid componentId, ComponentType componentType);


        /// <summary>
        /// Deletes the current solution
        /// </summary>
        Task DeleteAsync();

		/// <summary>
		/// Deletes the current solution
		/// </summary>
		void Delete();


		/// <summary>
		/// Downloads the solution as a zip file.
		/// </summary>
		/// <returns></returns>
		Task<SolutionZipArchive> DownloadAsync();

		/// <summary>
		/// Uploads a new version of a given solution.
		/// </summary>
		/// <param name="zipFile">The zip file to upload</param>
		/// <param name="tableName">The name of the table to which the form belongs</param>
		/// <returns></returns>
		Task UploadAndPublishAsync(byte[] zipFile, string tableName);
	}
}
