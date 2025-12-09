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
            Description = "The cascading values to be provided for the children.",
            LinkType = LinkType.Link,
            Href = "#cascading-value"
        },
        new()
        {
            Name = "ValueList",
            Type = "BitCascadingValueList?",
            DefaultValue = "null",
            Description = "The cascading value list to be provided for the children.",
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
                    Description = "The value to be provided.",
                },
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The optional name of the cascading value.",
                },
                new()
                {
                    Name = "IsFixed",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "If true, indicates that Value will not change.",
                },
                new()
                {
                    Name = "ValueType",
                    Type = "Type",
                    DefaultValue = "Value?.GetType()",
                    Description = "The type to use as the TValue of the CascadingValue component.",
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
                    Name = "Add<T>(T value, string? name = null, bool isFixed = false)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a typed BitCascadingValue to the list.",
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



    private readonly string example1RazorCode = @"
<BitCascadingValueProvider 
    Values=""@([
                 (""light"", ""Theme""),
                 (true, ""IsAuthenticated""),
                 ((2) as int?, ""NotificationCount""),
                 new(new CascadingDemoUser(""Saleh Xafan"", ""CTO""), ""NamedUser""),
                 new(new CascadingDemoUser(""Yaser Moradi"", ""CEO""))
             ])"">
    <CascadingValueConsumer />
</BitCascadingValueProvider>";
    private readonly string example1CsharpCode = @"
public sealed class CascadingValueConsumer : ComponentBase
{
    [Parameter] public string? Title { get; set; }

    [CascadingParameter(Name = ""Theme"")]
    public string? Theme { get; set; }

    [CascadingParameter(Name = ""NotificationCount"")]
    public int? NotificationCount { get; set; }

    [CascadingParameter(Name = ""IsAuthenticated"")]
    public bool? IsAuthenticated { get; set; }

    [CascadingParameter(Name = ""NamedUser"")]
    public CascadingDemoUser? NamedUser { get; set; }

    [CascadingParameter]
    public CascadingDemoUser? TypedUser { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;

        builder.OpenElement(seq++, ""div"");

        builder.OpenElement(seq++, ""div"");
        builder.AddAttribute(seq++, ""style"", ""font-weight:bold;font-size:20px"");
        builder.AddContent(seq++, Title ?? ""Child component with cascading parameters:"");
        builder.CloseElement();

        builder.AddMarkupContent(seq++, ""<br />"");

        builder.OpenElement(seq++, ""div"");

        AddValueRow(ref seq, builder, ""Theme: "", Theme ?? ""null"");

        AddValueRow(ref seq, builder, ""Notifications: "", NotificationCount?.ToString() ?? ""null"");

        AddValueRow(ref seq, builder, ""Authenticated: "", IsAuthenticated?.ToString() ?? ""null"");

        AddValueRow(ref seq, builder, ""User (named parameter): "", FormatUser(NamedUser) ?? ""null"");
        AddValueRow(ref seq, builder, ""User (typed parameter): "", FormatUser(TypedUser) ?? ""null"");

        builder.CloseElement();

        builder.CloseElement();
    }

    private static void AddValueRow(ref int seq, RenderTreeBuilder builder, string caption, string? value)
    {
        if (value is null) return;

        builder.OpenElement(seq++, ""div"");

        builder.OpenElement(seq++, ""span"");
        builder.AddAttribute(seq++, ""style"", ""font-weight:bold"");
        builder.AddContent(seq++, caption);
        builder.CloseElement();

        builder.OpenElement(seq++, ""span"");
        builder.AddContent(seq++, value);
        builder.CloseElement();

        builder.CloseElement();
    }

    private static string? FormatUser(CascadingDemoUser? user) => user is null ? null : $""{user.Name} [{user.Role}]"";
}";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => currentTheme = nextTheme"">Switch to @nextTheme theme</BitButton>
<BitButton OnClick=""() => notificationCount++"">Add notification (@notificationCount)</BitButton>
<BitToggle @bind-Value=""isAuthenticated"" Text=""Authenticated user"" />
<BitTextField @bind-Value=""userName"" Label=""UserName:"" Immediate DebounceTime=""300"" />
<BitTextField @bind-Value=""userRole"" Label=""UserRole:"" Immediate DebounceTime=""300"" />


<BitCascadingValueProvider Values=""values"">
    <CascadingValueConsumer Title=""Values"" />
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
];



public sealed class CascadingValueConsumer : ComponentBase
{
    [Parameter] public string? Title { get; set; }

    [CascadingParameter(Name = ""Theme"")]
    public string? Theme { get; set; }

    [CascadingParameter(Name = ""NotificationCount"")]
    public int? NotificationCount { get; set; }

    [CascadingParameter(Name = ""IsAuthenticated"")]
    public bool? IsAuthenticated { get; set; }

