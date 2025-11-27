using SupportTools.Shared.Models;

public class Apotheek {
    public int Id {
        get; set;
    }
    public string AGB { get; set; } = string.Empty;
    public string Naam { get; set; } = string.Empty;
    public string MosadexId { get; set; } = string.Empty;

    public ICollection<ApotheekATC> ApotheekATCs { get; set; } = new List<ApotheekATC>();
}
