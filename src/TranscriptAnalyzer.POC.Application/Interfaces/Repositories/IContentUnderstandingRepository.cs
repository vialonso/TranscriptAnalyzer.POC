using TranscriptAnalyzer.POC.Domain.Entities;

namespace TranscriptAnalyzer.POC.Application.Interfaces.Repositories
{
    public interface IContentUnderstandingRepository
    {
        Task<List<string>> GetAnalyzers();
        Task<List<TranscriptCourse>> TryGetTranscriptCoursesFromDocument(string fileUrl, string analyzerId);
    }
}
