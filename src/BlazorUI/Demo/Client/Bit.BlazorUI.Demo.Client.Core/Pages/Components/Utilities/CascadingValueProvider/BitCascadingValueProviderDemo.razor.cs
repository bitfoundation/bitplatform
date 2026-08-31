namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.CascadingValueProvider;

public partial class BitCascadingValueProviderDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content to which the values should be provided.",
        },
        new()
        {
            Name = "Values",
            Type = "IEnumerable<BitCascadingValue>?",
            DefaultValue = "null",
            Description = "The cascading values to be provided for the children. These values are provided after (so they take precedence over) the ones of the ValueList parameter.",
            LinkType = LinkType.Link,
            Href = "#cascading-value"
        },
        new()
        {
            Name = "ValueList",
            Type = "BitCascadingValueList?",
            DefaultValue = "null",
            Description = "The cascading value list to be provided for the children. These values are provided before (so they can be overridden by) the ones of the Values parameter.",
            LinkType = LinkType.Link,
            Href = "#cascading-value-list"
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "cascading-value",
            Title = "BitCascadingValue",
            Description = "Defines a value that can be cascaded to descendant components.",
            Parameters =
            [
                new()
                {
                    Name = "Value",
                    Type = "object?",
                    DefaultValue = "null",
                    Description = "The value to be provided. Assigning a value that is not compatible with the ValueType throws an ArgumentException.",
                },
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The optional name of the cascading value. An empty or white-space name is treated as no name at all.",
                },
                new()
                {
                    Name = "IsFixed",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "If true, indicates that Value will not change, so consumers are not subscribed for change notifications.",
                },
                new()
                {
                    Name = "Enabled",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Determines whether this cascading value is provided to the children. A disabled value is skipped as if it was never added, so an outer or root level cascading value of the same type or name shows through.",
                },
                new()
                {
                    Name = "ValueType",
                    Type = "Type",
                    DefaultValue = "Value?.GetType()",
                    Description = "The type to use as the TValue of the CascadingValue component. It is read-only and defaults to the runtime type of the value, so it must be provided explicitly for null values, nullable value types, base types and interfaces.",
                },
                new()
                {
                    Name = "From<T>(T value, string? name = null, bool isFixed = false)",
                    Type = "BitCascadingValue",
                    DefaultValue = "",
                    Description = "Creates a cascading value whose ValueType is the static type of T.",
                },
                new()
                {
                    Name = "Fixed<T>(T value, string? name = null)",
                    Type = "BitCascadingValue",
                    DefaultValue = "",
                    Description = "Creates a fixed (IsFixed) cascading value whose ValueType is the static type of T.",
                }
            ]
        },
        new()
        {
            Id = "cascading-value-list",
            Title = "BitCascadingValueList",
            Description = "A helper class to ease the using of a list of the BitCascadingValue.",
            Parameters =
            [
                new()
                {
                    Name = "Add<T>(T value, string? name = null, bool isFixed = false, bool enabled = true)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a typed BitCascadingValue to the list, cascading the value as the static type of T.",
                },
                new()
                {
                    Name = "Add(BitCascadingValue? value)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds an already created BitCascadingValue to the list. A null item is ignored.",
                },
                new()
                {
                    Name = "Add(object? value, Type valueType, string? name = null, bool isFixed = false, bool enabled = true)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a BitCascadingValue with an explicit ValueType to the list, for when the cascaded type is only known at runtime.",
                },
                new()
                {
                    Name = "AddIf<T>(bool condition, T value, string? name = null, bool isFixed = false)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a typed BitCascadingValue to the list only when the given condition is true.",
                },
                new()
                {
                    Name = "AddFixed<T>(T value, string? name = null)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a fixed (IsFixed) typed BitCascadingValue to the list.",
                }
            ]
        }
    ];



    private bool isAuthenticated = true;
    private string currentTheme = "Light";
    private int notificationCount = 2;
    private string userName = "Ava Smith";
    private string userRole = "Product manager";

    private string nextTheme => currentTheme == "Light" ? "Dark" : "Light";

    private IEnumerable<BitCascadingValue> values =>
    [
        (currentTheme, "Theme"),
        (isAuthenticated, "IsAuthenticated"),
        (notificationCount, "NotificationCount"),
        new (new CascadingDemoUser("Saleh Xafan", "CTO"), "NamedUser"),
        new (new CascadingDemoUser(userName, userRole))
    ];



    private readonly string? nullableTheme = null;
    private readonly bool? nullableIsAuthenticated = null;
    private readonly int? nullableNotificationCount = null;
    private readonly CascadingDemoUser? nullableNamedUser = null;
    private readonly CascadingDemoUser? nullableTypedUser = null;



    private readonly IEnumerable<BitCascadingValue> nullCountValues = [BitCascadingValue.From<int?>(null)];



    private readonly IEnumerable<BitCascadingValue> fixedValues =
    [
        BitCascadingValue.Fixed("Light", "Theme"),
        BitCascadingValue.Fixed((3) as int?, "NotificationCount"),
        BitCascadingValue.Fixed(new CascadingDemoUser("Yaser Moradi", "CEO"))
    ];



    private bool provideTheme = true;
    private bool provideUser = true;

    private IEnumerable<BitCascadingValue> conditionalValues =>
    [
        new("Dark", "Theme") { Enabled = provideTheme },
        new(new CascadingDemoUser("Ava Smith", "Product manager")) { Enabled = provideUser }
    ];



    private readonly string example1RazorCode = @"
