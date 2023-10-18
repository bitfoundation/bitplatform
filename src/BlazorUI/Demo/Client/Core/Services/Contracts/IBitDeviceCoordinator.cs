namespace Bit.BlazorUI.Demo.Client.Core.Services.Contracts;
public interface IBitDeviceCoordinator
{
    public double GetStatusBarHeight() { return 0; }

    public async Task ApplyTheme(bool isDark) { }
}
