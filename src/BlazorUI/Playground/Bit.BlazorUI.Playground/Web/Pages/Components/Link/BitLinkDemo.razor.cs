using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Link;

public partial class BitLinkDemo
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "ChildContent",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "The content of link, can be any custom tag or a text.",
        },
        new ComponentParameter()
        {
            Name = "HasUnderline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the link is styled with an underline or not.",
        },
        new ComponentParameter()
        {
            Name = "Href",
            Type = "string",
            DefaultValue = "",
            Description = "URL the link points to.",
        },
        new ComponentParameter()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the action button clicked.",
        },
        new ComponentParameter()
        {
            Name = "Target",
            Type = "string",
            DefaultValue = "",
            Description = "If Href provided, specifies how to open the link.",
        },
        new ComponentParameter()
        {
            Name = "Title",
            Type = "string",
            DefaultValue = "",
            Description = "The title to show when the mouse is placed on the action button.",
        },
    };

    private void LinkOnClick()
    {
        // Here you can do something else...

        Navigation.NavigateTo("https://github.com/bitfoundation/bitplatform");
    }

    private readonly string example1HTMLCode = @"
<BitLink Href=""https://github.com/bitfoundation/bitplatform"">Basic Link</BitLink>
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" IsEnabled=""false"">Disabled Link</BitLink>
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"">Blank Target Link</BitLink>
<BitLink Href=""https://github.com/bitfoundation/bitplatform"" HasUnderline=""true"">Underlined link</BitLink>
<BitLink OnClick=""LinkOnClick"">Link with OnClick</BitLink>
";

    private readonly string example1CSharpCode = @"
@inject NavigationManager Navigation

private void LinkOnClick()
{
    // Here you can do something else...

    Navigation.NavigateTo(""https://github.com/bitfoundation/bitplatform"");
}
";
}
