using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>
/// The input of a <see cref="BitBhPasswordField"/>: a <see cref="BitBhInput"/> (so <c>@bind-Value</c>, <c>EditForm</c> validation
/// and <see cref="BitBhField"/> wiring work) whose type follows the toggle.
/// </summary>
public class BitBhPasswordFieldInput : BitBhInput
{
    [CascadingParameter]
    private BitBhPasswordField? PasswordField { get; set; }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var visible = PasswordField?.IsVisible ?? false;
        attrs["type"] = visible ? "text" : "password";
        attrs["autocomplete"] = "current-password";
        attrs["autocapitalize"] = "off";
        attrs["autocorrect"] = "off";
        attrs["spellcheck"] = "false";
        SetDataFlag(attrs, "visible", visible);
        return attrs;
    }

    /// <inheritdoc />
    protected override string ControlId => Id ?? PasswordField?.InputId ?? base.ControlId;
}
