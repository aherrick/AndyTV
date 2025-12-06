namespace AndyTV.Maui.Controls;

public class NativeVideoPlayer : View
{
    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(nameof(Source), typeof(string), typeof(NativeVideoPlayer), string.Empty);

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public void Play() => Handler?.Invoke(nameof(Play));
    public void Stop() => Handler?.Invoke(nameof(Stop));
}
