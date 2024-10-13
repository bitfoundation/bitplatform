namespace Boilerplate.Client.Core.Components.Layout.Identity;

public partial class IdentityLayout
{
    [CascadingParameter(Name = Parameters.IsCrossLayoutPage)] private bool? isCrossLayoutPage { get; set; }
}