    [CascadingParameter(Name = ""NamedUser"")]
    public CascadingDemoUser? NamedUser { get; set; }

    [CascadingParameter]
    public CascadingDemoUser? TypedUser { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;

        builder.OpenElement(seq++, ""div"");

        builder.OpenElement(seq++, ""div"");
        builder.AddAttribute(seq++, ""style"", ""font-weight:bold;font-size:20px"");
        builder.AddContent(seq++, Title ?? ""Child component with cascading parameters:"");
        builder.CloseElement();

        builder.AddMarkupContent(seq++, ""<br />"");

        builder.OpenElement(seq++, ""div"");

        AddValueRow(ref seq, builder, ""Theme: "", Theme ?? ""null"");

        AddValueRow(ref seq, builder, ""Notifications: "", NotificationCount?.ToString() ?? ""null"");

        AddValueRow(ref seq, builder, ""Authenticated: "", IsAuthenticated?.ToString() ?? ""null"");

        AddValueRow(ref seq, builder, ""User (named parameter): "", FormatUser(NamedUser) ?? ""null"");
        AddValueRow(ref seq, builder, ""User (typed parameter): "", FormatUser(TypedUser) ?? ""null"");

        builder.CloseElement();

        builder.CloseElement();
    }

    private static void AddValueRow(ref int seq, RenderTreeBuilder builder, string caption, string? value)
    {
        if (value is null) return;

        builder.OpenElement(seq++, ""div"");

        builder.OpenElement(seq++, ""span"");
        builder.AddAttribute(seq++, ""style"", ""font-weight:bold"");
        builder.AddContent(seq++, caption);
        builder.CloseElement();

        builder.OpenElement(seq++, ""span"");
        builder.AddContent(seq++, value);
        builder.CloseElement();

        builder.CloseElement();
    }

    private static string? FormatUser(CascadingDemoUser? user) => user is null ? null : $""{user.Name} [{user.Role}]"";
}";

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
    <CascadingValueConsumer Title=""ValueList"" />
</BitCascadingValueProvider>";
    private readonly string example3CsharpCode = @"
private readonly string? nullableTheme = null;
private readonly bool? nullableIsAuthenticated = null;
private readonly int? nullableNotificationCount = null;
private readonly CascadingDemoUser? nullableNamedUser = null;
private readonly CascadingDemoUser? nullableTypedUser = null;



public sealed class CascadingValueConsumer : ComponentBase
{
    [Parameter] public string? Title { get; set; }

    [CascadingParameter(Name = ""Theme"")]
    public string? Theme { get; set; }

    [CascadingParameter(Name = ""NotificationCount"")]
    public int? NotificationCount { get; set; }

    [CascadingParameter(Name = ""IsAuthenticated"")]
    public bool? IsAuthenticated { get; set; }

    [CascadingParameter(Name = ""NamedUser"")]
    public CascadingDemoUser? NamedUser { get; set; }

    [CascadingParameter]
    public CascadingDemoUser? TypedUser { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;

        builder.OpenElement(seq++, ""div"");

        builder.OpenElement(seq++, ""div"");
        builder.AddAttribute(seq++, ""style"", ""font-weight:bold;font-size:20px"");
        builder.AddContent(seq++, Title ?? ""Child component with cascading parameters:"");
        builder.CloseElement();

        builder.AddMarkupContent(seq++, ""<br />"");

        builder.OpenElement(seq++, ""div"");

        AddValueRow(ref seq, builder, ""Theme: "", Theme ?? ""null"");

        AddValueRow(ref seq, builder, ""Notifications: "", NotificationCount?.ToString() ?? ""null"");

        AddValueRow(ref seq, builder, ""Authenticated: "", IsAuthenticated?.ToString() ?? ""null"");

        AddValueRow(ref seq, builder, ""User (named parameter): "", FormatUser(NamedUser) ?? ""null"");
        AddValueRow(ref seq, builder, ""User (typed parameter): "", FormatUser(TypedUser) ?? ""null"");

        builder.CloseElement();

        builder.CloseElement();
    }

    private static void AddValueRow(ref int seq, RenderTreeBuilder builder, string caption, string? value)
    {
        if (value is null) return;

        builder.OpenElement(seq++, ""div"");

        builder.OpenElement(seq++, ""span"");
        builder.AddAttribute(seq++, ""style"", ""font-weight:bold"");
        builder.AddContent(seq++, caption);
        builder.CloseElement();

        builder.OpenElement(seq++, ""span"");
        builder.AddContent(seq++, value);
        builder.CloseElement();

        builder.CloseElement();
    }

    private static string? FormatUser(CascadingDemoUser? user) => user is null ? null : $""{user.Name} [{user.Role}]"";
}";
}
