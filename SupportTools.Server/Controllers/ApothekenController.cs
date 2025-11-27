using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportTools.Server.Data; // of waar je ApplicationDbContext staat
using SupportTools.Shared.Models;

namespace SupportTools.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApothekenController : ControllerBase
    {
        private readonly SupportToolsDbContext _dbContext;

        public ApothekenController(SupportToolsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchApotheken(string query)
        {
            Console.WriteLine($"[API] Ontvangen zoekterm: {query}");

            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query mag niet leeg zijn.");

            var results = await _dbContext.Apotheken
                .Where(a =>
                    a.AGB.Contains(query) ||
                    a.Naam.Contains(query) ||
                    a.MosadexId.Contains(query))
                .Select(a => new ApotheekDto
                {
                    Id = a.Id,
                    AGB = a.AGB,
                    Naam = a.Naam,
                    MosadexId = a.MosadexId
                })
                .ToListAsync();

            Console.WriteLine($"[API] Aantal resultaten: {results.Count}");

            return Ok(results);
        }

        [HttpGet("{id}/atccodes")]
        public async Task<IActionResult> GetAtcCodes(int id)
        {
            var exists = await _dbContext.Apotheken.AnyAsync(a => a.Id == id);
            if (!exists) return NotFound();

            var codes = await _dbContext.ApotheekATCs
                .Where(x => x.ApotheekId == id)
                .Include(x => x.ATCCode)
                .Select(x => new AtcCodeDto
                {
                    Code = x.ATCCode.Code,
                    Naam = x.ATCCode.Naam
                })
                .AsNoTracking()
                .ToListAsync();

            return Ok(codes);
        }

        public class AddAtcRequest
        {
            public string Code { get; set; } = string.Empty; // ATC Code string
        }

        public class HierarchyConflictResponse
        {
            public string Type { get; set; } = string.Empty; // parentExists | childrenReplaceRequired
            public string Message { get; set; } = string.Empty;
            public List<string> ChildrenToRemove { get; set; } = new();
        }

        [HttpPost("{id}/atccodes")]
        public async Task<IActionResult> AddAtcCode(int id, [FromBody] AddAtcRequest request, [FromQuery] bool force = false)
        {
            if (string.IsNullOrWhiteSpace(request.Code)) return BadRequest("Code is verplicht.");
            var code = request.Code.Trim();

            var apotheek = await _dbContext.Apotheken.FindAsync(id);
            if (apotheek == null) return NotFound("Apotheek niet gevonden.");

            var atc = await _dbContext.ATCCodes.FirstOrDefaultAsync(c => c.Code == code);
            if (atc == null) return NotFound("ATC-code bestaat niet.");

            // All existing codes (strings) for this apotheek
            var existing = await _dbContext.ApotheekATCs
                .Where(x => x.ApotheekId == id)
                .Include(x => x.ATCCode)
                .Select(x => new { x.Id, x.ATCCode.Code })
                .ToListAsync();

            // Duplicate exact
            if (existing.Any(e => e.Code == code))
                return Conflict("Deze ATC-code is al gekoppeld.");

            // If any existing code is a prefix of the new code: parent exists, cannot add child
            var parent = existing.FirstOrDefault(e => code.StartsWith(e.Code, StringComparison.OrdinalIgnoreCase) && e.Code.Length < code.Length);
            if (parent != null)
            {
                return Conflict(new HierarchyConflictResponse
                {
                    Type = "parentExists",
                    Message = $"Kan '{code}' niet toevoegen omdat parent '{parent.Code}' al gekoppeld is.",
                    ChildrenToRemove = new List<string>()
                });
            }

            // Collect children that would need to be removed if adding a parent code
            var children = existing
                .Where(e => e.Code.StartsWith(code, StringComparison.OrdinalIgnoreCase) && e.Code.Length > code.Length)
                .ToList();

            if (children.Any() && !force)
            {
                return Conflict(new HierarchyConflictResponse
                {
                    Type = "childrenReplaceRequired",
                    Message = $"Toevoegen van parent '{code}' zal {children.Count} child codes verwijderen.",
                    ChildrenToRemove = children.Select(c => c.Code).ToList()
                });
            }

            // If force and children exist, remove them first
            if (children.Any() && force)
            {
                var childIds = children.Select(c => c.Code).ToHashSet();
                var toRemoveLinks = await _dbContext.ApotheekATCs
                    .Where(x => x.ApotheekId == id && childIds.Any(codePrefix => x.ATCCode.Code == codePrefix))
                    .ToListAsync();
                _dbContext.ApotheekATCs.RemoveRange(toRemoveLinks);
            }

            // Add new link
            var link = new ApotheekATC { ApotheekId = id, ATCCodeId = atc.Id };
            _dbContext.ApotheekATCs.Add(link);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAtcCodes), new { id }, new AtcCodeDto { Code = atc.Code, Naam = atc.Naam });
        }

        [HttpDelete("{id}/atccodes/{code}")]
        public async Task<IActionResult> DeleteAtcCode(int id, string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return BadRequest();

            var atc = await _dbContext.ATCCodes.FirstOrDefaultAsync(c => c.Code == code);
            if (atc == null) return NotFound("ATC-code niet gevonden.");

            var link = await _dbContext.ApotheekATCs.FirstOrDefaultAsync(x => x.ApotheekId == id && x.ATCCodeId == atc.Id);
            if (link == null) return NotFound("Koppeling niet gevonden.");

            _dbContext.ApotheekATCs.Remove(link);
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("atccodes/all")]
        public async Task<IActionResult> GetAllAtcCodes()
        {
            var all = await _dbContext.ATCCodes
                .OrderBy(c => c.Code)
                .Select(c => new AtcCodeDto { Code = c.Code, Naam = c.Naam })
                .AsNoTracking()
                .ToListAsync();
            return Ok(all);
        }

        [HttpGet("atccodes/suggest")]
        public async Task<IActionResult> SuggestAtcCodes([FromQuery] string term, [FromQuery] int take = 20)
        {
            if (string.IsNullOrWhiteSpace(term)) return Ok(new List<AtcCodeDto>());
            term = term.Trim();
            take = Math.Clamp(take, 1, 50);

            var results = await _dbContext.ATCCodes
                .Where(c => EF.Functions.Like(c.Code, term + "%") || EF.Functions.Like(c.Naam, term + "%"))
                .OrderBy(c => c.Code)
                .Select(c => new AtcCodeDto { Code = c.Code, Naam = c.Naam })
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
            return Ok(results);
        }
    }
}
