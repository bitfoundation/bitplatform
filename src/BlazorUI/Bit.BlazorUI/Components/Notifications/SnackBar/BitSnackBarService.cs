using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// Shows snack bar notifications from anywhere in the app, through the <see cref="BitSnackBar"/> marked as the
/// <see cref="BitSnackBar.ServiceHost"/>.
/// </summary>
/// <remarks>
/// A notification is usually raised by code far away from the layout that holds the snack bar - a page, a form, a
/// service handling an error. Injecting this service is how that code reaches the host without the host's
/// reference being passed down to it. It is registered by <c>AddBitBlazorUIServices</c>.
/// <br />
/// Place one <c>&lt;BitSnackBar ServiceHost /&gt;</c> in the layout. While several hosts are marked, the one
/// rendered most recently takes the notifications, and when it goes away the one before it takes over. A
/// notification shown while no host is rendered is not shown at all; it is reported through the logger factory,
/// where one is registered, and <see cref="IsHostAvailable"/> tells the two apart beforehand.
/// </remarks>
public class BitSnackBarService
{
    private readonly ILogger? _logger;
    private readonly List<BitSnackBar> _hosts = [];
    private readonly object _hostsLock = new();

    // An app without a host shows every notification into nothing, and one clear line is the message - a line per
    // notification is noise.
    private bool _missingHostLogged;



    public BitSnackBarService() : this(null)
    {
    }

    public BitSnackBarService(ILoggerFactory? loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger<BitSnackBarService>();
    }



    /// <summary>
    /// Whether a <see cref="BitSnackBar"/> marked as the <see cref="BitSnackBar.ServiceHost"/> is rendered, so the
    /// notifications shown through this service reach the screen.
    /// </summary>
    public bool IsHostAvailable => Host is not null;

    /// <summary>
    /// The snack bar the notifications of this service are shown through, if one is rendered.
    /// </summary>
    /// <remarks>
    /// This is the host itself, so everything it offers beyond the members of this service - <c>Items</c>,
    /// <c>Pause</c>, <c>Resume</c>, <c>FocusAsync</c> - is reachable through it.
    /// </remarks>
    public BitSnackBar? Host
    {
        get
        {
            lock (_hostsLock)
            {
                return _hosts.Count > 0 ? _hosts[^1] : null;
            }
        }
    }



    /// <summary>
    /// Shows a new snackbar with Info color.
    /// </summary>
    public Task<BitSnackBarItem> Info(string title, string? body = "", bool persistent = false, TimeSpan? autoDismissTime = null) => Show(title, body, BitColor.Info, persistent: persistent, autoDismissTime: autoDismissTime);

    /// <summary>
    /// Shows a new snackbar with Success color.
    /// </summary>
    public Task<BitSnackBarItem> Success(string title, string? body = "", bool persistent = false, TimeSpan? autoDismissTime = null) => Show(title, body, BitColor.Success, persistent: persistent, autoDismissTime: autoDismissTime);

    /// <summary>
    /// Shows a new snackbar with Warning color.
    /// </summary>
    public Task<BitSnackBarItem> Warning(string title, string? body = "", bool persistent = false, TimeSpan? autoDismissTime = null) => Show(title, body, BitColor.Warning, persistent: persistent, autoDismissTime: autoDismissTime);

    /// <summary>
    /// Shows a new snackbar with SevereWarning color.
    /// </summary>
    public Task<BitSnackBarItem> SevereWarning(string title, string? body = "", bool persistent = false, TimeSpan? autoDismissTime = null) => Show(title, body, BitColor.SevereWarning, persistent: persistent, autoDismissTime: autoDismissTime);

    /// <summary>
    /// Shows a new snackbar with Error color.
    /// </summary>
    public Task<BitSnackBarItem> Error(string title, string? body = "", bool persistent = false, TimeSpan? autoDismissTime = null) => Show(title, body, BitColor.Error, persistent: persistent, autoDismissTime: autoDismissTime);

    /// <summary>
    /// Shows a new snackbar.
    /// </summary>
    public Task<BitSnackBarItem> Show(
        string title,
        string? body = "",
        BitColor color = BitColor.Info,
        string? cssClass = null,
        string? cssStyle = null,
        bool persistent = false,
        TimeSpan? autoDismissTime = null)
    {
        return Show(new BitSnackBarItem
        {
            Title = title,
            Body = body,
            Color = color,
            CssClass = cssClass,
            CssStyle = cssStyle,
            Persistent = persistent,
            AutoDismissTime = autoDismissTime
        });
    }

    /// <summary>
    /// Shows a new snackbar.
    /// </summary>
    /// <remarks>
    /// The item comes back as it would from <see cref="BitSnackBar.Show(BitSnackBarItem)"/> of the host, and is the
    /// handle <see cref="Close"/> and <see cref="Update"/> take. With no host rendered it comes back unshown.
    /// </remarks>
    public Task<BitSnackBarItem> Show(BitSnackBarItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var host = Host;

        if (host is null)
        {
            ReportMissingHost();

            return Task.FromResult(item);
        }

        return host.Show(item);
    }

    /// <summary>
    /// Closes a snackbar item shown through this service.
    /// </summary>
    /// <remarks>
    /// The item is closed by whichever host is showing it (or holding it in its queue), which is not necessarily
    /// the current one: a host that took over later does not own what an earlier one showed.
    /// </remarks>
    public Task Close(BitSnackBarItem item) => FindOwner(item)?.Close(item) ?? Task.CompletedTask;

    /// <summary>
    /// Re-renders a snackbar item after its properties were changed, restarts its auto-dismiss countdown and
    /// announces its new text again.
    /// </summary>
    public Task Update(BitSnackBarItem item) => FindOwner(item)?.Update(item) ?? Task.CompletedTask;

    /// <summary>
    /// Closes every snackbar item of the current host, and drops everything that was waiting in its queue.
    /// </summary>
    public Task Clear() => Host?.Clear() ?? Task.CompletedTask;



    internal void Register(BitSnackBar host)
    {
        lock (_hostsLock)
        {
            _hosts.Remove(host);
            _hosts.Add(host);

            _missingHostLogged = false;
        }
    }

    internal void Unregister(BitSnackBar host)
    {
        lock (_hostsLock)
        {
            _hosts.Remove(host);
        }
    }

    private BitSnackBar? FindOwner(BitSnackBarItem item)
    {
        if (item is null) return null;

        lock (_hostsLock)
        {
            for (var i = _hosts.Count - 1; i >= 0; i--)
            {
                if (_hosts[i].Owns(item)) return _hosts[i];
            }
        }

        return null;
    }

    private void ReportMissingHost()
    {
        if (_missingHostLogged) return;

        _missingHostLogged = true;

        _logger?.LogWarning("A snack bar was shown through BitSnackBarService while no <BitSnackBar ServiceHost /> is rendered, so it did not reach the screen. Place one in the layout.");
    }
}
