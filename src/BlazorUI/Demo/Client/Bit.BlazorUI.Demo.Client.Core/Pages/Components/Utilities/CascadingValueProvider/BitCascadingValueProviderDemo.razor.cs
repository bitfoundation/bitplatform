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
            Description = "One value to cascade: what is cascaded, as which type, under which name, and whether it is fixed or provided at all. Bare values and (value, name) tuples of the common primitive, date, string and BitDir types convert to it implicitly.",
            Parameters =
            [
                new()
                {
                    Name = "BitCascadingValue(object? value, string? name, bool isFixed, Type? valueType = null, bool enabled = true)",
                    Type = "constructor",
                    DefaultValue = "",
                    Description = "Creates a cascading value. Shorter overloads take (value, name), (value, isFixed), (value, valueType) and (value, name, valueType). The valueType is required when the value is null.",
                },
                new()
                {
                    Name = "Value",
                    Type = "object?",
                    DefaultValue = "null",
                    Description = "The value to be provided. Assigning a value not assignable to ValueType throws an ArgumentException; assigning a different value raises Changed. A lazy factory runs on the first read, a computed one on every read.",
                },
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The optional name of the cascading value, matched case-insensitively; an empty or white-space name means no name. Renaming a live value re-creates its CascadingValue so the consumers are matched again under the new name.",
                },
                new()
                {
                    Name = "IsFixed",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Marks a value that never changes, so its consumers are not subscribed for change notifications. Toggling it re-creates the underlying CascadingValue.",
                },
                new()
                {
                    Name = "Enabled",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Whether the value is provided at all. A disabled value is skipped as if it had never been added, so an outer or root-level value of the same type and name shows through.",
                },
                new()
                {
                    Name = "AutoNotify",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Watches the cascaded object itself, so an INotifyPropertyChanged or INotifyCollectionChanged value raises Changed on its own. The watch is only held while a provider is listening.",
                },
                new()
                {
                    Name = "ValueType",
                    Type = "Type",
                    DefaultValue = "Value?.GetType()",
                    Description = "The TValue of the underlying CascadingValue, which decides the cascading parameters the value reaches. Read-only; defaults to the runtime type of the value, so pass it explicitly for null values, nullable value types, base types and interfaces.",
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
                    Description = "Whether the value is re-read from a factory on every read, as the Computed factories create it.",
                },
                new()
                {
                    Name = "Changed",
                    Type = "event Action<BitCascadingValue>?",
                    DefaultValue = "",
                    Description = "Raised whenever the value changes; the hosting provider listens to it and re-renders on its own.",
                },
                new()
                {
                    Name = "ChangedAsync",
                    Type = "event Func<BitCascadingValue, Task>?",
                    DefaultValue = "",
                    Description = "The awaitable counterpart of Changed, which the provider subscribes to so NotifyChangedAsync completes only once it has re-rendered.",
                },
                new()
                {
                    Name = "NotifyChanged()",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Raises Changed and ChangedAsync on demand, which pushes an object that was mutated in place down to the consumers.",
                },
                new()
                {
                    Name = "NotifyChangedAsync()",
                    Type = "Task",
                    DefaultValue = "",
                    Description = "The awaitable form of NotifyChanged; completes once every listening provider has re-rendered, like CascadingValueSource.NotifyChangedAsync.",
                },
                new()
                {
                    Name = "NotifyChangedAsync(object? newValue)",
                    Type = "Task",
                    DefaultValue = "",
                    Description = "Assigns Value and completes once every listening provider has re-rendered with it, like CascadingValueSource.NotifyChangedAsync(newValue). Notifies even when the value is unchanged.",
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
                    Description = "Creates a value whose factory runs once, the first time it is provided, so a disabled or shadowed value is never built. A failing factory is retried on the next read. An overload taking an explicit ValueType is available as well.",
                },
                new()
                {
                    Name = "Computed<T>(Func<T> valueFactory, string? name = null, bool isFixed = false, bool enabled = true)",
                    Type = "BitCascadingValue",
                    DefaultValue = "",
                    Description = "Creates a value whose factory runs every time it is provided, so one long-lived value keeps tracking the state it is derived from. An overload taking an explicit ValueType is available as well.",
                },
                new()
                {
                    Name = "Observed<T>(T value, string? name = null, bool enabled = true)",
                    Type = "BitCascadingValue",
                    DefaultValue = "",
                    Description = "Creates a value with AutoNotify turned on, so an object reporting its own mutations refreshes the consumers without any call to NotifyChanged.",
                }
            ]
        },
        new()
        {
            Id = "cascading-value-list",
            Title = "BitCascadingValueList",
            Description = "A List<BitCascadingValue> with typed helpers for building and revising a set of cascading values; its collection initializer takes { value, name } pairs.",
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
                    Description = "Adds an already created BitCascadingValue only when the condition is true; paired with a lazy value, the value of a skipped entry is never built.",
                },
                new()
                {
                    Name = "AddFixed<T>(T value, string? name = null, bool enabled = true)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a fixed (IsFixed) typed BitCascadingValue to the list.",
                },
                new()
                {
                    Name = "AddFixed(object? value, Type valueType, string? name = null, bool enabled = true)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a fixed (IsFixed) BitCascadingValue with an explicit ValueType to the list.",
                },
                new()
                {
                    Name = "AddLazy<T>(Func<T> valueFactory, string? name = null, bool isFixed = false, bool enabled = true)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a lazy value, whose factory runs once, the first time it is provided. An overload taking an explicit ValueType is available as well.",
                },
                new()
                {
                    Name = "AddComputed<T>(Func<T> valueFactory, string? name = null, bool isFixed = false, bool enabled = true)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a computed value, whose factory runs every time it is provided. An overload taking an explicit ValueType is available as well.",
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
                    Description = "Finds the entry the type and name resolve to: the last one matching both, since it shadows the others. An overload taking an explicit ValueType is available as well.",
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
                    Description = "Replaces every entry of the static type of T and the given name with one new entry, in the place of the first, or appends it when there is none.",
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
        factoryValues =
        [
            lazyTypedUser,
            lazyNamedUser,
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



    private int listNotificationCount;
    private readonly BitCascadingValueList listValues = new()
    {
        { "Light", "Theme" },
        { (int?)null, "NotificationCount" },
        { (bool?)null, "IsAuthenticated" },
        { new CascadingDemoUser("Ava Smith", "Product manager") }
    };

    private void SetListNotificationCount() => listValues.Set<int?>(++listNotificationCount, "NotificationCount");

    private void ToggleListUser()
    {
        if (listValues.Contains<CascadingDemoUser>())
        {
            listValues.Remove<CascadingDemoUser>();
        }
        else
        {
            listValues.Add(new CascadingDemoUser("Ava Smith", "Product manager"));
        }
    }



    private readonly IEnumerable<BitCascadingValue> nullCountValues = [BitCascadingValue.From<int?>(null)];



    private bool provideTheme = true;
    private bool provideUser = true;

    private IEnumerable<BitCascadingValue> flagValues =>
    [
        BitCascadingValue.Fixed("Dark", "Theme", enabled: provideTheme),
        BitCascadingValue.Fixed((3) as int?, "NotificationCount"),
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

                    // Assigns the Value and waits until the consumers have rendered it.
                    await jobProgressValue.NotifyChangedAsync(i);
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



    private int computedClicks;
    private int lazyUserFactoryCalls;
    private readonly BitCascadingValue lazyTypedUser;
    private readonly BitCascadingValue lazyNamedUser;
    private readonly IEnumerable<BitCascadingValue> factoryValues;

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
<BitButton OnClick=""SetListNotificationCount"">Set NotificationCount (@listNotificationCount)</BitButton>
<BitButton OnClick=""ToggleListUser"">@(listValues.Contains<CascadingDemoUser>() ? ""Remove"" : ""Add"") the typed user</BitButton>

<BitCascadingValueProvider ValueList=""listValues"" Values=""@([(""Dark"", ""Theme"")])"">
    <CascadingValueDemoConsumer Title=""ValueList defaults, Theme overridden by Values:"" />
</BitCascadingValueProvider>";
    private readonly string example3CsharpCode = @"
private int listNotificationCount;
private readonly BitCascadingValueList listValues = new()
{
    { ""Light"", ""Theme"" },
    { (int?)null, ""NotificationCount"" },
    { (bool?)null, ""IsAuthenticated"" },
    { new CascadingDemoUser(""Ava Smith"", ""Product manager"") }
};

private void SetListNotificationCount() => listValues.Set<int?>(++listNotificationCount, ""NotificationCount"");

private void ToggleListUser()
{
    if (listValues.Contains<CascadingDemoUser>())
    {
        listValues.Remove<CascadingDemoUser>();
    }
    else
    {
        listValues.Add(new CascadingDemoUser(""Ava Smith"", ""Product manager""));
    }
}";

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
<BitToggle @bind-Value=""provideTheme"" Text=""Provide the Theme value"" />
<BitToggle @bind-Value=""provideUser"" Text=""Provide the typed user value"" />

<BitCascadingValueProvider Values=""flagValues"">
    <CascadingValueDemoConsumer Title=""Fixed and conditional cascading values:"" />
</BitCascadingValueProvider>";
    private readonly string example6CsharpCode = @"
private bool provideTheme = true;
private bool provideUser = true;

private IEnumerable<BitCascadingValue> flagValues =>
[
    BitCascadingValue.Fixed(""Dark"", ""Theme"", enabled: provideTheme),
    BitCascadingValue.Fixed((3) as int?, ""NotificationCount""),
    new(new CascadingDemoUser(""Ava Smith"", ""Product manager"")) { Enabled = provideUser }
];";

    private readonly DemoCodeFile[] example6CodeFiles = [consumerFile, userFile];

    private readonly string example7RazorCode = @"
<BitButton OnClick=""RunBackgroundJob"">Run a background job</BitButton>

<BitCascadingValueProvider Values=""notifyingValues"">
    <CascadingValueDemoStatusConsumer Title=""Self-refreshing cascading values:"" />
</BitCascadingValueProvider>";
    private readonly string example7CsharpCode = @"
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

                // Assigns the Value and waits until the consumers have rendered it.
                await jobProgressValue.NotifyChangedAsync(i);
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

    private readonly DemoCodeFile[] example7CodeFiles = [statusConsumerFile, statusFile];

    private readonly string example8RazorCode = @"
<BitButton OnClick=""RunObservableJob"">Run a background job</BitButton>

<BitCascadingValueProvider Values=""observableValues"">
    <CascadingValueDemoObservableConsumer Title=""Self-watching cascading values:"" />
</BitCascadingValueProvider>";
    private readonly string example8CsharpCode = @"
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

    private readonly DemoCodeFile[] example8CodeFiles = [observableConsumerFile, observableStatusFile];

    private readonly string example9RazorCode = @"
<BitToggle @bind-Value=""provideLazyNamedUser"" Text=""Provide the lazy named user"" />
<BitButton OnClick=""() => computedClicks++"">Click me (@computedClicks)</BitButton>

<BitCascadingValueProvider Values=""factoryValues"">
    <CascadingValueDemoConsumer Title=""Lazy and computed cascading values:"" />
    <div>Lazy factory runs so far: <b>@lazyUserFactoryCalls</b></div>
</BitCascadingValueProvider>";
    private readonly string example9CsharpCode = @"
private int computedClicks;
private int lazyUserFactoryCalls;
private readonly BitCascadingValue lazyTypedUser;
private readonly BitCascadingValue lazyNamedUser;
private readonly IEnumerable<BitCascadingValue> factoryValues;

public MyPage()
{
    lazyTypedUser = BitCascadingValue.Lazy(() => CreateLazyUser(""Ava Smith"", ""Product manager""));
    lazyNamedUser = BitCascadingValue.Lazy(() => CreateLazyUser(""Saleh Xafan"", ""CTO""), ""NamedUser"", enabled: false);

    factoryValues =
    [
        lazyTypedUser,
        lazyNamedUser,
        BitCascadingValue.Computed(() => computedClicks % 2 == 0 ? ""Light"" : ""Dark"", ""Theme""),
        BitCascadingValue.Computed<int?>(() => computedClicks, ""NotificationCount"")
    ];
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

    private readonly DemoCodeFile[] example9CodeFiles = [consumerFile, userFile];
}