namespace Boilerplate.Client.Core.Services.Contracts;

public interface IThemeService
{
    Task<AppThemeType> GetCurrentTheme();

    Task<AppThemeType> ToggleTheme();
}
