using System.Timers;

namespace Bit.BlazorUI;

internal class BitSnackbarItem : IDisposable
{
    private bool _disposed;
    private int _dismissCounter = default!;
    private BitSnackbar _parent = default!;
    private System.Timers.Timer _timer = default!;

    public BitSnackbarItem(BitSnackbar parent)
    {
        _parent = parent;

        if (parent.AutoDismiss)
        {
            _timer = new System.Timers.Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }
    }

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public BitSnackbarType Type { get; set; } = BitSnackbarType.Info;

    private async void TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _dismissCounter++;

        if (_dismissCounter == _parent.AutoDismissTime.TotalSeconds)
        {
            await _parent.Dismiss(this);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing is false || _disposed) return;

        if (_parent.AutoDismiss && _timer is not null)
        {
            _timer.Elapsed -= TimerElapsed;
            _timer.Stop();
            _timer.Close();
        }

        _disposed = true;
    }
}
