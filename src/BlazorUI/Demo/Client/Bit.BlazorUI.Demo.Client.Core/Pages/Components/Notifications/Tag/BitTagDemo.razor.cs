namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Tag;

public partial class BitTagDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Child content of component, the content that the tag will apply to."
        },
        new()
        {
            Name = "Classes",
            Type = "BitTagClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitTag.",
            LinkType = LinkType.Link,
            Href = "#tag-class-styles"
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the Icon to use in the tag."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Click event handler of the tag."
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Dismiss button click event, if set the dismiss icon will show up."
        },
        new()
        {
            Name = "Severity",
            Type = "BitSeverity?",
            DefaultValue = "null",
            Description = "The severity of the tag.",
            LinkType = LinkType.Link,
            Href = "#severity-enum"
        },
        new()
        {
            Name = "Styles",
            Type = "BitTagClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitTag.",
            LinkType = LinkType.Link,
            Href = "#tag-class-styles"
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the tag."
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the tag.",
            LinkType = LinkType.Link,
            Href = "#variant-enum"
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "tag-class-styles",
            Title = "BitTagClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitTag."
               },
               new()
               {
                   Name = "Content",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the content of the BitTag."
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text of the BitTag."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitTag."
               },
               new()
               {
                   Name = "DismissButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dismiss button of the BitTag."
               },
               new()
               {
                   Name = "DismissIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dismiss icon of the BitTag."
               },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new()
                {
                    Name= "Fill",
                    Description="Fill styled variant.",
                    Value="0",
                },
                new()
                {
                    Name= "Outline",
                    Description="Outline styled variant.",
                    Value="1",
                },
                new()
                {
                    Name= "Text",
                    Description="Text styled variant.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "severity-enum",
            Name = "BitSeverity",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Info",
                    Description="Info styled tag.",
                    Value="0",
                },
                new()
                {
                    Name= "Success",
                    Description="Success styled tag.",
                    Value="1",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning styled tag.",
                    Value="2",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="Severe Warning styled tag.",
                    Value="3",
                },
                new()
                {
                    Name= "Error",
                    Description="Error styled tag.",
                    Value="4",
                }
            ]
        },
    ];



    private bool isDismissed;
    private bool isDismissed2;
    private bool isDismissed3;



    private readonly string example1RazorCode = @"
<BitTag Text=""Text"" />";

    private readonly string example2RazorCode = @"
<BitTag Text=""Text"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Text"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Text"" Variant=""BitVariant.Text"" />";

    private readonly string example3RazorCode = @"
<BitTag Text=""Enabled"" />
<BitTag Text=""Disabled"" IsEnabled=""false"" />";

    private readonly string example4RazorCode = @"
<BitTag Text=""Enabled"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Disabled"" Variant=""BitVariant.Outline"" IsEnabled=""false"" />";

    private readonly string example5RazorCode = @"
<BitTag Text=""Enabled"" Variant=""BitVariant.Text"" />
<BitTag Text=""Disabled"" Variant=""BitVariant.Text"" IsEnabled=""false"" />";

    private readonly string example6RazorCode = @"
<BitTag Text=""Text"" IconName=""@BitIconName.Calendar"" />";

    private readonly string example7RazorCode = @"
@if (isDismissed is false)
{
    <BitTag IconName=""@BitIconName.AlarmClock"" Text=""Dismiss me"" OnDismiss=""() => isDismissed = true"" />
}

<BitButton IsEnabled=""isDismissed"" Variant=""BitVariant.Outline"" OnClick=""() => isDismissed = false"">
    Dismissed, click to reset
</BitButton>";
    private readonly string example7CsharpCode = @"
private bool isDismissed;";

    private readonly string example8RazorCode = @"
<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.Info"" />
<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.Info"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.Info"" Variant=""BitVariant.Text"" />

<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.Success"" />
<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.Success"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.Success"" Variant=""BitVariant.Text"" />

<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.Warning"" />
<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.Warning"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.Warning"" Variant=""BitVariant.Text"" />

<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.SevereWarning"" />
<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.SevereWarning"" Variant=""BitVariant.Outline"" />
<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.SevereWarning"" Variant=""BitVariant.Text"" />

<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.Error"" />
<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.Error"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Severity=""BitSeverity.Error"" Variant=""BitVariant.Text"" />";

    private readonly string example9RazorCode = @"
<BitTag>
    <BitStack Horizontal Gap=""0.5rem"" Style=""padding-inline: 0.5rem;"">
        <BitLabel>Custom content</BitLabel>
        <BitRollerLoading Size=""32"" />
    </BitStack>
</BitTag>";

    private readonly string example10RazorCode = @"
<style>
    .custom-class {
        border-radius: 0.25rem;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-root {
        color: mediumpurple;
        border-color: mediumpurple;
        border-radius: 0.5rem;
        box-shadow: mediumpurple 0 0 0.5rem;
    }

    .custom-icon {
        font-size: 1.25rem;
        font-weight: bolder;
    }

    .custom-dismiss {
        margin: 0.25rem;
        border-radius: 50%;
        border: 1px solid mediumpurple;
    }
</style>


<BitTag Text=""Styled Tag""
        IconName=""@BitIconName.People""
        Style=""border-radius: 1rem; font-weight:bold"" />
<BitTag Text=""Classed Tag""
        IconName=""@BitIconName.People""
        Class=""custom-class"" Variant=""BitVariant.Outline"" />

@if (isDismissed2 is false)
{
    <BitTag Text=""Fill""
            IconName=""@BitIconName.People""
            OnDismiss=""() => isDismissed2 = true""
            Styles=""@(new() { Root = ""border-color: darkblue; border-width: 0.25rem;"",
                              Text = ""color: darkblue; font-weight: 600;"",
                              DismissIcon = ""color: darkblue; font-weight: 600;"" })"" />
}

@if (isDismissed3 is false)
{
    <BitTag Text=""Outline""
            IconName=""@BitIconName.People""
            OnDismiss=""() => isDismissed3 = true""
            Variant=""BitVariant.Outline""
            Classes=""@(new() { Root = ""custom-root"",
                               Icon = ""custom-icon"",
                               DismissButton = ""custom-dismiss"" })"" />
}

<BitButton IsEnabled=""@(isDismissed3 && isDismissed2)"" Variant=""BitVariant.Outline"" OnClick=""() => isDismissed2 = isDismissed3 = false"">
    Dismissed, click to reset
</BitButton>";
    private readonly string example10CsharpCode = @"
private bool isDismissed2;
private bool isDismissed3;";
}
