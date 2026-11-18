using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>The input of a <see cref="BitBhSearchField"/> (<c>type="search"</c>, which is <c>role="searchbox"</c>).</summary>
public class BitBhSearchFieldInput : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhSearchField? SearchField { get; set; }

    protected override string DefaultTag => "input";

    protected override void OnParametersSet() => SearchField?.SetInputId(Id);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var owner = SearchField;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", owner?.InputId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (owner is not null)
        {
            builder.AddAttribute(30, "value", owner.CurrentValue);
            builder.AddAttribute(31, "oninput", On<ChangeEventArgs>("oninput", e => owner.SetValueAsync(e.Value?.ToString() ?? string.Empty)));
            builder.SetUpdatesAttributeName("value");
            builder.AddAttribute(32, "onkeydown", On<KeyboardEventArgs>("onkeydown", owner.HandleKeyDownAsync));
            // While there is text, Escape clears it and goes no further (a dialog around stays open).
            builder.AddEventStopPropagationAttribute(33, "onkeydown", owner.CurrentValue.Length > 0);
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (SearchField is not { } owner) return attrs;
        if (Tag.Equals("input", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "search";
        else attrs["role"] = "searchbox";
        attrs["autocomplete"] = "off";
        if (owner.IsDisabled) attrs["disabled"] = true;
        if (owner.Field?.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;
        // The browser's own Escape-clears for type=search is replaced by ours (it would not tell .NET).
        attrs[BitBhKeyboard.KeysAttribute] = owner.CurrentValue.Length > 0 ? "Escape Enter" : "Enter";
        owner.AddState(attrs);
        return attrs;
    }
}
