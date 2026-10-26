using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Borm.Demo.Client.Renderers;

/// <summary>
/// Blazor's InputNumber&lt;int&gt;, taking an emptied box as 0 - what Borm's own number box does for an int. A number
/// box hands over nothing at all for a cleared box, and for letters typed into one in Firefox; InputNumber refused that
/// with a message only the ValidationSummary showed, the value never reached the form, and the form went on to save
/// the number from before while the box stood empty.
/// </summary>
public sealed class EmptyAsZeroInputNumber : InputNumber<int>
{
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out int result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0;
            validationErrorMessage = null;
            return true;
        }

        return base.TryParseValueFromString(value, out result, out validationErrorMessage);
    }
}
