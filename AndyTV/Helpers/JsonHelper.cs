using System.Text.Json;

namespace AndyTV.Helpers;

public static class JsonHelper
{
    /// <summary>
    /// Generates a JSON snapshot of an object for change detection.
    /// </summary>
    public static string GenerateSnapshot<T>(T value) => JsonSerializer.Serialize(value);
}
