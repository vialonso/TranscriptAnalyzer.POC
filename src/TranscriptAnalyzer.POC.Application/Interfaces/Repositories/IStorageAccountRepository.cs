namespace TranscriptAnalyzer.POC.Application.Interfaces.Repositories
{
    public interface IStorageAccountRepository
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    }
}
