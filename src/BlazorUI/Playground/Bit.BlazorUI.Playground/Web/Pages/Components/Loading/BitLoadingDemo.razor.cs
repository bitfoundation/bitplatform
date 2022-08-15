using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Loading;

public partial class BitLoadingDemo
{
    public string OnClickValue { get; set; } = string.Empty;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Size",
            Type = "int",
            DefaultValue = "64",
            Description = "The Size of the loading component in px."
        },
        new()
        {
            Name = "Color",
            Type = "string",
            DefaultValue = "#FFFFFF",
            Description = "The Color of the loading component compatible with colors in CSS."
        }
    };

    private readonly string example1HTMLCode = @"<div class=""example-desc"">With items rendered as links</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""></BitBreadcrumb>
</div>

<div class=""example-desc"">With custom rendered divider and overflow icon</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""
                   DividerIcon=""BitIconName.Separator""
                   OverflowIndex=""1""
                   MaxDisplayedItems=""2""
                   OnRenderOverflowIcon=""BitIconName.ChevronDown""></BitBreadcrumb>
</div>

<div class=""example-desc"">With item OnClick event: @OnClickValue</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""></BitBreadcrumb>
</div>";

    private readonly string example2HTMLCode = @"<div class=""example-desc"">With no maxDisplayedItems</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""
                   AriaLabel=""Breadcrumb with no maxDisplayedItems""></BitBreadcrumb>
</div>
<div class=""example-desc"">With maxDisplayedItems set to 3</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""
                   MaxDisplayedItems=""3""
                   AriaLabel=""Breadcrumb with 3 maxDisplayedItems""></BitBreadcrumb>
</div>

<div class=""example-desc"">With maxDisplayedItems set to 2 and overflowIndex set to 1 (second element)""</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""
                   MaxDisplayedItems=""2""
                   OverflowIndex=""1""></BitBreadcrumb>
</div>";

    private readonly string example3HTMLCode = @"<div class=""example-desc"">BitBreadcrumb can be disabled or enabled by setting IsEnabled attribute.</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""
                   MaxDisplayedItems=""2""
                   OverflowIndex=""1""
                   IsEnabled=""false""></BitBreadcrumb>
</div>";

    private readonly string example4HTMLCode = @"<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2""
                   OverflowAriaLabel=""More Items""></BitBreadcrumb>
</div>";

    private readonly string example1CSharpCode = @"
public string OnClickValue { get; set; } = string.Empty;

private List<BitBreadcrumbItem> GetBreadcrumbItems()
{
    return new List<BitBreadcrumbItem>()
    {
        new()
        {
            Text = ""Folder 1"",
            Key = ""f1"",
            href = ""/components/breadcrumb"",
            OnClick = (() => OnClickValue = ""Folder 1 clicked"")
        },
        new()
        {
            Text = ""Folder 2"",
            Key = ""f2"",
            href = ""/components/breadcrumb"",
            OnClick = (() => OnClickValue = ""Folder 2 clicked"")
        },
        new()
        {
            Text = ""Folder 3"",
            Key = ""f3"",
            href = ""/components/breadcrumb"",
            OnClick = (() => OnClickValue = ""Folder 3 clicked"")
        },
        new()
        {
            Text = ""Folder 4"",
            Key = ""f4"",
            href = ""/components/breadcrumb"",
            IsCurrentItem = true,
            OnClick = (() => OnClickValue = ""Folder 4 clicked"")
        }
    };
}";
}
