#if IOS
using CoreGraphics;
using Foundation;
using UIKit;

namespace AndyTV.Maui.Services;

// Remote only supports Left Arrow -> VolumeDown, Right Arrow -> VolumeUp.
public sealed class RemoteCommandService : IRemoteCommandService
{
    private HardwareInputView _inputView;
    private bool _started;

    public event EventHandler<RemoteCommandEventArgs> CommandReceived;

    public void Start()
    {
        if (_started)
        {
            return;
        }
        _started = true;

        MainThread.BeginInvokeOnMainThread(AttachToRootView);
    }

    public void Stop()
    {
        if (!_started)
        {
            return;
        }
        _started = false;
        MainThread.BeginInvokeOnMainThread(DetachFromRootView);
    }

    private void AttachToRootView()
    {
        var rootView = GetRootView();
        if (rootView is null)
        {
            return;
        }

        _inputView = new HardwareInputView(HandlePress)
        {
            Frame = CGRect.Empty,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            BackgroundColor = UIColor.Clear,
            UserInteractionEnabled = true,
        };
        rootView.AddSubview(_inputView);
        _inputView.BecomeFirstResponder();
    }

    private void DetachFromRootView()
    {
        if (_inputView is not null)
        {
            _inputView.ResignFirstResponder();
            _inputView.RemoveFromSuperview();
            _inputView.Dispose();
            _inputView = null;
        }
    }

    private bool HandlePress(UIPress press)
    {
        switch (press.Type)
        {
            case UIPressType.LeftArrow:
                Publish(RemoteCommandKind.VolumeDown);
                return true;
            case UIPressType.RightArrow:
                Publish(RemoteCommandKind.VolumeUp);
                return true;
            default:
                return false;
        }
    }

    private void Publish(RemoteCommandKind kind)
    {
        CommandReceived?.Invoke(this, new RemoteCommandEventArgs(kind));
    }

    private static UIView GetRootView()
    {
        foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is UIWindowScene ws)
            {
                foreach (var window in ws.Windows)
                {
                    if (window.IsKeyWindow)
                    {
                        return window.RootViewController?.View;
                    }
                }
            }
        }
        return null;
    }

    private sealed class HardwareInputView(Func<UIPress, bool> handlePress) : UIView(CGRect.Empty)
    {
        public override bool CanBecomeFirstResponder => true;

        public override void MovedToWindow()
        {
            base.MovedToWindow();
            if (Window is not null)
            {
                BecomeFirstResponder();
            }
        }

        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            var handled = false;
            foreach (var p in presses)
            {
                if (handlePress(p))
                {
                    handled = true;
                }
            }
            if (!handled)
            {
                base.PressesBegan(presses, evt);
            }
        }
    }
}
#endif
