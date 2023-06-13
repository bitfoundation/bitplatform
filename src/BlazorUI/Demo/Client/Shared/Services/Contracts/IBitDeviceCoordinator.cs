namespace Bit.BlazorUI.Demo.Client.Shared.Services.Contracts;
public interface IBitDeviceCoordinator
{
    public double GetStatusBarHeight();

    public void SetUserAppTheme(bool isDark);
}
