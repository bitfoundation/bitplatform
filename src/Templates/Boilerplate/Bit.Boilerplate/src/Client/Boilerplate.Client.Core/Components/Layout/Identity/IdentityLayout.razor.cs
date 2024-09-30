namespace Boilerplate.Client.Core.Components.Layout.Identity;

public partial class IdentityLayout
{
    [CascadingParameter(Name = Parameters.CurrentUrl)] private string? CurrentUrl { get; set; }
    [CascadingParameter(Name = Parameters.IsAnonymousPage)] private bool? IsAnonymousPage { get; set; }
}
