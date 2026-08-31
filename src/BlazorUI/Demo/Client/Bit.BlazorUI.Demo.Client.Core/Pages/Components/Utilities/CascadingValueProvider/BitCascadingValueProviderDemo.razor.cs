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
                    Description = "The value to be provided. Assigning a value that is not compatible with the ValueType throws an ArgumentException, and assigning a different value raises the Changed event.",
                },
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The optional name of the cascading value. An empty or white-space name is treated as no name at all, and the consumers match it case-insensitively.",
                },
                new()
                {
                    Name = "IsFixed",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "If true, indicates that Value will not change, so consumers are not subscribed for change notifications. Toggling it re-creates the underlying CascadingValue component.",
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
                    Name = "IsValueCreated",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Whether the value is already available. It is only false for a lazily created value whose factory has not run yet.",
                },
                new()
                {
                    Name = "Changed",
                    Type = "event Action<BitCascadingValue>?",
                    DefaultValue = "",
                    Description = "Raised whenever the value changes, which is what lets the hosting BitCascadingValueProvider re-render and push the new value down to the consumers on its own.",
                },
                new()
                {
                    Name = "NotifyChanged()",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Raises the Changed event on demand, which is how a cascaded object that is mutated in place is pushed down to the consumers.",
                },
                new()
                {
                    Name = "From<T>(T value, string? name = null, bool isFixed = false, bool enabled = true)",
                    Type = "BitCascadingValue",
                    DefaultValue = "",
                    Description = "Creates a cascading value whose ValueType is the static type of T.",
                },
                new()
                {
                    Name = "Fixed<T>(T value, string? name = null, bool enabled = true)",
                    Type = "BitCascadingValue",
                    DefaultValue = "",
                    Description = "Creates a fixed (IsFixed) cascading value whose ValueType is the static type of T.",
                },
                new()
                {
                    Name = "Lazy<T>(Func<T> valueFactory, string? name = null, bool isFixed = false, bool enabled = true)",
                    Type = "BitCascadingValue",
                    DefaultValue = "",
                    Description = "Creates a cascading value whose value is produced by the factory the first time it is actually needed, so a disabled or shadowed value is never built. The factory runs at most once.",
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
                    Name = "AddIf(bool condition, BitCascadingValue? value)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds an already created BitCascadingValue to the list only when the given condition is true, which paired with a lazy value keeps the value of a conditional entry from being built at all.",
                },
                new()
                {
                    Name = "AddFixed<T>(T value, string? name = null)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a fixed (IsFixed) typed BitCascadingValue to the list.",
                },
                new()
                {
                    Name = "AddFixed(object? value, Type valueType, string? name = null)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a fixed (IsFixed) BitCascadingValue with an explicit ValueType to the list.",
                },
                new()
                {
                    Name = "AddLazy<T>(Func<T> valueFactory, string? name = null, bool isFixed = false, bool enabled = true)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a typed BitCascadingValue whose value is produced by the factory the first time it is actually needed. The factory runs at most once.",
                }
            ]
        }
    ];



    public BitCascadingValueProviderDemo()
    {
        jobStatusValue = BitCascadingValue.From(jobStatus);
        jobProgressValue = BitCascadingValue.From<int?>(null, "Progress");
        notifyingValues = [jobStatusValue, jobProgressValue];

        lazyTypedUser = BitCascadingValue.Lazy(() => CreateLazyUser("Ava Smith", "Product manager"));
        lazyNamedUser = BitCascadingValue.Lazy(() => CreateLazyUser("Saleh Xafan", "CTO"), "NamedUser", enabled: false);
        lazyValues = [lazyTypedUser, lazyNamedUser];
    }



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



    private bool jobIsRunning;
    private readonly CascadingDemoStatus jobStatus = new();
    private readonly BitCascadingValue jobStatusValue;
    private readonly BitCascadingValue jobProgressValue;
    private readonly IEnumerable<BitCascadingValue> notifyingValues;

    private void RunBackgroundJob()
    {
        if (jobIsRunning) return;

        jobIsRunning = true;

        _ = Task.Run(async () =>
        {
            // The cascaded status object is mutated in place, so there is no assignment to notice.
            jobStatus.Text = "Running";
            jobStatusValue.NotifyChanged();

            for (var i = 1; i <= 5; i++)
            {
                await Task.Delay(500);

                // Assigning the Value raises the Changed event on its own.
                jobProgressValue.Value = i;
            }

            jobStatus.Text = "Done";
            jobStatusValue.NotifyChanged();

            jobIsRunning = false;
        });
    }



    private int lazyUserFactoryCalls;
    private readonly BitCascadingValue lazyTypedUser;
    private readonly BitCascadingValue lazyNamedUser;
    private readonly IEnumerable<BitCascadingValue> lazyValues;

    private bool provideLazyNamedUser
    {
        get => lazyNamedUser.Enabled;
        set => lazyNamedUser.Enabled = value;
    }

    private CascadingDemoUser CreateLazyUser(string name, string role)
    {
        lazyUserFactoryCalls++;

        return new CascadingDemoUser(name, role);
    }



    private readonly string example1RazorCode = @"
