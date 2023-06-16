namespace Bit.BlazorUI.Demo.Client.Shared.Services.Contracts;
public interface IBitDeviceCoordinator
{
    public double GetStatusBarHeight() { return 0; }

    public async Task SetDeviceTheme(bool isDark) { }
}
