namespace AndyTV.vNext;

sealed class PlaylistRef
{
    public string Name { get; set; } = "";
    public string Source { get; set; } = "";
    public bool Hidden { get; set; }
    public bool Grouped { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool Visible
    {
        get => !Hidden;
        set => Hidden = !value;
    }
}
