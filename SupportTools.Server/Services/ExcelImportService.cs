using ClosedXML.Excel;
using SupportTools.Shared.Models;
using SupportTools.Server.Data;

namespace SupportTools.Server.Services;

public class ExcelImportService {
    private readonly SupportToolsDbContext _dbContext;

    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(SupportToolsDbContext dbContext, ILogger<ExcelImportService> logger) {
        _dbContext = dbContext;
        _logger = logger;
    }


    public void LoadApotheken(string filePath) {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet("Apotheken");

        foreach (var row in sheet.RowsUsed().Skip(1)) // Skip header
        {
            var apotheek = new Apotheek {
                MosadexId = row.Cell(1).GetString(),
                Naam = row.Cell(2).GetString(),
                AGB = row.Cell(3).GetString()
            };

            _dbContext.Apotheken.Add(apotheek);
        }
    }

    public void LoadATCCodes(string filePath) {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet("ATCCodes");

        foreach (var row in sheet.RowsUsed().Skip(1)) {
            var code = new ATCCode {
                Code = row.Cell(1).GetString(),
                Naam = row.Cell(2).GetString()
            };

            _dbContext.ATCCodes.Add(code);
        }
    }

    public void LoadApotheekATCs(string filePath) {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet("ApotheekATC");

        int toegevoegd = 0;
        int overgeslagen = 0;

        foreach (var row in sheet.RowsUsed().Skip(1)) {
            var agb = row.Cell(1).GetString().Trim();
            var code = row.Cell(2).GetString().Trim();

            var apotheek = _dbContext.Apotheken.FirstOrDefault(a => a.AGB == agb);
            var atcCode = _dbContext.ATCCodes.FirstOrDefault(c => c.Code == code);

            if (apotheek != null && atcCode != null) {
                var bestaatAl = _dbContext.ApotheekATCs.Any(k =>
                    k.ApotheekId == apotheek.Id && k.ATCCodeId == atcCode.Id);

                if (!bestaatAl) {
                    _dbContext.ApotheekATCs.Add(new ApotheekATC {
                        ApotheekId = apotheek.Id,
                        ATCCodeId = atcCode.Id
                    });
                    toegevoegd++;
                }
            }
            else {
                overgeslagen++;
                Console.WriteLine($"Overgeslagen koppeling: AGB={agb}, Code={code} → Apotheek gevonden: {apotheek != null}, ATC gevonden: {atcCode != null}");
                _logger.LogWarning("❌ Koppeling overgeslagen: AGB={AGB}, Code={Code}", agb, code);

            }
        }

        Console.WriteLine($"✅ ApotheekATC: {toegevoegd} koppelingen toegevoegd, {overgeslagen} overgeslagen.");
    }


    public async Task Import(string filePath) {
        LoadApotheken(filePath);
        LoadATCCodes(filePath);
        LoadApotheekATCs(filePath);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("✅ Bestand '{File}' succesvol geïmporteerd op {Time}", filePath, DateTime.Now);
    }
}
