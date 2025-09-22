namespace AndyTV.Data.Models;

public class ChannelTop
{
    public string Name { get; set; }
    public List<string> AltNames { get; set; }
    public string StreamingTvId { get; set; }

    public IEnumerable<string> Terms
    {
        get
        {
            var alts = AltNames;
            if (alts != null)
            {
                foreach (var a in alts)
                {
                    yield return a;
                }
            }

            yield return Name; // Name is always present
        }
    }
}