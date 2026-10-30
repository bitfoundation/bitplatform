using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The search input of a <see cref="BitBhCommand"/>: <c>&lt;input role="combobox"&gt;</c>
/// controlling the list, with <c>aria-activedescendant</c> pointing at the
/// highlighted item. Focus stays here while navigating.
/// </summary>
public class BitBhCommandInput : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhCommandContext? CommandContext { get; set; }

    /// <summary>Focus the input when it first renders (default: true).</summary>
    [Parameter]
    public bool AutoFocus { get; set; } = true;

    protected override string DefaultTag => "input";

    private BitBhCommand? Command => CommandContext?.Command;

    protected override void OnInitialized()
    {
        if (CommandContext is not null) CommandContext.Input = this;
    }

    internal void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", CommandContext?.InputId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "value", CommandContext?.Search ?? string.Empty);
        builder.AddAttribute(31, "oninput",
            On<ChangeEventArgs>("oninput", HandleInput));
        builder.SetUpdatesAttributeName("value");
        builder.AddAttribute(32, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && AutoFocus && CommandContext is not null)
            await Interop.FocusAsync(CommandContext.InputId);
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["type"] = "text";
        attrs["role"] = "combobox";
        attrs["aria-expanded"] = "true";
        attrs["aria-autocomplete"] = "list";
        attrs["autocomplete"] = "off";
        attrs["autocorrect"] = "off";
        attrs["spellcheck"] = "false";
        if (CommandContext is not null)
        {
            if (CommandContext.List is not null) attrs["aria-controls"] = CommandContext.ListId;
            if (CommandContext.ActiveItem is { } active)
                attrs["aria-activedescendant"] = active.ItemId;
            attrs[BitBhKeyboard.KeysAttribute] = "ArrowDown ArrowUp PageDown PageUp Enter Ctrl+n Ctrl+p Ctrl+j Ctrl+k Ctrl+Home Ctrl+End Meta+Home Meta+End";
        }
        return attrs;
    }

    private Task HandleInput(ChangeEventArgs e)
        => Command?.SetSearchAsync(e.Value?.ToString() ?? string.Empty) ?? Task.CompletedTask;

    private Task HandleKeyDown(KeyboardEventArgs e)
        => Command?.HandleInputKeyDownAsync(e) ?? Task.CompletedTask;

    public void Dispose()
    {
        if (CommandContext?.Input == this) CommandContext.Input = null;
    }
}
