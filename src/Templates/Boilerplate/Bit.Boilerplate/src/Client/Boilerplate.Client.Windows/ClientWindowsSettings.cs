//+:cnd:noEmit
using Boilerplate.Client.Core;
using Microsoft.Extensions.Options;
using Boilerplate.Client.Windows.Infrastructure.Services;

namespace Boilerplate.Client.Windows;

public class ClientWindowsSettings : ClientCoreSettings
{
    public WindowsUpdateOptions? WindowsUpdate { get; set; }

    public KioskOptions? Kiosk { get; set; }
}

[OptionsValidator]
public partial class ClientWindowsSettingsValidator : IValidateOptions<ClientWindowsSettings>
{
}

public partial class WindowsUpdateOptions
{
    public bool AutoReload { get; set; }

    public string? FilesUrl { get; set; }
}

/// <summary>
/// Options of the .NET 11 <see cref="System.Windows.Forms.KioskModeManager"/> and of <see cref="WindowsKioskGuard"/>.
/// </summary>
public partial class KioskOptions
{
    /// <summary>
    /// Starts the app full screen, above the taskbar, and locks it there: neither F11 nor Escape leaves it, the
    /// window cannot be closed, the key combinations that reach the desktop are swallowed and the browser's own
    /// menus, dev tools, zoom and shortcuts are off. When off, F11 still toggles full screen and Escape leaves it.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Keeps the display and the machine awake for as long as the app runs.
    /// </summary>
    public bool PreventSleep { get; set; }
}
