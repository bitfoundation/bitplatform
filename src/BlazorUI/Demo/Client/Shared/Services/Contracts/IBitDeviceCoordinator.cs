namespace Bit.BlazorUI.Demo.Client.Shared.Services.Contracts;
public interface IBitDeviceCoordinator
{
    public double GetStatusBarHeight();

    public void SetDeviceTheme(bool isDark);
}
