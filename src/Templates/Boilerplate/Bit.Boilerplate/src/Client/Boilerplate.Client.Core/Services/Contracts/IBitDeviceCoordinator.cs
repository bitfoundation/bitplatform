namespace Boilerplate.Client.Core.Services.Contracts;

/// <summary>
/// This service performs device-specific tasks, such as setting the theme.
/// </summary>
public interface IBitDeviceCoordinator
{
    public async Task ApplyTheme(bool isDark) { }
}
