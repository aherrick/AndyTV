using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AndyTV.Watchlist.Models;

// A field code fills in from the game's ESPN event after research (WatchlistResearchService), so the model is
// never asked for it. Saved editions still store it; only the model's answer format leaves it out.
[AttributeUsage(AttributeTargets.Property)]
public sealed class FromFeedAttribute : Attribute
{
    // A copy of options whose JSON schema and parsing skip [FromFeed] properties.
    public static JsonSerializerOptions OmitFrom(JsonSerializerOptions options) =>
        new(options)
        {
            TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver()).WithAddedModifier(type =>
            {
                foreach (var property in type.Properties.Where(IsFromFeed).ToList())
                {
                    type.Properties.Remove(property);
                }
            }),
        };

    private static bool IsFromFeed(JsonPropertyInfo property) =>
        property.AttributeProvider?.IsDefined(typeof(FromFeedAttribute), inherit: false) == true;
}
