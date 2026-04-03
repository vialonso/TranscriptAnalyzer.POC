namespace TranscriptAnalyzer.POC.Application.Interfaces.Services
{
    public interface ITranscriptAnalyzerService
    {
        Task<List<Domain.Entities.TranscriptCourse>> GetTranscriptCourses(string fileUrl);
    }
}
