namespace AndyTV.Maui.Services;

public interface IOrientationLockService
{
    LockMode CurrentLockMode { get; }

    void CycleLockMode();

    void ApplyForPlayback();

    void UseDefaultOrientation();
}
