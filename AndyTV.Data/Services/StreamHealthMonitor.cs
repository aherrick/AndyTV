namespace AndyTV.Data.Services;

// Played audio buffers only advance on real playback; VLC's clock events and video counters keep
// moving on a frozen picture. Displayed pictures are the fallback for video-only streams.
public sealed class StreamHealthMonitor(int stallSeconds = 5, int startupSeconds = 15)
{
    private int _progress;
    private DateTime _progressAt = DateTime.UtcNow;

    // Call when (re)starting a stream.
    public void Reset()
    {
        _progress = 0;
        _progressAt = DateTime.UtcNow;
    }

    // Call about once a second with VLC's media statistics; true means restart the stream.
    public bool IsStalled(int playedAudioBuffers, int displayedPictures)
    {
        var progress = playedAudioBuffers > 0 ? playedAudioBuffers : displayedPictures;
        var now = DateTime.UtcNow;
        if (progress != _progress)
        {
            _progress = progress;
            _progressAt = now;
            return false;
        }
        var limit = progress == 0 ? startupSeconds : stallSeconds;
        return (now - _progressAt).TotalSeconds >= limit;
    }
}