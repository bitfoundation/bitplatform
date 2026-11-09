namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>What the Surfaces section's BitModalService sample shows in its modal.</summary>
public partial class ThemeModalContent
{
    /// <summary>Closes the modal this content was opened in.</summary>
    [Parameter] public Action? OnClose { get; set; }

    private void Close() => OnClose?.Invoke();
}
