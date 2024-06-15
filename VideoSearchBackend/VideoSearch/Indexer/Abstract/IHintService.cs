namespace VideoSearch.Indexer.Abstract;

public interface IHintService
{
    public void NotifyIndexUpdated();
    public Task Rebuild();
    public List<string> GetHintsFor(string query);
}