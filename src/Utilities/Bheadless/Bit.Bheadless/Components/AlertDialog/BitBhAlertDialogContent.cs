using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The alert dialog's surface — the <c>role="alertdialog"</c> element that holds
/// the title, description, and the Cancel/Action buttons. It is the focus-trap
/// boundary; focus starts on <see cref="BitBhAlertDialogCancel"/> when present.
///
/// <para>Rendered only while open (and while its exit animation runs).</para>
/// </summary>
public class BitBhAlertDialogContent : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhAlertDialogContext? AlertDialogContext { get; set; }

    /// <summary>Child content. Title, description, Cancel/Action, and any other markup go here.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (AlertDialogContext is { IsPresent: false }) return;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", AlertDialogContext?.ContentId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (AlertDialogContext?.IsHidden == true)
            builder.AddAttribute(25, "hidden", true);
        builder.AddAttribute(30, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        builder.AddEventStopPropagationAttribute(31, "onkeydown", true);
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "alertdialog";
        attrs["aria-modal"] = "true";
        if (AlertDialogContext?.HasTitle == true)
            attrs["aria-labelledby"] = AlertDialogContext.TitleId;
        if (AlertDialogContext?.HasDescription == true)
            attrs["aria-describedby"] = AlertDialogContext.DescriptionId;
        attrs["tabindex"] = "-1";
        SetDataState(attrs, AlertDialogContext?.IsOpen ?? false);
        attrs["data-bit-bh-presence"] = "";

        // Default positioning — centred in the viewport, above the overlay.
        // Consumers override entirely via class=/style=.
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = "position:fixed;left:50%;top:50%;transform:translate(-50%,-50%);z-index:51;";
        return attrs;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
        => args.Key == "Escape" && AlertDialogContext is not null
            ? AlertDialogContext.DismissAsync(BitBhDismissReason.EscapeKey)
            : Task.CompletedTask;
}
