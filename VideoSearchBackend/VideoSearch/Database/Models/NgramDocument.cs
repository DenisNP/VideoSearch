namespace VideoSearch.Database.Models;

public class NgramDocument
{
    public string Ngram { get; set; }
    public Guid DocumentId { get; set; }
    public double CountInDoc { get; set; }
    public double Tf { get; set; }
    public double TfBm { get; set; }
    public double Score { get; set; }
    public double ScoreBm { get; set; }
}