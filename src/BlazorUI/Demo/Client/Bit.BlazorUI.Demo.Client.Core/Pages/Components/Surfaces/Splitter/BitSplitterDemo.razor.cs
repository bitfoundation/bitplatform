namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Splitter;

public partial class BitSplitterDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "GutterSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The size of BitSplitter gutter in pixels.",
        },
        new()
        {
            Name = "GutterIcon",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon of BitSplitter gutter.",
        },
        new()
        {
            Name = "FirstPanel",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content for the first panel.",
        },
        new()
        {
            Name = "FirstPanelSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The size of first panel.",
        },
        new()
        {
            Name = "FirstPanelMaxSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The max size of first panel.",
        },
        new()
        {
            Name = "FirstPanelMinSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The min size of first panel.",
        },
        new()
        {
            Name = "SecondPanel",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content for the second panel.",
        },
        new()
        {
            Name = "SecondPanelSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The size of second panel.",
        },
        new()
        {
            Name = "SecondPanelMaxSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The max size of second panel.",
        },
        new()
        {
            Name = "SecondPanelMinSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The min size of second panel.",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the orientation of BitSplitter to vertical.",
        },
    ];



    private double gutterSize = 10;



    private readonly string example1RazorCode = @"
<BitSplitter>
    <FirstPanel>
        <p style=""padding: 4px;"">
            First Panel -  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
            Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
        </p>
    </FirstPanel>
    <SecondPanel>
        <p style=""padding: 4px;"">
            Second Panel -  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
            Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
        </p>
    </SecondPanel>
</BitSplitter>";

    private readonly string example2RazorCode = @"
<BitSplitter Vertical>
    <FirstPanel>
        <p style=""padding: 4px;"">
            First Panel -  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
            Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
        </p>
    </FirstPanel>
    <SecondPanel>
        <p style=""padding: 4px;"">
            Second Panel -  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
            Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
        </p>
    </SecondPanel>
</BitSplitter>";

    private readonly string example3RazorCode = @"
<BitSplitter FirstPanelMinSize=""128"" FirstPanelSize=""128"" SecondPanelMinSize=""64"">
    <FirstPanel>
        <p style=""padding: 4px;"">
            First Panel -  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
            Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
        </p>
    </FirstPanel>
    <SecondPanel>
        <p style=""padding: 4px;"">
            Second Panel -  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
            Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
        </p>
    </SecondPanel>
</BitSplitter>";

    private readonly string example4RazorCode = @"
<BitSplitter>
    <FirstPanel>
        <p style=""padding: 4px;"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
            Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
        </p>
    </FirstPanel>
    <SecondPanel>
        <BitSplitter Vertical>
            <FirstPanel>
                <p style=""padding: 4px;"">
                    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
                    Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
                </p>
            </FirstPanel>
            <SecondPanel>
                <p style=""padding: 4px;"">
                    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
                    Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
                </p>
            </SecondPanel>
        </BitSplitter>
    </SecondPanel>
</BitSplitter>";

    private readonly string example5RazorCode = @"
<BitSlider @bind-Value=""gutterSize"" Max=""50"" />

<BitSplitter GutterSize=""@((int)gutterSize)"">
    <FirstPanel>
        <p style=""padding: 4px;"">
            First Panel -  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
            Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
        </p>
    </FirstPanel>
    <SecondPanel>
        <p style=""padding: 4px;"">
            Second Panel -  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
            Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
        </p>
    </SecondPanel>
</BitSplitter>";
    private readonly string example5CsharpCode = @"
private double gutterSize = 10;
";

    private readonly string example6RazorCode = @"
<BitSplitter GutterIcon=""@BitIconName.GripperDotsVertical"">
    <FirstPanel>
        <p style=""padding: 4px;"">
            First Panel -  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
            Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
        </p>
    </FirstPanel>
    <SecondPanel>
        <p style=""padding: 4px;"">
            Second Panel -  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
            Eget dolor morbi non arcu risus quis varius. Turpis tincidunt id aliquet risus feugiat in ante.
        </p>
    </SecondPanel>
</BitSplitter>";
}
