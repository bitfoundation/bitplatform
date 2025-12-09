using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.CascadingValueProvider;

public sealed class CascadingValueConsumer : ComponentBase
{
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



    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;

        builder.OpenElement(seq++, "div");

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "style", "font-weight:bold;font-size:20px");
        builder.AddContent(seq++, Title ?? "Child component with cascading parameters:");
        builder.CloseElement();

        builder.AddMarkupContent(seq++, "<br />");

        builder.OpenElement(seq++, "div");

        AddValueRow(ref seq, builder, "Theme: ", Theme ?? "null");

        AddValueRow(ref seq, builder, "Notifications: ", NotificationCount?.ToString() ?? "null");

        AddValueRow(ref seq, builder, "Authenticated: ", IsAuthenticated?.ToString() ?? "null");

        AddValueRow(ref seq, builder, "User (named parameter): ", FormatUser(NamedUser) ?? "null");
        AddValueRow(ref seq, builder, "User (typed parameter): ", FormatUser(TypedUser) ?? "null");

        builder.CloseElement();

        builder.CloseElement();
    }



    private static void AddValueRow(ref int seq, RenderTreeBuilder builder, string caption, string? value)
    {
        if (value is null) return;

        builder.OpenElement(seq++, "div");

        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "style", "font-weight:bold");
        builder.AddContent(seq++, caption);
        builder.CloseElement();

        builder.OpenElement(seq++, "span");
        builder.AddContent(seq++, value);
        builder.CloseElement();

        builder.CloseElement();
    }

    private static string? FormatUser(CascadingDemoUser? user) => user is null ? null : $"{user.Name} [{user.Role}]";
}
