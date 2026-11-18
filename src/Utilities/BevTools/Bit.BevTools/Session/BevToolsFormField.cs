using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BevTools;

/// <summary>One field of a form as its <see cref="EditContext"/> knows it. Its value is never read.</summary>
/// <param name="Name">The field ("Email"), with its model type when it is not the form's model ("Address.City").</param>
/// <param name="Modified">Whether the EditContext marks it modified.</param>
/// <param name="Changes">How many times it notified a change while BevTools watched the form.</param>
/// <param name="Messages">Its validation messages: redacted, and hidden under <see cref="BevToolsValueAccess.None"/> (a parse error can repeat what was typed).</param>
public sealed record BevToolsFormField(string Name, bool Modified, int Changes, IReadOnlyList<string> Messages);
