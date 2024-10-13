namespace Boilerplate.Client.Core.Components;

public class Parameters
{
    public const string CurrentDir = nameof(CurrentDir);
    public const string CurrentUrl = nameof(CurrentUrl);
    public const string CurrentTheme = nameof(CurrentTheme);
    public const string IsAuthenticated = nameof(IsAuthenticated);

    /// <summary>
    /// If the current page is part of the cross-layout pages that are rendered in multiple layouts.
    /// </summary>
    public const string IsCrossLayoutPage = nameof(IsCrossLayoutPage);
}
