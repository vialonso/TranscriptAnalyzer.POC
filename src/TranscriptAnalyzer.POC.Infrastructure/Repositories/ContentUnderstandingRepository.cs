using Azure;
using Azure.AI.ContentUnderstanding;
using TranscriptAnalyzer.POC.Application.Interfaces.Repositories;
using TranscriptAnalyzer.POC.Domain.Entities;

namespace TranscriptAnalyzer.POC.Infrastructure.Repositories
{
    public class ContentUnderstandingRepository(ContentUnderstandingClient contentUnderstandingClient) : IContentUnderstandingRepository
    {
        private readonly ContentUnderstandingClient _client = contentUnderstandingClient;

        public async Task<List<string>> GetAnalyzers()
        {
            List<string> result = [];
            var analyzers = _client.GetAnalyzersAsync();
            var contentAnalyzers = await analyzers.ToListAsync();
            foreach (var item in contentAnalyzers.Where(e => e.AnalyzerId.Contains("document") || e.AnalyzerId.Contains("transcript")).ToList())
                result.Add(item.AnalyzerId);

            return result;
        }

        public async Task<List<TranscriptCourse>> TryGetTranscriptCoursesFromDocument(string fileUrl, string analyzerId)
        {
            var analyzeOperation = await _client.AnalyzeAsync(
            WaitUntil.Completed,
            analyzerId,
            inputs: [
                new AnalysisInput { Uri = new Uri(fileUrl) }
            ]);


            var analyzeResult = analyzeOperation.Value;

            List<TranscriptCourse> transcriptCourses = [];

            if (analyzeResult.Contents?.FirstOrDefault() is DocumentContent content) 
            {
                if (content.Fields.TryGetValue("Courses", out var coursesField))
                {
                    var coursesArrayField = coursesField is ContentArrayField sf ? sf : null;

                    coursesArrayField?.Value?.ToList().ForEach(course =>
                        {
                            if (course is ContentObjectField courseObject)
                            {
                                TranscriptCourse transcriptCourse = new()
                                {
                                    Month = courseObject["Month"] is ContentIntegerField monthNumber ? (int)(monthNumber?.Value ?? 0) : 0,
                                    Year = courseObject["Year"] is ContentIntegerField yearNumber ? (int)(yearNumber?.Value ?? 0) : 0,
                                    Code = courseObject["Code"] is ContentStringField codeString ? codeString.Value : null,
                                    Title = courseObject["Title"] is ContentStringField titleString ? titleString.Value : null,
                                    Credits = courseObject["Credits"] is ContentNumberField creditsNumber ? (creditsNumber.Value ?? 0) : 0,
                                    Grade = courseObject["Grade"] is ContentStringField gradeString ? gradeString.Value : null,
                                    CalendarSystem = courseObject["CalendarSystem"] is ContentStringField calendarSystemString ? calendarSystemString.Value : null
                                };
                                transcriptCourses.Add(transcriptCourse);
                            }
                        });
                    
                }
            }

            return transcriptCourses;

        }
    }
}
