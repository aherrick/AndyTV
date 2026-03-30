namespace AndyTV.Maui.Controls;

public class NativeVideoPlayer : View
{
    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(nameof(Source), typeof(string), typeof(NativeVideoPlayer));

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public bool IsPaused { get; private set; } = true;

    public event Action PlaybackActivity;

    public void OnPlaybackActivity() => PlaybackActivity?.Invoke();

    public void SetPaused(bool paused) => IsPaused = paused;

    public void Play() => Handler?.Invoke(nameof(Play));

    public void Stop() => Handler?.Invoke(nameof(Stop));
}
