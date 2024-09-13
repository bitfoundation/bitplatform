namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Header;

public partial class BitHeaderDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Gets or sets the content to be rendered inside the BitHeader.",
        },
        new()
        {
            Name = "Height",
            Type = "int?",
            DefaultValue = "null",
            Description = "Gets or sets the height of the BitHeader (in pixels).",
        },
        new()
        {
            Name = "Fixed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the header with a fixed position at the top of the page.",
        }
    ];



    private readonly string example1RazorCode = @"
<BitHeader>I'm a Header</BitHeader>";

    private readonly string example2RazorCode = @"
<BitHeader Style=""gap: 1rem;"">
    <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.GlobalNavButton"" Title=""Open Navigation""/>
    <BitText Typography=""BitTypography.Caption1"">My Awesome App</BitText>
    <BitSpacer />
    <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Contact"" Title=""Sign in"" />
    <BitMenuButton TItem=""BitMenuButtonOption""
                   ChevronDownIcon=""@BitIconName.More""
                   Variant=""BitVariant.Text""
                   title=""See more""
                   Styles=""@(new() { OperatorButton = ""padding: 0.5rem;"" })"">
        <BitMenuButtonOption Text=""Settings"" IconName=""@BitIconName.Settings"" />
        <BitMenuButtonOption Text=""About"" IconName=""@BitIconName.Info"" />
        <BitMenuButtonOption Text=""Feedback"" IconName=""@BitIconName.Feedback"" />
    </BitMenuButton>
</BitHeader>";
}
