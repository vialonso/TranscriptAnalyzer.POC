using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TranscriptAnalyzer.POC.Application.Interfaces.Services;
using TranscriptAnalyzer.POC.Domain.Entities;

namespace TranscriptAnalyzer.POC.Web.Pages
{
    public class IndexModel(
        IBlobStorageService blobStorageService,
        ITranscriptAnalyzerService transcriptAnalyzerService
        ) : PageModel
    {

        private readonly IBlobStorageService _blobStorageService = blobStorageService;
        private readonly ITranscriptAnalyzerService _transcriptAnalyzerService = transcriptAnalyzerService;

        [BindProperty]
        public IFormFile? PdfFile { get; set; }

        [BindProperty]
        public string? SelectedAnalyzer { get; set; }

        [BindProperty]
        public string? PdfUrl { get; set; }

        public List<string> Analyzers { get; set; } = [];

        public List<TranscriptCourse> TranscriptCourses { get; set; } = [];

        public async Task OnGet()
        {
            Analyzers = await _transcriptAnalyzerService.GetAnalyzers();
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (PdfFile != null && PdfFile.Length > 0)
            {
                // get the stream of the uploaded file
                using var stream = PdfFile.OpenReadStream();
                PdfUrl = await _blobStorageService.UploadFileAsync(stream, PdfFile.ContentType);
            }
            Analyzers = await _transcriptAnalyzerService.GetAnalyzers();
            return Page();

        }

        public async Task<IActionResult> OnPostAnalyzeAsync()
        {
            Analyzers = await _transcriptAnalyzerService.GetAnalyzers();
            if (!string.IsNullOrEmpty(PdfUrl) && !string.IsNullOrEmpty(SelectedAnalyzer))
            {
                TranscriptCourses = await _transcriptAnalyzerService.GetTranscriptCourses(PdfUrl, SelectedAnalyzer);
            }
            return Page();
        }
    }
}
