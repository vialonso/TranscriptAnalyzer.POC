using TranscriptAnalyzer.POC.Application.Interfaces.Repositories;
using TranscriptAnalyzer.POC.Application.Interfaces.Services;

namespace TranscriptAnalyzer.POC.Application.Services
{
    public class BlobStorageService(IStorageAccountRepository storageAccountRepository) : IBlobStorageService
    {
        private readonly IStorageAccountRepository _storageAccountRepository = storageAccountRepository;

        public async Task<string> UploadFileAsync(Stream fileStream, string contentType)
        {
            // Generate a unique file name using a GUID and the current timestamp
            string fileName = $"{Guid.NewGuid()}_{DateTime.UtcNow.Ticks}.pdf";
            return await _storageAccountRepository.UploadFileAsync(fileStream, fileName, contentType);
        }
    }
}
