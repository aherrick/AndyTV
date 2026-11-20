using System.Text;

namespace AndyTV.Data.Helpers;

public readonly record struct M3uEntry(string Title, string Url, string Group, string Logo);

// Minimal streaming parser for M3U/M3U8 playlists; extracts only the fields AndyTV uses.
public static class M3uParser
{
    private const string ExtInf = "#EXTINF:";
    private const string ExtGrp = "#EXTGRP:";

    public static async Task<List<M3uEntry>> ParseAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 65536, leaveOpen: true);
        return await ParseAsync(reader).ConfigureAwait(false);
    }

    public static async Task<List<M3uEntry>> ParseAsync(TextReader reader)
    {
        List<M3uEntry> entries = [];
        string title = null;
        string group = null;
        string logo = null;

        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } rawLine)
        {
            var line = rawLine.AsSpan().Trim();
            if (line.IsEmpty)
            {
                continue;
            }

            if (line.StartsWith(ExtInf, StringComparison.OrdinalIgnoreCase))
            {
                ParseExtInf(line[ExtInf.Length..], out title, out group, out logo);
            }
            else if (line.StartsWith(ExtGrp, StringComparison.OrdinalIgnoreCase))
            {
                if (title is not null && string.IsNullOrEmpty(group))
                {
                    var extGrp = line[ExtGrp.Length..].Trim();
                    if (!extGrp.IsEmpty)
                    {
                        group = extGrp.ToString();
                    }
                }
            }
            else if (line[0] != '#' && title is not null)
            {
                entries.Add(new M3uEntry(title, line.ToString(), group, logo));
                title = null;
                group = null;
                logo = null;
            }
        }

        return entries;
    }

    private static void ParseExtInf(
        ReadOnlySpan<char> header,
        out string title,
        out string group,
        out string logo)
    {
        var comma = FindTitleComma(header);
        if (comma < 0)
        {
            title = null;
            group = null;
            logo = null;
            return;
        }

        var titleSpan = header[(comma + 1)..].Trim();
        if (titleSpan.IsEmpty)
        {
            title = null;
            group = null;
            logo = null;
            return;
        }

        var attrs = header[..comma];
        title = titleSpan.ToString();
        group = ReadAttribute(attrs, "group-title");
        logo = ReadAttribute(attrs, "tvg-logo");
    }

    // First comma outside quotes splits attributes from the channel title;
    // falls back to the last comma if a provider emits an unclosed quote.
    private static int FindTitleComma(ReadOnlySpan<char> header)
    {
        var inQuotes = false;
        for (var i = 0; i < header.Length; i++)
        {
            var ch = header[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                return i;
            }
        }
        return header.LastIndexOf(',');
    }

    private static string ReadAttribute(ReadOnlySpan<char> attrs, ReadOnlySpan<char> name)
    {
        var pos = 0;
        while (pos < attrs.Length)
        {
            var idx = attrs[pos..].IndexOf(name, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                return null;
            }

            idx += pos;
            var afterName = idx + name.Length;
            if ((idx == 0 || char.IsWhiteSpace(attrs[idx - 1]))
                && afterName < attrs.Length
                && attrs[afterName] == '=')
            {
                var rest = attrs[(afterName + 1)..];
                if (rest.Length >= 2 && rest[0] is '"' or '\'')
                {
                    var end = rest[1..].IndexOf(rest[0]);
                    return end > 0 ? rest.Slice(1, end).ToString() : null;
                }
            }

            pos = afterName;
        }
        return null;
    }
}
