using SupportTools.Shared.Models;

public class ATCCode {
    public int Id {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string Naam { get; set; } = string.Empty;

    public ICollection<ApotheekATC> ApotheekATCs { get; set; } = new List<ApotheekATC>();
}
