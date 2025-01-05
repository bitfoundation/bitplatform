namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.DropMenu;

public partial class BitDropMenuDemo
{
    private readonly string example1RazorCode = @"
<BitDropMenu Text=""Basic"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitButton>Click me</BitButton>
        <BitToggle>Toggle me</BitToggle>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Disabled"" IsEnabled=""false"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitButton>Click me</BitButton>
        <BitToggle>Toggle me</BitToggle>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Transparent"" Transparent>
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitButton>Click me</BitButton>
        <BitToggle>Toggle me</BitToggle>
    </BitStack>
</BitDropMenu>";

    private readonly string example2RazorCode = @"
<BitDropMenu Text=""IconName"" IconName=""@BitIconName.Emoji2"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""ChevronDownIcon"" ChevronDownIcon=""@BitIconName.DoubleChevronDown"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>";

    private readonly string example3RazorCode = @"
<BitDropMenu Text=""Responsive"" Responsive>
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>";

    private readonly string example4RazorCode = @"
<BitDropMenu Text=""Add Icon"" IconName=""@BitIconName.Emoji2"">
    <Template>
        <div style=""display:flex;gap:10px;"">
            <BitIcon IconName=""@BitIconName.Airplane"" Color=""BitColor.Tertiary"" />
            <span>A template</span>
            <BitRippleLoading CustomSize=""20"" Color=""BitColor.Tertiary"" />
        </div>
    </Template>
    <Body>
        <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
            <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        </BitStack>
    </Body>
</BitDropMenu>";

    private readonly string example5RazorCode = @"
<BitDropMenu Text=""@($""Click me ({clickCounter})"")"" OnClick=""() => clickCounter++"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>";
    private readonly string example5CsharpCode = @"
private int clickCounter;";

    private readonly string example6RazorCode = @"
<style>
    .custom-class {
        border-radius: 1rem;
        border-color: blueviolet;
        transition: background-color 1s;
        background: linear-gradient(90deg, magenta, transparent) blue;
    }

    .custom-class:hover {
        border-color: magenta;
        background-color: magenta;
    }

    .custom-root {
        color: aqua;
        min-width: 7.2rem;
        font-weight: bold;
        border-color: aqua;
        border-radius: 1rem;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-root:hover {
        background-color: gray;
    }

    .custom-text {
        text-shadow: tomato 0 0 0.5rem;
    }

    .custom-opened {
        color: green;
    }
</style>

<BitDropMenu Text=""Styled Drop menu"" Style=""background-color: transparent; border-color: blueviolet; color: blueviolet;"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Classed Drop menu"" Class=""custom-class"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Styled Drop menu""
             Styles=""@(new() { Root = ""background-color: peachpuff; border-color: peachpuff; min-width: 6rem;"",
                               Text = ""color: tomato; font-weight: bold;""
                               Opened = ""border-color: tomato; background-color: goldenrod;"" })"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Classed Drop menu""
             Classes=""@(new() { Root = ""custom-root"",
                                Text = ""custom-text"",
                                Opened = ""custom-opened"" })"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>";

    private readonly string example7RazorCode = @"
<BitDropMenu Text=""منو"" Dir=""BitDir.Rtl"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">این یک محتوای تستی می باشد.</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""ریسپانسیو منو"" Dir=""BitDir.Rtl"" Responsive>
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">این یک محتوای تستی می باشد.</BitText>
    </BitStack>
</BitDropMenu>";

}
