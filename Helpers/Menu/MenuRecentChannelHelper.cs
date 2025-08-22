using System.Text.Json;
using AndyTV.Models;

namespace AndyTV.Helpers.Menu;

public class MenuRecentChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
{
    private readonly SynchronizationContext _ui =
        SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

    private readonly int MaxRecent = 5;
    private readonly ToolStripMenuItem _header = MenuHelper.AddHeader(menu, "RECENT");

    public static readonly string FileName = PathHelper.GetPath("recents.json");

    public void AddOrPromote(Channel ch)
    {
        if (ch == null || string.IsNullOrWhiteSpace(ch.Url))
            return;

        var list = LoadListFromDisk();
        list.RemoveAll(x => string.Equals(x?.Url, ch.Url, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, ch);

        if (list.Count > MaxRecent)
        {
            list = [.. list.Take(MaxRecent)];
        }

        SaveListToDisk(list);

        RebuildRecentMenu();
    }

    public void RebuildRecentMenu()
    {
        var recents = LoadListFromDisk().Take(MaxRecent).ToList();

        _ui.Post(
            _ =>
            {
                int headerIndex = menu.Items.IndexOf(_header);
                int insertIndex = headerIndex + 2; // header + separator

                // Clear existing recents until next separator
                while (
                    insertIndex < menu.Items.Count
                    && menu.Items[insertIndex] is not ToolStripSeparator
                )
                {
                    menu.Items.RemoveAt(insertIndex);
                }

                // Insert current recents
                foreach (var ch in recents)
                {
                    var item = new ToolStripMenuItem(ch.Name) { Tag = ch.Url };
                    item.Click += clickHandler;
                    menu.Items.Insert(insertIndex++, item);
                }
            },
            null
        );
    }

    public Channel GetPrevious()
    {
        var list = LoadListFromDisk().Take(MaxRecent).ToList();
        return list.ElementAtOrDefault(1);
    }

    private static List<Channel> LoadListFromDisk()
    {
        if (!File.Exists(FileName))
        {
            File.WriteAllText(FileName, "[]");
            return [];
        }

        var json = File.ReadAllText(FileName);
        return JsonSerializer.Deserialize<List<Channel>>(json) ?? [];
    }

    private void SaveListToDisk(List<Channel> list)
    {
        var trimmed = list.Take(MaxRecent).ToList();
        var json = JsonSerializer.Serialize(trimmed);
        File.WriteAllText(FileName, json);
    }
}