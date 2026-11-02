namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class LandingFooter
{
    [AutoInject] private LandingConsentPrompt consentPrompt = default!;

    [Parameter, EditorRequired] public LandingSite Site { get; set; } = default!;

    [Parameter, EditorRequired] public RenderFragment Brand { get; set; } = default!;

    [Parameter] public string? Tagline { get; set; }

    [Parameter] public RenderFragment? Intro { get; set; }

    [Parameter] public RenderFragment? Columns { get; set; }

    [Parameter] public string? Class { get; set; }
}
