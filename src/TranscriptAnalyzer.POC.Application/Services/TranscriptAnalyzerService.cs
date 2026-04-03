using TranscriptAnalyzer.POC.Application.Interfaces.Repositories;
using TranscriptAnalyzer.POC.Application.Interfaces.Services;
using TranscriptAnalyzer.POC.Domain.Entities;

namespace TranscriptAnalyzer.POC.Application.Services
{
    public class TranscriptAnalyzerService(IContentUnderstandingRepository contentUnderstandingRepository) : ITranscriptAnalyzerService
    {
        private readonly IContentUnderstandingRepository _contentUnderstandingRepository = contentUnderstandingRepository;

        public async Task<List<string>> GetAnalyzers()
        {
            return await _contentUnderstandingRepository.GetAnalyzers();
        }

        public async Task<List<TranscriptCourse>> GetTranscriptCourses(string fileUrl, string analyzerId)
        {
            var result = await _contentUnderstandingRepository.TryGetTranscriptCoursesFromDocument(fileUrl, analyzerId);
            return result;
        }
    }
}
