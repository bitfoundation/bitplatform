namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Tag;

public partial class BitTagDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Appearance",
            Type = "BitAppearance",
            DefaultValue = "BitAppearance.Primary",
            Description = "The appearance of tag, Possible values: Primary | Standard | Text",
            LinkType = LinkType.Link,
            Href = "#tag-appearance-enum"
        },
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
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The color of the tag.",
            LinkType = LinkType.Link,
            Href = "#tag-color-enum"
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
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the tag."
        },
        new()
        {
            Name = "Styles",
            Type = "BitTagClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitTag.",
            LinkType = LinkType.Link,
            Href = "#tag-class-styles"
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "tag-appearance-enum",
            Name = "BitAppearance",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Primary",
                    Description="The appearance for primary actions that are high-emphasis.",
                    Value="0",
                },
                new()
                {
                    Name= "Standard",
                    Description="The appearance for important actions that are medium-emphasis.",
                    Value="1",
                },
                new()
                {
                    Name= "Text",
                    Description="The appearance for less-pronounced actions.",
                    Value="2",
                }
            }
        },
        new()
        {
            Id = "tag-color-enum",
            Name = "BitColor",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
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
            }
        },
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "tag-class-styles",
            Title = "BitTagClassStyles",
            Parameters = new()
            {
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
            }
        }
    };



    private bool isDismissed;
    private bool isDismissed2;
    private bool isDismissed3;



    private readonly string example1RazorCode = @"
<BitTag Text=""Text"" />
<BitTag Text=""Text"" Appearance=""BitAppearance.Standard"" />
<BitTag Text=""Text"" Appearance=""BitAppearance.Text"" />";

    private readonly string example2RazorCode = @"
<BitTag Text=""Primary"" />
<BitTag Text=""Disabled"" IsEnabled=""false"" />";

    private readonly string example3RazorCode = @"
<BitTag Text=""Standard"" Appearance=""BitAppearance.Standard"" />
<BitTag Text=""Disabled"" Appearance=""BitAppearance.Standard"" IsEnabled=""false"" />";

    private readonly string example4RazorCode = @"
<BitTag Text=""Text"" Appearance=""BitAppearance.Text"" />
<BitTag Text=""Disabled"" Appearance=""BitAppearance.Text"" IsEnabled=""false"" />";

    private readonly string example5RazorCode = @"
<BitTag Text=""Text"" IconName=""@BitIconName.Calendar"" />";

    private readonly string example6RazorCode = @"
@if (isDismissed is false)
{
    <BitTag IconName=""@BitIconName.AlarmClock"" Text=""Dismiss me"" OnDismiss=""() => isDismissed = true"" />
}

<BitButton IsEnabled=""isDismissed"" ButtonStyle=""BitButtonStyle.Standard"" OnClick=""() => isDismissed = false"">
    Dismissed, click to reset
</BitButton>";
    private readonly string example6CsharpCode = @"
private bool isDismissed;";

    private readonly string example7RazorCode = @"
<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" />
<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" Appearance=""BitAppearance.Standard"" />
<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" Appearance=""BitAppearance.Text"" />

<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" />
<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" Appearance=""BitAppearance.Standard"" />
<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" Appearance=""BitAppearance.Text"" />

<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" />
<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" Appearance=""BitAppearance.Standard"" />
<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" Appearance=""BitAppearance.Text"" />

<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" />
<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" Appearance=""BitAppearance.Standard"" />
<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" Appearance=""BitAppearance.Text"" />

<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" />
<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" Appearance=""BitAppearance.Standard"" />
<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" Appearance=""BitAppearance.Text"" />";

    private readonly string example8RazorCode = @"
<BitTag>
    <BitStack Horizontal Gap=""0.5rem"" Style=""padding-inline: 0.5rem;"">
        <BitLabel>Custom content</BitLabel>
        <BitRollerLoading Size=""32"" />
    </BitStack>
</BitTag>";

    private readonly string example9RazorCode = @"
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
        Class=""custom-class"" Appearance=""BitAppearance.Standard"" />

@if (isDismissed2 is false)
{
    <BitTag Text=""Primary""
            IconName=""@BitIconName.People""
            OnDismiss=""() => isDismissed2 = true""
            Styles=""@(new() { Root = ""border-color: darkblue; border-width: 0.25rem;"",
                              Text = ""color: darkblue; font-weight: 600;"",
                              DismissIcon = ""color: darkblue; font-weight: 600;"" })"" />
}

@if (isDismissed3 is false)
{
    <BitTag Text=""Standard""
            IconName=""@BitIconName.People""
            OnDismiss=""() => isDismissed3 = true""
            Appearance=""BitAppearance.Standard""
            Classes=""@(new() { Root = ""custom-root"",
                               Icon = ""custom-icon"",
                               DismissButton = ""custom-dismiss"" })"" />
}

<BitButton IsEnabled=""@(isDismissed3 && isDismissed2)"" ButtonStyle=""BitButtonStyle.Standard"" OnClick=""() => isDismissed2 = isDismissed3 = false"">
    Dismissed, click to reset
</BitButton>";
    private readonly string example9CsharpCode = @"
private bool isDismissed2;
private bool isDismissed3;";
}