<BitCascadingValueProvider
    Values=""@([
                 (""light"", ""Theme""),
                 (true, ""IsAuthenticated""),
                 ((2) as int?, ""NotificationCount""),
                 new(new CascadingDemoUser(""Saleh Xafan"", ""CTO""), ""NamedUser""),
                 new(new CascadingDemoUser(""Yaser Moradi"", ""CEO""))
             ])"">
    <!-- Place components with cascading parameters here.
        The demo CascadingValueDemoConsumer's source code is located at https://github.com/bitfoundation/bitplatform/tree/develop/src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Pages/Components/Utilities/CascadingValueProvider/CascadingValueDemoConsumer.razor
        CascadingDemoUser's source code can be found at https://github.com/bitfoundation/bitplatform/tree/develop/src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Pages/Components/Utilities/CascadingValueProvider/CascadingDemoUser.cs -->
    <CascadingValueDemoConsumer />
</BitCascadingValueProvider>";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => currentTheme = nextTheme"">Switch to @nextTheme theme</BitButton>
<BitButton OnClick=""() => notificationCount++"">Add notification (@notificationCount)</BitButton>
<BitToggle @bind-Value=""isAuthenticated"" Text=""Authenticated user"" />
<BitTextField @bind-Value=""userName"" Label=""UserName:"" Immediate DebounceTime=""300"" />
<BitTextField @bind-Value=""userRole"" Label=""UserRole:"" Immediate DebounceTime=""300"" />


<BitCascadingValueProvider Values=""values"">
    <!-- Place components with cascading parameters here.
        The demo CascadingValueDemoConsumer's source code is located at https://github.com/bitfoundation/bitplatform/tree/develop/src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Pages/Components/Utilities/CascadingValueProvider/CascadingValueDemoConsumer.razor
        CascadingDemoUser's source code can be found at https://github.com/bitfoundation/bitplatform/tree/develop/src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Pages/Components/Utilities/CascadingValueProvider/CascadingDemoUser.cs -->
    <CascadingValueDemoConsumer />
</BitCascadingValueProvider>";
    private readonly string example2CsharpCode = @"
private bool isAuthenticated = true;
private string currentTheme = ""Light"";
private int notificationCount = 2;
private string userName = ""Ava Smith"";
private string userRole = ""Product manager"";

private string nextTheme => currentTheme == ""Light"" ? ""Dark"" : ""Light"";

private IEnumerable<BitCascadingValue> values =>
[
    (currentTheme, ""Theme""),
    (isAuthenticated, ""IsAuthenticated""),
    (notificationCount, ""NotificationCount""),
    new (new CascadingDemoUser(""Saleh Xafan"", ""CTO""), ""NamedUser""),
    new (new CascadingDemoUser(userName, userRole))
];";

    private readonly string example3RazorCode = @"
