namespace VideoSearch.Database.Models;

public class NgramModel
{
    public string Ngram { get; set; }
    public double Idf { get; set; }
    public double IdfBm { get; set; }
    public int TotalDocs { get; set; }
    public double TotalNgrams { get; set; }
}