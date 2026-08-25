namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class DemoParametersTable
{
    [Parameter] public string? Name { get; set; }
    [Parameter] public string NameSuffix { get; set; } = "parameters";

    [Parameter] public List<ComponentParameter> Parameters { get; set; } = new();



    // The API tables are built from the constant lists a demo page declares once, so nothing about
    // them can change while the page is open. The hosting DemoPage, on the other hand, is handed a
    // fresh Examples render fragment every time anything on the page raises a state change, so it
    // re-renders in full - and would re-diff every one of the hundreds of rows below - on each of
    // them. Skipping that keeps a click inside a sample from paying for the whole API section.
    private bool _shouldRender = true;
    private string? _renderedName;
    private string? _renderedNameSuffix;
    private List<ComponentParameter>? _renderedParameters;

    protected override void OnParametersSet()
    {
        _shouldRender = _renderedName != Name
                     || _renderedNameSuffix != NameSuffix
                     || ReferenceEquals(_renderedParameters, Parameters) is false;

        if (_shouldRender is false) return;

        _renderedName = Name;
        _renderedNameSuffix = NameSuffix;
        _renderedParameters = Parameters;
    }

    protected override bool ShouldRender() => _shouldRender;
}
