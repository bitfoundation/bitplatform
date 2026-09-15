using Microsoft.AspNetCore.Components;

namespace Bit.Brouter.Tests.Harness.Pages;

public partial class MissingPage
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    protected override void OnParametersSet()
    {
#if NET10_0_OR_GREATER
        // Id 0 stands for an entity the page looked up and did not find: the .NET 10 not-found
        // contract, which Brouter has to turn into its fallback (and static rendering into a 404).
        if (Id == 0) Navigation.NotFound();
#endif
    }
}
