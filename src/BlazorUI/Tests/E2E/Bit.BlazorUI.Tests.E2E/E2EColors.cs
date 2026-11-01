namespace Bit.BlazorUI.Tests.E2E;

/// <summary>
/// What every category of the E2E tests reads its expected colors with: the rules of color-role-maps.scss, written once
/// so that every test follows a change to them, and a color resolved on the live page the way the stylesheet resolves it.
/// </summary>
internal static class E2EColors
{
    /// <summary>
    /// The theme token (<c>--bit-clr-{token}</c>) a role's text is drawn in when it sits on the page rather than on the
    /// role's fill - the label of an Outline or Text variant, a colored text (role-text in color-role-maps.scss). It is the
    /// role's fg: the foreground shade of an accent role, whose main is picked to fill a surface; the on-color of a
    /// background or border role, whose main is the page itself or a hairline on it; and the main of a foreground role.
    /// The primary role is the one exception, which reads its main, the brand's link color.
    /// </summary>
    /// <param name="suffix">The class suffix of the role (BitCssClasses.Color): pri, sec, ... err, pbg, ... tbr.</param>
    public static string TextToken(string suffix) => suffix switch
    {
        "pri" => "pri",
        "sec" or "ter" or "inf" or "suc" or "wrn" or "swr" or "err" => $"{suffix}-fg",
        "pbg" or "sbg" or "tbg" or "pfg" or "pbr" or "sbr" or "tbr" => "fg-pri",
        "sfg" => "fg-sec",
        "tfg" => "fg-ter",
        _ => throw new ArgumentOutOfRangeException(nameof(suffix), suffix, "Not the class suffix of a color role."),
    };

    /// <summary>
    /// Resolves a CSS color (a keyword, a var() of a theme token) to the rgb() a computed style reports, by painting
    /// it on a probe element appended to the given parent (the body by default) so it inherits what that one does.
    /// </summary>
    public static Task<string> ResolveColorAsync(this IPage page, string cssColor, string? parentSelector = null)
    {
        return page.EvaluateAsync<string>(
            """
            ([value, parent]) => {
                const host = parent ? document.querySelector(parent) : document.body;
                const probe = document.createElement('div');
                probe.style.color = value;
                host.appendChild(probe);
                const resolved = getComputedStyle(probe).color;
                probe.remove();
                return resolved;
            }
            """, new object?[] { cssColor, parentSelector });
    }
}
