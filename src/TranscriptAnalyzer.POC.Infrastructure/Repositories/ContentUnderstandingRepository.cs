using Azure;
using Azure.AI.ContentUnderstanding;
using Microsoft.Extensions.Logging;
using TranscriptAnalyzer.POC.Application.Interfaces.Repositories;
using TranscriptAnalyzer.POC.Domain.Entities;
using System.Diagnostics;

namespace TranscriptAnalyzer.POC.Infrastructure.Repositories
{
    public class ContentUnderstandingRepository(
        ILogger<ContentUnderstandingRepository> logger,
        ContentUnderstandingClient contentUnderstandingClient
        ) : IContentUnderstandingRepository
    {
        private readonly ILogger<ContentUnderstandingRepository> _logger = logger;
        private readonly ContentUnderstandingClient _client = contentUnderstandingClient;

        public async Task<List<string>> GetAnalyzers()
        {
            _logger.LogInformation("Listing analyzers from Azure Content Understanding...");
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                List<string> result = [];
                var analyzers = _client.GetAnalyzersAsync();
                var contentAnalyzers = await analyzers.ToListAsync();
                foreach (var item in contentAnalyzers.Where(e => e.AnalyzerId.Contains("transcript")).ToList().OrderByDescending(e => e.CreatedAt))
                    result.Add(item.AnalyzerId);
                sw.Stop();
                _logger.LogInformation("Found {AnalyzerCount} analyzers in {ElapsedMilliseconds} ms.", result.Count, sw.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Failed to list analyzers after {ElapsedMilliseconds} ms.", sw.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<List<TranscriptCourse>> TryGetTranscriptCoursesFromDocument(string fileUrl, string analyzerId)
        {
            _logger.LogInformation("Analyzing transcript document. AnalyzerId: {AnalyzerId}, FileUrl: {FileUrl}", analyzerId, fileUrl);
            Stopwatch sw = Stopwatch.StartNew();
            try
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
                sw.Stop();
                _logger.LogInformation("Extracted {CourseCount} courses from transcript in {ElapsedMilliseconds} ms.", transcriptCourses.Count, sw.ElapsedMilliseconds);
                return transcriptCourses;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Failed to analyze transcript after {ElapsedMilliseconds} ms. AnalyzerId: {AnalyzerId}, FileUrl: {FileUrl}", sw.ElapsedMilliseconds, analyzerId, fileUrl);
                throw;
            }
        }
    }
}
