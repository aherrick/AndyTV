namespace AndyTV.Maui.Services;

public interface IOrientationLockService
{
    bool IsLandscapeLockEnabled { get; }

    void SetLandscapeLockEnabled(bool isEnabled);

    void ApplyForPlayback();

    void UseDefaultOrientation();
}
