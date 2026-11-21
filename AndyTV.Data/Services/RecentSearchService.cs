namespace AndyTV.Data.Services;

public class RecentSearchService(IStorageProvider storage)
{
    private const int MaxRecent = 5;
    private const string RecentSearchesFile = "recent-searches.json";

    // Newest first.
    public List<string> Get() =>
        [.. (storage.ReadJson<List<string>>(RecentSearchesFile) ?? []).Take(MaxRecent)];

    public void Add(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return;
        }

        var list = Get();
        list.RemoveAll(x => string.Equals(x, term, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, term);
        storage.WriteJson(RecentSearchesFile, list.Take(MaxRecent));
    }

    public void Clear() => storage.WriteJson<string[]>(RecentSearchesFile, []);
}
