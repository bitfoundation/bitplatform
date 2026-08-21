namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class DemoSubEnumsTable
{
    [Parameter] public List<ComponentSubEnum> Enums { get; set; } = new();



    // The API tables are built from the constant lists a demo page declares once, so nothing about
    // them can change while the page is open. The hosting DemoPage, on the other hand, is handed a
    // fresh Examples render fragment every time anything on the page raises a state change, so it
    // re-renders in full - and would re-diff every one of the hundreds of rows below - on each of
    // them. Skipping that keeps a click inside a sample from paying for the whole API section.
    private bool _shouldRender = true;
    private List<ComponentSubEnum>? _renderedEnums;

    protected override void OnParametersSet()
    {
        _shouldRender = ReferenceEquals(_renderedEnums, Enums) is false;

        if (_shouldRender is false) return;

        _renderedEnums = Enums;
    }

    protected override bool ShouldRender() => _shouldRender;
}
