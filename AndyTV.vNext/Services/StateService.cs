using System.Text.Json;

namespace AndyTV.vNext;

static class StateService
{
    private static readonly string StatePath = Path.Combine(AppContext.BaseDirectory, "iptv.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppState Load()
    {
        try
        {
            if (File.Exists(StatePath))
            {
                return JsonSerializer.Deserialize<AppState>(File.ReadAllText(StatePath)) ?? new();
            }
        }
        catch { }
        return new();
    }

    public static void Save(AppState state)
    {
        try { File.WriteAllText(StatePath, JsonSerializer.Serialize(state, Options)); }
        catch { }
    }
}
