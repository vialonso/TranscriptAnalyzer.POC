using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using TranscriptAnalyzer.POC.Application.Interfaces.Repositories;

namespace TranscriptAnalyzer.POC.Infrastructure.Repositories
{
    public class StorageAccountRepository(BlobServiceClient blobServiceClient) : IStorageAccountRepository
    {
        private readonly BlobServiceClient _blobServiceClient = blobServiceClient;
        private readonly string _containerName = "uploads";

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            var blobClient = containerClient.GetBlobClient(fileName);
            var blobHttpHeader = new BlobHttpHeaders { ContentType = contentType };
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
            return blobClient.Uri.ToString();
        }
    }
}
