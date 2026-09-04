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
                    Description = "The value to be provided. Assigning a value that is not compatible with the ValueType throws an ArgumentException, and assigning a different value raises the Changed event. A lazy factory runs the first time it is read, a computed one on every read.",
                },
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The optional name of the cascading value. An empty or white-space name is treated as no name at all, and the consumers match it case-insensitively. Renaming a live value re-creates the underlying CascadingValue component so that the consumers are matched again under the new name.",
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
                    Name = "AutoNotify",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Watches the cascaded value itself, so an INotifyPropertyChanged or INotifyCollectionChanged value raises Changed on its own. The subscription is only held while a provider is listening, so the cascaded object never keeps this value alive.",
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
                    Name = "IsComputed",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the value is produced by a factory that runs on every read rather than being stored once, which is what the Computed factory methods create.",
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
                    Name = "ChangedAsync",
                    Type = "event Func<BitCascadingValue, Task>?",
                    DefaultValue = "",
                    Description = "The awaitable counterpart of Changed, which is what the provider subscribes to and what makes NotifyChangedAsync complete only once the re-render is done.",
                },
                new()
                {
                    Name = "NotifyChanged()",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Raises the Changed and ChangedAsync events on demand, which is how a cascaded object that is mutated in place is pushed down to the consumers.",
                },
                new()
                {
                    Name = "NotifyChangedAsync()",
                    Type = "Task",
                    DefaultValue = "",
                    Description = "The awaitable form of NotifyChanged, whose task completes once every listening provider has re-rendered, like CascadingValueSource.NotifyChangedAsync does.",
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
                },
                new()
                {
                    Name = "Computed<T>(Func<T> valueFactory, string? name = null, bool isFixed = false)",
                    Type = "BitCascadingValue",
                    DefaultValue = "",
                    Description = "Creates a cascading value that is re-read from the factory every time it is provided, so one long lived value keeps tracking the state it is derived from.",
                },
                new()
                {
                    Name = "Observed<T>(T value, string? name = null, bool enabled = true)",
                    Type = "BitCascadingValue",
                    DefaultValue = "",
                    Description = "Creates a cascading value with AutoNotify turned on, so a value reporting its own mutations refreshes the consumers without any call to NotifyChanged.",
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
                    Name = "AddIf<T>(bool condition, T value, string? name = null, bool isFixed = false, bool enabled = true)",
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
                    Description = "Adds a typed BitCascadingValue whose value is produced by the factory the first time it is actually needed. The factory runs at most once. An overload taking an explicit ValueType is available as well.",
                },
                new()
                {
                    Name = "AddComputed<T>(Func<T> valueFactory, string? name = null, bool isFixed = false)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a typed BitCascadingValue that is re-read from the factory on every render, so a list built once keeps tracking the state its values are derived from. An overload taking an explicit ValueType is available as well.",
                },
                new()
                {
                    Name = "AddObserved<T>(T value, string? name = null, bool enabled = true)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a typed BitCascadingValue that watches the value itself, so an INotifyPropertyChanged or INotifyCollectionChanged object refreshes the consumers on its own.",
                },
                new()
                {
                    Name = "Find<T>(string? name = null)",
                    Type = "BitCascadingValue?",
                    DefaultValue = "",
                    Description = "Finds the entry that the given type and name resolve to, which is the last one matching both, since that is the one shadowing all the others. An overload taking an explicit ValueType is available as well.",
                },
                new()
                {
                    Name = "Contains<T>(string? name = null)",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Whether the list holds an entry of the static type of T carrying the given name, regardless of whether it is enabled.",
                },
                new()
                {
                    Name = "Remove<T>(string? name = null)",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Removes every entry of the static type of T carrying the given name, and reports whether anything was removed. An overload taking an explicit ValueType is available as well.",
                },
                new()
                {
                    Name = "Set<T>(T value, string? name = null, bool isFixed = false, bool enabled = true)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Replaces every entry of the static type of T carrying the given name with a new one, or adds it when the list has none, so the list ends up with exactly one entry per type and name.",
                }
            ]
        }
    ];



    public BitCascadingValueProviderDemo()
    {
        jobStatusValue = BitCascadingValue.From(jobStatus);
        jobProgressValue = BitCascadingValue.From<int?>(null, "Progress");
        notifyingValues = [jobStatusValue, jobProgressValue];

        observableValues = [BitCascadingValue.Observed(observableStatus)];

        lazyTypedUser = BitCascadingValue.Lazy(() => CreateLazyUser("Ava Smith", "Product manager"));
        lazyNamedUser = BitCascadingValue.Lazy(() => CreateLazyUser("Saleh Xafan", "CTO"), "NamedUser", enabled: false);
        lazyValues = [lazyTypedUser, lazyNamedUser];

        computedValues =
        [
            BitCascadingValue.Computed(() => computedClicks % 2 == 0 ? "Light" : "Dark", "Theme"),
            BitCascadingValue.Computed<int?>(() => computedClicks, "NotificationCount")
        ];
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
            try
            {
                // The cascaded status object is mutated in place, so there is no assignment to notice.
                jobStatus.Text = "Running";
                await jobStatusValue.NotifyChangedAsync();

                for (var i = 1; i <= 5; i++)
                {
                    await Task.Delay(500);

                    // Assigning the Value raises the Changed event on its own.
                    jobProgressValue.Value = i;
                }

                jobStatus.Text = "Done";
                await jobStatusValue.NotifyChangedAsync();
            }
            finally
            {
                jobIsRunning = false;
            }
        });
    }



    private bool observableJobIsRunning;
    private readonly CascadingDemoObservableStatus observableStatus = new();
    private readonly IEnumerable<BitCascadingValue> observableValues;

    private void RunObservableJob()
    {
        if (observableJobIsRunning) return;

        observableJobIsRunning = true;

        _ = Task.Run(async () =>
        {
            try
            {
                // Nothing here notifies anything: the status object reports its own changes.
                observableStatus.Text = "Running";

                for (var i = 1; i <= 5; i++)
                {
                    await Task.Delay(500);

                    observableStatus.Count = i;
                }

                observableStatus.Text = "Done";
            }
            finally
            {
                observableJobIsRunning = false;
            }
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



    private int computedClicks;
    private readonly IEnumerable<BitCascadingValue> computedValues;



    // The source of every file the examples reach for, each of them a tab of its own beside the
    // sample that uses it: the demo consumers, where the [CascadingParameter] properties a provider
    // feeds are declared, and the types being cascaded. Verbatim copies of the files next to this
    // one, which is what makes a section copyable rather than a link to go and read.
    private const string consumerRazorCode = """
        <div>
            <div style="font-weight:bold;font-size:20px">
                @(Title ?? "Child component with cascading parameters:")
            </div>

            <br />

            <div>
                <div>
                    <span style="font-weight:bold">Theme: </span>
                    <span>@(Theme ?? "null")</span>
                </div>

                <div>
                    <span style="font-weight:bold">Notifications: </span>
                    <span>@(NotificationCount?.ToString() ?? "null")</span>
                </div>

                <div>
                    <span style="font-weight:bold">Authenticated: </span>
                    <span>@(IsAuthenticated?.ToString() ?? "null")</span>
                </div>

                <div>
                    <span style="font-weight:bold">User (named parameter): </span>
                    <span>@(FormatUser(NamedUser) ?? "null")</span>
                </div>

                <div>
                    <span style="font-weight:bold">User (typed parameter): </span>
                    <span>@(FormatUser(TypedUser) ?? "null")</span>
                </div>
            </div>
        </div>

        @code {
            [Parameter] public string? Title { get; set; }

            [CascadingParameter(Name = "Theme")]
            public string? Theme { get; set; }

            [CascadingParameter(Name = "NotificationCount")]
            public int? NotificationCount { get; set; }

            [CascadingParameter(Name = "IsAuthenticated")]
            public bool? IsAuthenticated { get; set; }

            [CascadingParameter(Name = "NamedUser")]
            public CascadingDemoUser? NamedUser { get; set; }

            [CascadingParameter]
            public CascadingDemoUser? TypedUser { get; set; }

            private static string? FormatUser(CascadingDemoUser? user) => user is null ? null : $"{user.Name} [{user.Role}]";
        }
        """;

    private const string typeConsumerRazorCode = """
        <div>
            <span style="font-weight:bold">@Title</span>
            <span>@(Count?.ToString() ?? "null")</span>
        </div>

        @code {
            [Parameter] public string? Title { get; set; }

            [CascadingParameter]
            public int? Count { get; set; }
        }
        """;

    private const string statusConsumerRazorCode = """
        <div>
            <div style="font-weight:bold;font-size:20px">
                @(Title ?? "Child component with cascading parameters:")
            </div>

            <br />

            <div>
                <div>
                    <span style="font-weight:bold">Status: </span>
                    <span>@(Status?.Text ?? "null")</span>
                </div>

                <div>
                    <span style="font-weight:bold">Progress: </span>
                    <span>@(Progress?.ToString() ?? "null")</span>
                </div>
            </div>
        </div>

        @code {
            [Parameter] public string? Title { get; set; }

            [CascadingParameter]
            public CascadingDemoStatus? Status { get; set; }

            [CascadingParameter(Name = "Progress")]
            public int? Progress { get; set; }
        }
        """;

    private const string observableConsumerRazorCode = """
        <div>
            <div style="font-weight:bold;font-size:20px">
                @(Title ?? "Child component with cascading parameters:")
            </div>

            <br />

            <div>
                <div>
                    <span style="font-weight:bold">Status: </span>
                    <span>@(Status?.Text ?? "null")</span>
                </div>

                <div>
                    <span style="font-weight:bold">Count: </span>
                    <span>@(Status?.Count.ToString() ?? "null")</span>
                </div>
            </div>
        </div>

        @code {
            [Parameter] public string? Title { get; set; }

            [CascadingParameter]
            public CascadingDemoObservableStatus? Status { get; set; }
        }
        """;

    private const string userCsharpCode = """
        public sealed record CascadingDemoUser(string Name, string Role);
        """;

    private const string statusCsharpCode = """
        /// <summary>
        /// A mutable state holder, cascaded as a single instance that is updated in place, which is the case
        /// that BitCascadingValue.NotifyChanged exists for.
        /// </summary>
        public sealed class CascadingDemoStatus
        {
            public string Text { get; set; } = "Idle";
        }
        """;

    private const string observableStatusCsharpCode = """
        using System.ComponentModel;
        using System.Runtime.CompilerServices;

        /// <summary>
        /// A state holder that reports its own mutations, which is what BitCascadingValue.Observed watches so that
        /// the consumers refresh without a single call to NotifyChanged.
        /// </summary>
        public sealed class CascadingDemoObservableStatus : INotifyPropertyChanged
        {
            private int _count;
            private string _text = "Idle";

            public event PropertyChangedEventHandler? PropertyChanged;

            public string Text
            {
                get => _text;
                set
                {
                    if (_text == value) return;

                    _text = value;

                    OnPropertyChanged();
                }
            }

            public int Count
            {
                get => _count;
                set
                {
                    if (_count == value) return;

                    _count = value;

                    OnPropertyChanged();
                }
            }

            private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        """;

    // One DemoCodeFile per file rather than one per example that shows it: the arrays below are then
    // built out of the same references, which is all a tab needs and all the panel compares.
    private static readonly DemoCodeFile consumerFile = new("CascadingValueDemoConsumer.razor", consumerRazorCode);
    private static readonly DemoCodeFile typeConsumerFile = new("CascadingValueDemoTypeConsumer.razor", typeConsumerRazorCode);
    private static readonly DemoCodeFile statusConsumerFile = new("CascadingValueDemoStatusConsumer.razor", statusConsumerRazorCode);
    private static readonly DemoCodeFile observableConsumerFile = new("CascadingValueDemoObservableConsumer.razor", observableConsumerRazorCode);
    private static readonly DemoCodeFile userFile = new("CascadingDemoUser.cs", userCsharpCode);
    private static readonly DemoCodeFile statusFile = new("CascadingDemoStatus.cs", statusCsharpCode);
    private static readonly DemoCodeFile observableStatusFile = new("CascadingDemoObservableStatus.cs", observableStatusCsharpCode);



    private readonly string example1RazorCode = @"
<BitCascadingValueProvider
    Values=""@([
                 (""Light"", ""Theme""),
                 (true, ""IsAuthenticated""),
                 ((2) as int?, ""NotificationCount""),
                 new(new CascadingDemoUser(""Saleh Xafan"", ""CTO""), ""NamedUser""),
                 new(new CascadingDemoUser(""Yaser Moradi"", ""CEO""))
             ])"">
    <CascadingValueDemoConsumer />
</BitCascadingValueProvider>";

    private readonly DemoCodeFile[] example1CodeFiles = [consumerFile, userFile];

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => currentTheme = nextTheme"">Switch to @nextTheme theme</BitButton>
<BitButton OnClick=""() => notificationCount++"">Add notification (@notificationCount)</BitButton>
<BitToggle @bind-Value=""isAuthenticated"" Text=""Authenticated user"" />
<BitTextField @bind-Value=""userName"" Label=""UserName:"" Immediate DebounceTime=""300"" />
<BitTextField @bind-Value=""userRole"" Label=""UserRole:"" Immediate DebounceTime=""300"" />


<BitCascadingValueProvider Values=""values"">
    <CascadingValueDemoConsumer Title=""Changing cascading values:"" />
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

    private readonly DemoCodeFile[] example2CodeFiles = [consumerFile, userFile];

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
    <CascadingValueDemoConsumer Title=""ValueList cascading values:"" />
</BitCascadingValueProvider>";
    private readonly string example3CsharpCode = @"
private readonly string? nullableTheme = null;
private readonly bool? nullableIsAuthenticated = null;
private readonly int? nullableNotificationCount = null;
private readonly CascadingDemoUser? nullableNamedUser = null;
private readonly CascadingDemoUser? nullableTypedUser = null;";

    private readonly DemoCodeFile[] example3CodeFiles = [consumerFile, userFile];

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

    private readonly DemoCodeFile[] example4CodeFiles = [consumerFile, userFile];

    private readonly string example5RazorCode = @"
<BitCascadingValueProvider Values=""@([new(5, typeof(int?))])"">
    <CascadingValueDemoTypeConsumer Title=""Outer provider cascades 5 as int?: "" />

    <BitCascadingValueProvider Values=""nullCountValues"">
        <CascadingValueDemoTypeConsumer Title=""Inner provider cascades null as the same int?: "" />
    </BitCascadingValueProvider>
</BitCascadingValueProvider>";
    private readonly string example5CsharpCode = @"
private readonly IEnumerable<BitCascadingValue> nullCountValues = [BitCascadingValue.From<int?>(null)];";

    private readonly DemoCodeFile[] example5CodeFiles = [typeConsumerFile];

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

    private readonly DemoCodeFile[] example6CodeFiles = [consumerFile, userFile];

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

    private readonly DemoCodeFile[] example7CodeFiles = [consumerFile, userFile];

    private readonly string example8RazorCode = @"
<BitButton OnClick=""RunBackgroundJob"">Run a background job</BitButton>

<BitCascadingValueProvider Values=""notifyingValues"">
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
        try
        {
            // The cascaded status object is mutated in place, so there is no assignment to notice.
            jobStatus.Text = ""Running"";
            await jobStatusValue.NotifyChangedAsync();

            for (var i = 1; i <= 5; i++)
            {
                await Task.Delay(500);

                // Assigning the Value raises the Changed event on its own.
                jobProgressValue.Value = i;
            }

            jobStatus.Text = ""Done"";
            await jobStatusValue.NotifyChangedAsync();
        }
        finally
        {
            jobIsRunning = false;
        }
    });
}";

    private readonly DemoCodeFile[] example8CodeFiles = [statusConsumerFile, statusFile];

    private readonly string example9RazorCode = @"
