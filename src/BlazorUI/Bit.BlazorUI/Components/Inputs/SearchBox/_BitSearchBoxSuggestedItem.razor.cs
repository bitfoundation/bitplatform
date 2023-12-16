namespace Bit.BlazorUI;

public partial class _BitSearchBoxSuggestedItem : ComponentBase
{
    [Parameter] public string Text { get; set; } = default!;

    [Parameter] public BitSearchBox SearchBox { get; set; } = default!;
}