<BitCascadingValueProvider
    Values=""@([
                 (""Light"", ""Theme""),
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

    private readonly string example8RazorCode = @"
<BitButton OnClick=""RunBackgroundJob"">Run a background job</BitButton>

<BitCascadingValueProvider Values=""notifyingValues"">
    <!-- The consumer declares [CascadingParameter] CascadingDemoStatus? Status
         and [CascadingParameter(Name = ""Progress"")] int? Progress -->
    <CascadingValueDemoStatusConsumer Title=""Self-refreshing cascading values:"" />
</BitCascadingValueProvider>";
    private readonly string example8CsharpCode = @"
private bool jobIsRunning;
private readonly CascadingDemoStatus jobStatus = new();
private readonly BitCascadingValue jobStatusValue;
private readonly BitCascadingValue jobProgressValue;
private readonly IEnumerable<BitCascadingValue> notifyingValues;

public MyPage()
{
    jobStatusValue = BitCascadingValue.From(jobStatus);
    jobProgressValue = BitCascadingValue.From<int?>(null, ""Progress"");
    notifyingValues = [jobStatusValue, jobProgressValue];
}

private void RunBackgroundJob()
{
    if (jobIsRunning) return;

    jobIsRunning = true;

    _ = Task.Run(async () =>
    {
        // The cascaded status object is mutated in place, so there is no assignment to notice.
        jobStatus.Text = ""Running"";
        jobStatusValue.NotifyChanged();

        for (var i = 1; i <= 5; i++)
        {
            await Task.Delay(500);

            // Assigning the Value raises the Changed event on its own.
            jobProgressValue.Value = i;
        }

        jobStatus.Text = ""Done"";
        jobStatusValue.NotifyChanged();

        jobIsRunning = false;
    });
}";

    private readonly string example9RazorCode = @"
<BitToggle @bind-Value=""provideLazyNamedUser"" Text=""Provide the named user as well"" />

<BitCascadingValueProvider Values=""lazyValues"">
    <CascadingValueDemoConsumer Title=""Lazy cascading values:"" />
    <div>Factory invocations so far: <b>@lazyUserFactoryCalls</b></div>
</BitCascadingValueProvider>";
    private readonly string example9CsharpCode = @"
private int lazyUserFactoryCalls;
private readonly BitCascadingValue lazyTypedUser;
private readonly BitCascadingValue lazyNamedUser;
private readonly IEnumerable<BitCascadingValue> lazyValues;

public MyPage()
{
    lazyTypedUser = BitCascadingValue.Lazy(() => CreateLazyUser(""Ava Smith"", ""Product manager""));
    lazyNamedUser = BitCascadingValue.Lazy(() => CreateLazyUser(""Saleh Xafan"", ""CTO""), ""NamedUser"", enabled: false);
    lazyValues = [lazyTypedUser, lazyNamedUser];
}

private bool provideLazyNamedUser
{
    get => lazyNamedUser.Enabled;
    set => lazyNamedUser.Enabled = value;
}

private CascadingDemoUser CreateLazyUser(string name, string role)
{
    lazyUserFactoryCalls++;

    return new CascadingDemoUser(name, role);
}";
}
