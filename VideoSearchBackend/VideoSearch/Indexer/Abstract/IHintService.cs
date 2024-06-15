namespace VideoSearch.Indexer.Abstract;

public interface IHintService
{
    public void AddToIndex(IEnumerable<string> keywords, bool disableRebuild = false);
    public Task WarmUp();
    public List<string> GetHintsFor(string query);
}