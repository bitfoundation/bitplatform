namespace Boilerplate.Client.Core.Components.Pages;

public partial class ErrorPage
{
    [SupplyParameterFromQuery(Name = "message"), Parameter] public string? Message { get; set; }
    
}
