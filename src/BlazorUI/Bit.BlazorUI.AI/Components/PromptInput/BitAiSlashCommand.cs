namespace Bit.BlazorUI;

/// <summary>
/// A command offered when the user types a forward slash in the <see cref="BitAiPromptInput"/>.
/// </summary>
/// <param name="Name">The command word, without the slash.</param>
/// <param name="Description">What it does, shown beside the name.</param>
/// <param name="Icon">The name of a Fluent UI icon of the built-in set (see <c>BitIconName</c>), shown before the name.</param>
/// <remarks>
/// Selecting a command either inserts its <see cref="Template"/> into the composer or invokes
/// <see cref="Handler"/>. The first suits a prompt shortcut, the second an application action such
/// as clearing the conversation.
/// </remarks>
public sealed record BitAiSlashCommand(string Name, string? Description = null, string? Icon = null)
{
    /// <summary>Text inserted into the composer when the command is chosen.</summary>
    public string? Template { get; init; }

    /// <summary>Invoked when the command is chosen, instead of inserting text.</summary>
    public Func<Task>? Handler { get; init; }
}
