namespace TranscriptAnalyzer.POC.Application.Interfaces.Services
{
    public interface IBlobStorageService
    {
        /// <summary>
        /// Asynchronously uploads a file to the remote storage and returns the URL of the uploaded file.
        /// </summary>
        /// <param name="fileStream">The stream containing the file data to upload. The stream must be readable and positioned at the beginning
        /// of the file.</param>
        /// <param name="contentType">The MIME type of the file to upload. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the URL of the uploaded file as
        /// a string.</returns>
        Task<string> UploadFileAsync(Stream fileStream, string contentType);
    }
}
