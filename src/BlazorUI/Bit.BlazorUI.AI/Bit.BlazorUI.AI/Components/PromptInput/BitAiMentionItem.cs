namespace Bit.BlazorUI;

/// <summary>
/// Something the user can reference by typing an at sign in the <see cref="BitAiPromptInput"/>: a file, a person, a data
/// source.
/// </summary>
/// <param name="Id">The stable identifier the application resolves later.</param>
/// <param name="Label">What the user sees and what is inserted into the text.</param>
/// <param name="Description">Extra detail shown beside the label.</param>
/// <param name="Icon">The name of a Fluent UI icon of the built-in set (see <c>BitIconName</c>), shown before the label.</param>
public sealed record BitAiMentionItem(string Id, string Label, string? Description = null, string? Icon = null);
