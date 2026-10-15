namespace AndyTV.Data.Models;

public class ChannelTop
{
    public string Name { get; set; } = string.Empty;
    public List<string> AltNames { get; set; }
    public List<string> ExcludeTerms { get; set; }
    public string StreamingTVId { get; set; }

    // A ChannelFamily constant; null groups by Name.
    public string Family { get; set; }

    public IEnumerable<string> Terms
    {
        get
        {
            foreach (var a in AltNames ?? [])
            {
                yield return a;
            }

            yield return Name; // Name is always present
        }
    }
}