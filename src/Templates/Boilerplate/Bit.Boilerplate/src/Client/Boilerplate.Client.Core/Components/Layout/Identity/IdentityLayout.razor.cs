namespace Boilerplate.Client.Core.Components.Layout.Identity;

public partial class IdentityLayout
{
    [CascadingParameter(Name = Parameters.IsAnonymousPage)] private bool? isAnonymousPage { get; set; }
}
