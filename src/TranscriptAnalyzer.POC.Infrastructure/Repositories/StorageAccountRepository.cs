using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TranscriptAnalyzer.POC.Application.Interfaces.Repositories;

namespace TranscriptAnalyzer.POC.Infrastructure.Repositories
{
    public class StorageAccountRepository(
        ILogger<StorageAccountRepository> logger,
        BlobServiceClient blobServiceClient
        ) : IStorageAccountRepository
    {
        private readonly ILogger<StorageAccountRepository> _logger = logger;
        private readonly BlobServiceClient _blobServiceClient = blobServiceClient;
        private readonly string _containerName = "uploads";

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            long? fileSize = fileStream.CanSeek ? fileStream.Length : null;
            _logger.LogInformation("Starting file upload to Azure Blob Storage. FileName: {FileName}, ContentType: {ContentType}, FileSize: {FileSize}", fileName, contentType, fileSize);

            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                var blobClient = containerClient.GetBlobClient(fileName);
                var blobHttpHeader = new BlobHttpHeaders { ContentType = contentType };
                await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
                sw.Stop();

                _logger.LogInformation("File upload completed in {ElapsedMilliseconds} ms. Blob URL: {BlobUrl}", sw.ElapsedMilliseconds, blobClient.Uri);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "File upload failed after {ElapsedMilliseconds} ms. FileName: {FileName}", sw.ElapsedMilliseconds, fileName);
                throw;
            }
        }
    }
}