<BitCascadingValueProvider
    ValueList=""@(new()
                {
                    { nullableTheme, ""Theme"" },
                    { nullableIsAuthenticated, ""IsAuthenticated"" },
                    { nullableNotificationCount, ""NotificationCount"" },
                    { nullableNamedUser, ""UserInfo"" },
                    { nullableTypedUser }
                })"">
    <!-- Place components with cascading parameters here.
        The demo CascadingValueDemoConsumer's source code is located at https://github.com/bitfoundation/bitplatform/tree/develop/src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Pages/Components/Utilities/CascadingValueProvider/CascadingValueDemoConsumer.razor
        CascadingDemoUser's source code can be found at https://github.com/bitfoundation/bitplatform/tree/develop/src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Pages/Components/Utilities/CascadingValueProvider/CascadingDemoUser.cs -->
    <CascadingValueDemoConsumer />
</BitCascadingValueProvider>";
    private readonly string example3CsharpCode = @"
private readonly string? nullableTheme = null;
private readonly bool? nullableIsAuthenticated = null;
private readonly int? nullableNotificationCount = null;
private readonly CascadingDemoUser? nullableNamedUser = null;
private readonly CascadingDemoUser? nullableTypedUser = null;";

    private readonly string example4RazorCode = @"
<BitCascadingValueProvider
    Values=""@([
                 (""Light"", ""Theme""),
                 ((7) as int?, ""NotificationCount""),
                 new(new CascadingDemoUser(""Ava Smith"", ""Product manager""))
             ])"">
    <CascadingValueDemoConsumer Title=""Outer provider:"" />

    <br />

    <BitCascadingValueProvider Values=""@([(""Dark"", ""Theme"")])"">
        <CascadingValueDemoConsumer Title=""Inner provider (only Theme is overridden):"" />
    </BitCascadingValueProvider>
</BitCascadingValueProvider>";

    private readonly string example5RazorCode = @"
<BitCascadingValueProvider Values=""@([new(5, typeof(int?))])"">
    <!-- The consumer declares [CascadingParameter] public int? Count { get; set; } -->
    <CascadingValueDemoTypeConsumer Title=""Outer provider cascades 5 as int?: "" />

    <BitCascadingValueProvider Values=""nullCountValues"">
        <CascadingValueDemoTypeConsumer Title=""Inner provider cascades null as the same int?: "" />
    </BitCascadingValueProvider>
</BitCascadingValueProvider>";
    private readonly string example5CsharpCode = @"
private readonly IEnumerable<BitCascadingValue> nullCountValues = [BitCascadingValue.From<int?>(null)];";

    private readonly string example6RazorCode = @"
<BitCascadingValueProvider Values=""fixedValues"">
    <CascadingValueDemoConsumer Title=""Fixed cascading values:"" />
</BitCascadingValueProvider>";
    private readonly string example6CsharpCode = @"
private readonly IEnumerable<BitCascadingValue> fixedValues =
[
    BitCascadingValue.Fixed(""Light"", ""Theme""),
    BitCascadingValue.Fixed((3) as int?, ""NotificationCount""),
    BitCascadingValue.Fixed(new CascadingDemoUser(""Yaser Moradi"", ""CEO""))
];";

    private readonly string example7RazorCode = @"
<BitToggle @bind-Value=""provideTheme"" Text=""Provide the named Theme value"" />
<BitToggle @bind-Value=""provideUser"" Text=""Provide the typed user value"" />

<BitCascadingValueProvider Values=""conditionalValues"">
    <CascadingValueDemoConsumer Title=""Conditional cascading values:"" />
</BitCascadingValueProvider>";
    private readonly string example7CsharpCode = @"
private bool provideTheme = true;
private bool provideUser = true;

private IEnumerable<BitCascadingValue> conditionalValues =>
[
    new(""Dark"", ""Theme"") { Enabled = provideTheme },
    new(new CascadingDemoUser(""Ava Smith"", ""Product manager"")) { Enabled = provideUser }
];";
}
