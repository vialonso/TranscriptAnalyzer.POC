using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TranscriptAnalyzer.POC.Application.Interfaces.Services;
using TranscriptAnalyzer.POC.Domain.Entities;

namespace TranscriptAnalyzer.POC.Web.Pages
{
    public class IndexModel(IBlobStorageService blobStorageService) : PageModel
    {

        public readonly IBlobStorageService _blobStorageService = blobStorageService;

        [BindProperty]
        public IFormFile? PdfFile { get; set; }

        public string? PdfUrl { get; set; }

        public List<TranscriptCourse> TranscriptCourses { get; set; } = [];

        public void OnGet()
        {
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (PdfFile != null && PdfFile.Length > 0)
            {
                // get the stream of the uploaded file
                using var stream = PdfFile.OpenReadStream();
                PdfUrl = await _blobStorageService.UploadFileAsync(stream, PdfFile.ContentType);
            }

            return Page();
        }
    }
}
