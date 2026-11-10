namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>Base of the Theme page's token sections: the values the browser resolved for the tokens, which the page reads.</summary>
public abstract class TokenSectionBase : WorkbenchComponentBase
{
    /// <summary>Each token's value in the view, by its name; empty until the page has read them.</summary>
    [Parameter] public IReadOnlyDictionary<string, string> Values { get; set; } = new Dictionary<string, string>();

    protected string? ValueOf(ThemeToken token) => Values.GetValueOrDefault(token.Name);

    /// <summary>The token's name without the --bit- prefix the names share.</summary>
    protected static string ShortName(ThemeToken token) => token.Name["--bit-".Length..];
}
