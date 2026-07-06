using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Handle to a live notification. Dispose to detach event listeners and close the toast.
/// </summary>
public sealed class NotificationHandle : IAsyncDisposable
{
    private readonly Notification _owner;
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _disposed;

    internal NotificationHandle(Notification owner, IJSRuntime js, Guid id) { _owner = owner; _js = js; _id = id; }

    /// <summary>The internal notification id.</summary>
    public Guid Id => _id;

    /// <summary>Closes the notification programmatically.</summary>
    public ValueTask Close() => _js.InvokeVoid("BitButil.notification.close", _id);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _owner.RemoveListener(_id);
        try { await _js.InvokeVoid("BitButil.notification.dispose", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
