public class ApotheekATC
{
    public int Id { get; set; }

    public int ApotheekId { get; set; }
    public Apotheek Apotheek { get; set; } = null!;

    public int ATCCodeId { get; set; }

    public ATCCode ATCCode { get; set; } = null!;
}
