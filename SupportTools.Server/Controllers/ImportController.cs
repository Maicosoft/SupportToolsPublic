using Microsoft.AspNetCore.Mvc;
using SupportTools.Server.Services;

namespace SupportTools.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImportController : ControllerBase {
    private readonly ExcelImportService _importer;

    public ImportController(ExcelImportService importer) {
        _importer = importer;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadExcel(IFormFile file) {
        if (file == null || file.Length == 0)
            return BadRequest("Geen bestand ontvangen.");

        var filePath = Path.Combine("Uploads", file.FileName);

        try {
            Directory.CreateDirectory("Uploads");

            using (var stream = new FileStream(filePath, FileMode.Create)) {
                await file.CopyToAsync(stream);
            }

            await _importer.Import(filePath);
            Console.WriteLine($"✅ Bestand '{filePath}' succesvol geïmporteerd op {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return Ok("Excel-bestand geïmporteerd!");
        }
        catch (Exception ex) {
            return StatusCode(500, $"Fout bij import: {ex.Message}");
        }
    }
}
