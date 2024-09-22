namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonCustomDemo
{
    private readonly string example1RazorCode = @"
<BitMenuButton Text=""MenuButton"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example1CsharpCode = @"
public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<Operation> nameSelectors = new()
{
    Text = { Name = nameof(Operation.Name) },
    Key = { Name = nameof(Operation.Id) },
    IconName = { Name = nameof(Operation.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example2RazorCode = @"
<BitMenuButton Text=""Split"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Split />";
    private readonly string example2CsharpCode = @"
public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<Operation> nameSelectors = new()
{
    Text = { Name = nameof(Operation.Name) },
    Key = { Name = nameof(Operation.Id) },
    IconName = { Name = nameof(Operation.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example3RazorCode = @"
<BitMenuButton Text=""Fill"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" />
<BitMenuButton Text=""Outline"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" />
<BitMenuButton Text=""Text"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" />

<BitMenuButton Text=""Fill"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" IsEnabled=""false"" />
<BitMenuButton Text=""Outline"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" IsEnabled=""false"" />
<BitMenuButton Text=""Text"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" IsEnabled=""false"" />

<BitMenuButton Text=""Fill"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Split />
<BitMenuButton Text=""Outline"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Split />
<BitMenuButton Text=""Text"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Split />

<BitMenuButton Text=""Fill"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Outline"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Text"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" IsEnabled=""false"" Split />";
    private readonly string example3CsharpCode = @"
public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<Operation> nameSelectors = new()
{
    Text = { Name = nameof(Operation.Name) },
    Key = { Name = nameof(Operation.Id) },
    IconName = { Name = nameof(Operation.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example4RazorCode = @"
<BitMenuButton Text=""Primary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"" />
<BitMenuButton Text=""Primary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"" />
<BitMenuButton Text=""Primary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"" />

<BitMenuButton Text=""Primary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"" Split />
<BitMenuButton Text=""Primary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"" Split />
<BitMenuButton Text=""Primary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"" Split />


<BitMenuButton Text=""Secondary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"" />
<BitMenuButton Text=""Secondary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" />
<BitMenuButton Text=""Secondary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"" />

<BitMenuButton Text=""Secondary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"" Split />
<BitMenuButton Text=""Secondary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" Split />
<BitMenuButton Text=""Secondary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"" Split />


<BitMenuButton Text=""Tertiary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"" />
<BitMenuButton Text=""Tertiary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" />
<BitMenuButton Text=""Tertiary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" />

<BitMenuButton Text=""Tertiary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"" Split />
<BitMenuButton Text=""Tertiary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" Split />
<BitMenuButton Text=""Tertiary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" Split />


<BitMenuButton Text=""Info"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" />
<BitMenuButton Text=""Info"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" />
<BitMenuButton Text=""Info"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Info"" />

<BitMenuButton Text=""Info"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" Split />
<BitMenuButton Text=""Info"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" Split />
<BitMenuButton Text=""Info"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Info"" Split />


<BitMenuButton Text=""Success"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" />
<BitMenuButton Text=""Success"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"" />
<BitMenuButton Text=""Success"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Success"" />

<BitMenuButton Text=""Success"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" Split />
<BitMenuButton Text=""Success"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"" Split />
<BitMenuButton Text=""Success"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Success"" Split />


<BitMenuButton Text=""Warning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"" />
<BitMenuButton Text=""Warning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"" />
<BitMenuButton Text=""Warning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"" />

<BitMenuButton Text=""Warning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"" Split />
<BitMenuButton Text=""Warning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"" Split />
<BitMenuButton Text=""Warning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"" Split />


<BitMenuButton Text=""SevereWarning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"" />
<BitMenuButton Text=""SevereWarning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"" />
<BitMenuButton Text=""SevereWarning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"" />

<BitMenuButton Text=""SevereWarning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"" Split />
<BitMenuButton Text=""SevereWarning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"" Split />
<BitMenuButton Text=""SevereWarning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"" Split />


<BitMenuButton Text=""Error"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"" />
<BitMenuButton Text=""Error"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" />
<BitMenuButton Text=""Error"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Error"" />

<BitMenuButton Text=""Error"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"" Split />
<BitMenuButton Text=""Error"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" Split />
<BitMenuButton Text=""Error"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Error"" Split />


<BitMenuButton Text=""PrimaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"" />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"" />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"" />

<BitMenuButton Text=""PrimaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"" Split />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"" Split />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"" Split />


<BitMenuButton Text=""SecondaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"" />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"" />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"" />

<BitMenuButton Text=""SecondaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"" Split />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"" Split />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"" Split />


<BitMenuButton Text=""TertiaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"" />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" />

<BitMenuButton Text=""TertiaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"" Split />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" Split />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" Split />


<BitMenuButton Text=""PrimaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"" />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"" />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"" />

<BitMenuButton Text=""PrimaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"" Split />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"" Split />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"" Split />


<BitMenuButton Text=""SecondaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"" />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"" />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"" />

<BitMenuButton Text=""SecondaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"" Split />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"" Split />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"" Split />


<BitMenuButton Text=""TertiaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"" />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"" />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"" />

<BitMenuButton Text=""TertiaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"" Split />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"" Split />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"" Split />


<BitMenuButton Text=""PrimaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"" />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"" />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"" />

<BitMenuButton Text=""PrimaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"" Split />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"" Split />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"" Split />


<BitMenuButton Text=""SecondaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"" />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"" />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"" />

<BitMenuButton Text=""SecondaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"" Split />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"" Split />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"" Split />


<BitMenuButton Text=""TertiaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"" />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"" />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"" />

<BitMenuButton Text=""TertiaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"" Split />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"" Split />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"" Split />";
    private readonly string example4CsharpCode = @"
public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<Operation> nameSelectors = new()
{
    Text = { Name = nameof(Operation.Name) },
    Key = { Name = nameof(Operation.Id) },
    IconName = { Name = nameof(Operation.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example5RazorCode = @"
<BitMenuButton Text=""Small"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Size=""BitSize.Small"" />
<BitMenuButton Text=""Small"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Size=""BitSize.Small"" />
<BitMenuButton Text=""Small"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Size=""BitSize.Small"" />

<BitMenuButton Text=""Medium"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Size=""BitSize.Medium"" />
<BitMenuButton Text=""Medium"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Size=""BitSize.Medium"" />
<BitMenuButton Text=""Medium"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Size=""BitSize.Medium"" />

<BitMenuButton Text=""Large"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Size=""BitSize.Large"" />
<BitMenuButton Text=""Large"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Size=""BitSize.Large"" />
<BitMenuButton Text=""Large"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Size=""BitSize.Large"" />";
    private readonly string example5CsharpCode = @"
public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<Operation> nameSelectors = new()
{
    Text = { Name = nameof(Operation.Name) },
    Key = { Name = nameof(Operation.Id) },
    IconName = { Name = nameof(Operation.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example6RazorCode = @"
<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Sticky />
<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Split Sticky />

<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Sticky />
<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Split Sticky />

<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Sticky />
<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Split Sticky />";
    private readonly string example6CsharpCode = @"
public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<Operation> nameSelectors = new()
{
    Text = { Name = nameof(Operation.Name) },
    Key = { Name = nameof(Operation.Id) },
    IconName = { Name = nameof(Operation.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example7RazorCode = @"
<BitMenuButton Text=""IconName"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" IconName=""@BitIconName.Edit"" />
<BitMenuButton Text=""ChevronDownIcon"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" ChevronDownIcon=""@BitIconName.DoubleChevronDown"" Split />";
    private readonly string example7CsharpCode = @"
public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private static List<Operation> basicIconCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"", Icon = BitIconName.Emoji },
    new() { Name = ""Custom B"", Id = ""B"", Icon = BitIconName.Emoji },
    new() { Name = ""Custom C"", Id = ""C"", Icon = BitIconName.Emoji2 }
];

private BitMenuButtonNameSelectors<Operation> nameSelectors = new()
{
    Text = { Name = nameof(Operation.Name) },
    Key = { Name = nameof(Operation.Id) },
    IconName = { Name = nameof(Operation.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        margin-inline: 1rem;
        border-radius: 1rem;
        border-color: tomato;
        border-width: 0.25rem;
    }

    .custom-class > button {
        color: tomato;
        border-color: tomato;
        background: transparent;
    }

    .custom-class > button:hover {
        background-color: #ff63473b;
    }


    .custom-item {
        color: peachpuff;
        background-color: tomato;
    }


    .custom-button {
        color: deepskyblue;
        background: transparent;
    }

    .custom-opened .custom-button {
        color: cornflowerblue;
    }

    .custom-callout {
        border-radius: 1rem;
        border-color: lightgray;
        backdrop-filter: blur(20px);
        background-color: transparent;
        box-shadow: darkgray 0 0 0.5rem;
    }

    .custom-item-button {
        border-bottom: 1px solid gray;
    }

    .custom-item-button:hover {
        background-color: rgba(255, 255, 255, 0.2);
    }

    .custom-callout li:last-child .custom-item-button {
        border-bottom: none;
    }
</style>


<BitMenuButton Text=""Styled Button"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Style=""border-radius: 1rem; margin: 1rem; box-shadow: aqua 0 0 1rem; overflow: hidden;"" />
<BitMenuButton Text=""Classed Button"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Class=""custom-class"" Variant=""BitVariant.Outline"" />


<BitMenuButton Text=""Item Styled & Classed Button"" Items=""itemStyleClassCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" />


<BitMenuButton Text=""Classes"" Items=""basicCustoms"" IconName=""@BitIconName.FormatPainter"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text""
               Classes=""@(new() { OperatorButton = ""custom-button"",
                                  Opened = ""custom-opened"",
                                  Callout = ""custom-callout"",
                                  ItemButton = ""custom-item-button"" })"" />

<BitMenuButton Text=""Styles"" Items=""basicCustoms"" IconName=""@BitIconName.Brush"" NameSelectors=""nameSelectors""
               Styles=""@(new() { Root = ""--button-background: tomato; background: var(--button-background); border-color: var(--button-background); border-radius: 0.25rem;"",
                                 Opened = ""--button-background: orangered;"",
                                 OperatorButton = ""background: var(--button-background);"",
                                 ItemButton = ""background: lightcoral;"",
                                 Callout = ""border-radius: 0.25rem; box-shadow: lightgray 0 0 0.5rem;"" })"" />";
    private readonly string example8CsharpCode = @"
public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private static List<Operation> itemStyleClassCustoms =
[
    new() { Name = ""Custom A (Default)"", Id = ""A"", Icon = BitIconName.Emoji },
    new() { Name = ""Custom B (Styled)"", Id = ""B"", Icon = BitIconName.Emoji, Style = ""color: tomato; border-color: brown; background-color: peachpuff;"" },
    new() { Name = ""Custom C (Classed)"", Id = ""C"", Icon = BitIconName.Emoji2, Class = ""custom-item"" },
];

private BitMenuButtonNameSelectors<Operation> nameSelectors = new()
{
    Text = { Name = nameof(Operation.Name) },
    Key = { Name = nameof(Operation.Id) },
    IconName = { Name = nameof(Operation.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example9RazorCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>


<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>

<BitMenuButton Text=""Customs"" Items=""itemTemplateCustoms"" NameSelectors=""nameSelectors"" Split>
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color:brown"">@item.Name (@item.Id)</span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<BitMenuButton Text=""Customs"" Items=""itemTemplateCustoms2"" NameSelectors=""nameSelectors"" />";
    private readonly string example9CsharpCode = @"
public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
    public RenderFragment<Operation>? Fragment { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private List<Operation> itemTemplateCustoms =
[
    new() { Name = ""Add"", Id = ""add-key"", Icon = BitIconName.Add },
    new() { Name = ""Edit"", Id = ""edit-key"", Icon = BitIconName.Edit },
    new() { Name = ""Delete"", Id = ""delete-key"", Icon = BitIconName.Delete }
];

private List<Operation> itemTemplateCustoms2 = 
[
    new()
    {
        Name = ""Add"", Id = ""add-key"", Icon = BitIconName.Add,
        Fragment = (item => @<div class=""item-template-box"" style=""color:green"">@item.Name (@item.Id)</div>)
    },
    new()
    {
        Name = ""Edit"", Id = ""edit-key"", Icon = BitIconName.Edit,
        Fragment = (item => @<div class=""item-template-box"" style=""color:yellow"">@item.Name (@item.Id)</div>)
    },
    new()
    {
        Name = ""Delete"", Id = ""delete-key"", Icon = BitIconName.Delete,
        Fragment = (item => @<div class=""item-template-box"" style=""color:red"">@item.Name (@item.Id)</div>)
    }
];

private BitMenuButtonNameSelectors<Operation> nameSelectors = new()
{
    Text = { Name = nameof(Operation.Name) },
    Key = { Name = nameof(Operation.Id) },
    IconName = { Name = nameof(Operation.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false },
    Template = { Name = nameof(Operation.Fragment) }
};";

    private readonly string example10RazorCode = @"
<BitMenuButton Text=""Customs"" Items=""basicCustoms"" NameSelectors=""nameSelectors""
               OnChange=""(Operation item) => eventsChangedCustom = item?.Id""
               OnClick=""(Operation item) => eventsClickedCustom = item?.Id"" />

<BitMenuButton Split Text=""Customs"" Items=""basicCustomsOnClick"" NameSelectors=""nameSelectors""
               OnChange=""(Operation item) => eventsChangedCustom = item?.Id""
               OnClick=""@((Operation item) => eventsClickedCustom = ""Main button clicked"")"" />

<BitMenuButton Sticky Items=""basicCustoms"" NameSelectors=""nameSelectors""
               OnChange=""(Operation item) => eventsChangedCustom = item?.Id""
               OnClick=""(Operation item) => eventsClickedCustom = item?.Id"" />

<BitMenuButton Sticky Split Items=""basicCustomsOnClick"" NameSelectors=""nameSelectors""
               OnChange=""(Operation item) => eventsChangedCustom = item?.Id""
               OnClick=""(Operation item) => eventsClickedCustom = item?.Id"" />

<div>Changed custom item: @eventsChangedCustom</div>
<div>Clicked custom item: @eventsClickedCustom</div>";
    private readonly string example10CsharpCode = @"
public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private List<Operation> basicCustomsOnClick =
[
    new() { Name = ""Custom A"", Id = ""A"", Icon = BitIconName.Emoji },
    new() { Name = ""Custom B"", Id = ""B"", Icon = BitIconName.Emoji },
    new() { Name = ""Custom C"", Id = ""C"", Icon = BitIconName.Emoji2 }
];

private BitMenuButtonNameSelectors<Operation> nameSelectors = new()
{
    Text = { Name = nameof(Operation.Name) },
    Key = { Name = nameof(Operation.Id) },
    IconName = { Name = nameof(Operation.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example11RazorCode = @"
<BitMenuButton Split Sticky Items=""basicCustoms"" DefaultSelectedItem=""basicCustoms[1]"" NameSelectors=""nameSelectors"" />

<BitMenuButton Sticky Items=""basicCustoms"" NameSelectors=""nameSelectors"" @bind-SelectedItem=""twoWaySelectedCustom"" />
<BitChoiceGroup Horizontal Items=""@choiceGroupCustoms"" @bind-Value=""@twoWaySelectedCustom"" />

<BitMenuButton Sticky Items=""isSelectedCustoms"" NameSelectors=""nameSelectors"" />

<BitMenuButton Sticky Items=""basicCustoms"" NameSelectors=""nameSelectors"" IsOpen=""oneWayIsOpen"" />
<BitCheckbox Label=""One-way IsOpen"" @bind-Value=""oneWayIsOpen"" OnChange=""async _ => { await Task.Delay(2000); oneWayIsOpen = false; }"" />

<BitMenuButton Sticky Items=""basicCustoms"" NameSelectors=""nameSelectors"" @bind-IsOpen=""twoWayIsOpen"" />
<BitCheckbox Label=""Two-way IsOpen"" @bind-Value=""twoWayIsOpen"" />";
    private readonly string example11CsharpCode = @"
private Operation twoWaySelectedCustom = default!;
private bool oneWayIsOpen;
private bool twoWayIsOpen;

public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private static List<Operation> isSelectedCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"", Icon = BitIconName.Emoji },
    new() { Name = ""Custom B"", Id = ""B"", Icon = BitIconName.Emoji },
    new() { Name = ""Custom C"", Id = ""C"", Icon = BitIconName.Emoji2, IsSelected = true }
];

private BitMenuButtonNameSelectors<Operation> nameSelectors = new()
{
    Text = { Name = nameof(Operation.Name) },
    Key = { Name = nameof(Operation.Id) },
    IconName = { Name = nameof(Operation.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example12RazorCode = @"
<BitMenuButton Text=""گزینه ها"" Dir=""BitDir.Rtl"" Items=""rtlCustoms"" IconName=""@BitIconName.Edit"" NameSelectors=""nameSelectors"" />
<BitMenuButton Text=""گرینه ها"" Dir=""BitDir.Rtl"" Items=""rtlCustoms"" ChevronDownIcon=""@BitIconName.DoubleChevronDown"" NameSelectors=""nameSelectors"" Split />";
    private readonly string example12CsharpCode = @"
public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private static List<Operation> itemStyleClassCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"", Icon = BitIconName.Emoji, Style = ""color:red"" },
    new() { Name = ""Custom B"", Id = ""B"", Icon = BitIconName.Emoji, Class = ""custom-item"" },
    new() { Name = ""Custom C"", Id = ""C"", Icon = BitIconName.Emoji2, Style = ""background:blue"" }
];

private BitMenuButtonNameSelectors<Operation> nameSelectors = new()
{
    Text = { Name = nameof(Operation.Name) },
    Key = { Name = nameof(Operation.Id) },
    IconName = { Name = nameof(Operation.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";
}
