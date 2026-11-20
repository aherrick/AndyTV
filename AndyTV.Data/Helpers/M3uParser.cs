namespace AndyTV.Data.Helpers;

internal readonly record struct M3uEntry(string Title, string Url, string Group, string Logo);

// Reads only the M3U fields AndyTV uses, line by line, so large playlists parse while they download.
internal static class M3uParser
{
    public static async Task<List<M3uEntry>> ParseAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        List<M3uEntry> entries = [];
        string extInf = null;
        string extGrp = null;

        while (await reader.ReadLineAsync() is { } rawLine)
        {
            var line = rawLine.Trim();
            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                extInf = line;
                extGrp = null;
            }
            else if (line.StartsWith("#EXTGRP:", StringComparison.OrdinalIgnoreCase))
            {
                extGrp = line["#EXTGRP:".Length..].Trim();
            }
            else if (line.Length > 0 && line[0] != '#' && extInf is not null)
            {
                if (ToEntry(extInf, line, extGrp) is { } entry)
                {
                    entries.Add(entry);
                }
                extInf = null;
            }
        }

        return entries;
    }

    // #EXTINF:-1 tvg-logo="..." group-title="...",Title
    private static M3uEntry? ToEntry(string extInf, string url, string extGrp)
    {
        var comma = TitleComma(extInf);
        var title = comma < 0 ? null : extInf[(comma + 1)..].Trim();
        if (string.IsNullOrEmpty(title))
        {
            return null;
        }

        var attributes = extInf[..comma];
        var group = Attribute(attributes, "group-title");
        return new M3uEntry(
            title,
            url,
            string.IsNullOrEmpty(group) ? extGrp : group,
            Attribute(attributes, "tvg-logo"));
    }

    // Reads name="value" from the attribute part of an #EXTINF line.
    private static string Attribute(string attributes, string name)
    {
        var key = $" {name}=\"";
        var start = attributes.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        start += key.Length;
        var end = attributes.IndexOf('"', start);
        return end < 0 ? null : attributes[start..end];
    }

    // The first comma outside quotes starts the title, since attribute values can contain commas.
    private static int TitleComma(string extInf)
    {
        var quoted = false;
        for (var i = 0; i < extInf.Length; i++)
        {
            if (extInf[i] == '"')
            {
                quoted = !quoted;
            }
            else if (extInf[i] == ',' && !quoted)
            {
                return i;
            }
        }
        // An unclosed quote hides every comma; the last one is the best guess.
        return extInf.LastIndexOf(',');
    }
}