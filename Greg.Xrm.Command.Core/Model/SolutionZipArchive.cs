using System.IO.Compression;
using System.Xml.Linq;

namespace Greg.Xrm.Command.Model
{
	/// <summary>
	/// This class represents a solution zip file stored in memory.
	/// Allows to read and update the content of the solution zip file.
	/// </summary>
	public class SolutionZipArchive : IDisposable
	{
        private readonly MemoryStream archiveStream = new();
		private bool disposedValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="SolutionZipArchive"/> class.
		/// </summary>
		/// <param name="byteContent">The contents of the solution zip file.</param>
		/// <exception cref="ArgumentNullException">If the provided array is null or has length = 0</exception>
		public SolutionZipArchive(byte[] byteContent)
        {
            if (byteContent == null || byteContent.Length == 0)
				throw new ArgumentNullException(nameof(byteContent));

            this.archiveStream.Write(byteContent, 0, byteContent.Length);
			this.disposedValue = false;
		}


		/// <summary>
		/// Saves the content of the solution zip file to the specified path.
		/// </summary>
		/// <param name="path">The full path of the file that will be created/updated with the solution contents</param>
		/// <returns>A task to process the operation asyncronously</returns>
		/// <exception cref="ObjectDisposedException">If current object has been already disposed</exception>
		public async Task SaveToAsync(string path)
		{
			if (disposedValue) throw new ObjectDisposedException(nameof(SolutionZipArchive));

			using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
			this.archiveStream.Position = 0;
			await this.archiveStream.CopyToAsync(fileStream);
		}

		/// <summary>
		/// Returns the content of the solution zip file as a byte array.
		/// </summary>
		/// <returns>The content of the solution zip file as a byte array.</returns>
		/// <exception cref="ObjectDisposedException">If current object has been already disposed</exception>
		public byte[] ToArray()
		{
			if (disposedValue) throw new ObjectDisposedException(nameof(SolutionZipArchive));

			this.archiveStream.Position = 0;
			return this.archiveStream.ToArray();
		}


		/// <summary>
		/// Reads the content of the specified entry in the solution zip file,
		/// and allows to process the content as an XDocument.
		/// </summary>
		/// <param name="entryName">The name of the entry to process</param>
		/// <param name="callback">
		/// The callback that should be invoked to manipulate the entry xml. 
		/// It must return <c>True</c> if something has been changed, <c>False</c> otherwise.
		/// </param>
		/// <returns>A value indicating whether something has been actually changed or not</returns>
		/// <exception cref="ArgumentNullException">If one of entryName or callback are null</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the solution file does not contains an entry with the given name</exception>
		/// <exception cref="ObjectDisposedException">If current object has been already disposed</exception>
		public bool UpdateEntryXml(string entryName, Func<XDocument, bool> callback)
		{
			if (disposedValue) throw new ObjectDisposedException(nameof(SolutionZipArchive));
			if (string.IsNullOrWhiteSpace(entryName)) throw new ArgumentNullException(nameof(entryName));
			if (callback == null) throw new ArgumentNullException(nameof(callback));


			bool hasBeenUpdated = false;
			using (var archive = new ZipArchive(this.archiveStream, ZipArchiveMode.Update, true))
			{
				var entry = archive.GetEntry(entryName) ??
						throw new ArgumentOutOfRangeException(nameof(entryName), $"The {entryName} file is not found in the solution archive.");

				XDocument doc;
				using (var entryStream = entry.Open())
				{
					doc = XDocument.Load(entryStream);

					hasBeenUpdated = callback(doc);
				}

				if (hasBeenUpdated)
				{
					entry.Delete();
					entry = archive.CreateEntry(entryName);
					using var entryStream = entry.Open();
					doc.Save(entryStream);
				}
			}
				

			return hasBeenUpdated;
		}


		#region IDisposable Interface

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					this.archiveStream.Dispose();
				}
				disposedValue = true;
			}
		}

		/// <summary>
		/// Implements the disposable pattern
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
