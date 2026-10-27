using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Samples.Server.Components.Pages;

public partial class Error
{
    [CascadingParameter] private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }

    protected override void OnInitialized() =>
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
