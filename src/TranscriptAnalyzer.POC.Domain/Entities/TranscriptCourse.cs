namespace TranscriptAnalyzer.POC.Domain.Entities
{
    public class TranscriptCourse
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string? Code { get; set; }
        public string? Title { get; set; }
        public double Credits { get; set; }
        public string? Grade { get; set; }
        public string? CalendarSystem { get; set; }
    }
}