<BitButton OnClick=""RunObservableJob"">Run a background job</BitButton>

<BitCascadingValueProvider Values=""observableValues"">
    <CascadingValueDemoObservableConsumer Title=""Self-watching cascading values:"" />
</BitCascadingValueProvider>";
    private readonly string example9CsharpCode = @"
private bool observableJobIsRunning;
private readonly CascadingDemoObservableStatus observableStatus = new();
private readonly IEnumerable<BitCascadingValue> observableValues;

public MyPage()
{
    observableValues = [BitCascadingValue.Observed(observableStatus)];
}

private void RunObservableJob()
{
    if (observableJobIsRunning) return;

    observableJobIsRunning = true;

    _ = Task.Run(async () =>
    {
        try
        {
            // Nothing here notifies anything: the status object reports its own changes.
            observableStatus.Text = ""Running"";

            for (var i = 1; i <= 5; i++)
            {
                await Task.Delay(500);

                observableStatus.Count = i;
            }

            observableStatus.Text = ""Done"";
        }
        finally
        {
            observableJobIsRunning = false;
        }
    });
}";

    private readonly DemoCodeFile[] example9CodeFiles = [observableConsumerFile, observableStatusFile];

    private readonly string example10RazorCode = @"
<BitToggle @bind-Value=""provideLazyNamedUser"" Text=""Provide the named user as well"" />

<BitCascadingValueProvider Values=""lazyValues"">
    <CascadingValueDemoConsumer Title=""Lazy cascading values:"" />
    <div>Factory invocations so far: <b>@lazyUserFactoryCalls</b></div>
</BitCascadingValueProvider>";
    private readonly string example10CsharpCode = @"
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

    private readonly DemoCodeFile[] example10CodeFiles = [consumerFile, userFile];

    private readonly string example11RazorCode = @"
<BitButton OnClick=""() => computedClicks++"">Click me (@computedClicks)</BitButton>

<BitCascadingValueProvider Values=""computedValues"">
    <CascadingValueDemoConsumer Title=""Computed cascading values:"" />
</BitCascadingValueProvider>";
    private readonly string example11CsharpCode = @"
private int computedClicks;
private readonly IEnumerable<BitCascadingValue> computedValues;

public MyPage()
{
    computedValues =
    [
        BitCascadingValue.Computed(() => computedClicks % 2 == 0 ? ""Light"" : ""Dark"", ""Theme""),
        BitCascadingValue.Computed<int?>(() => computedClicks, ""NotificationCount"")
    ];
}";
    private readonly DemoCodeFile[] example11CodeFiles = [consumerFile, userFile];

}
