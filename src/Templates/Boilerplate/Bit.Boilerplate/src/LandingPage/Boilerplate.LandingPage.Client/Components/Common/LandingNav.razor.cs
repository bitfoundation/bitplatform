namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class LandingNav
{
    [Parameter, EditorRequired] public LandingSite Site { get; set; } = default!;

    [Parameter, EditorRequired] public RenderFragment Brand { get; set; } = default!;

    [Parameter] public IReadOnlyList<LandingLink> Links { get; set; } = [];

    [Parameter] public RenderFragment? Actions { get; set; }

    [Parameter] public bool ShowSignIn { get; set; }

    [Parameter] public string? Class { get; set; }

    private bool isMenuOpen;
}

public sealed record LandingLink(string Label, string Href);
