namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class DemoCssVariablesTable
{
    [Parameter] public string? Name { get; set; }

    [Parameter] public List<ComponentCssVariable> Variables { get; set; } = [];



    // Built from a constant list the demo page declares once, so it never re-diffs its rows for the
    // state changes the examples above it raise - the same guard DemoParametersTable carries.
    private bool _shouldRender = true;
    private string? _renderedName;
    private List<ComponentCssVariable>? _renderedVariables;

    protected override void OnParametersSet()
    {
        _shouldRender = _renderedName != Name || ReferenceEquals(_renderedVariables, Variables) is false;

        if (_shouldRender is false) return;

        _renderedName = Name;
        _renderedVariables = Variables;
    }

    protected override bool ShouldRender() => _shouldRender;
}
