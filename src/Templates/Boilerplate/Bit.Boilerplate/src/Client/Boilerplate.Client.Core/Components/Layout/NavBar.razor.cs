namespace Boilerplate.Client.Core.Components.Layout;

public partial class NavBar
{
    [CascadingParameter(Name = Parameters.IsAuthenticated)] public bool? IsAuthenticated { get; set; }
}
