namespace Bit.BlazorUI;

/// <summary>
/// What the viewer reports when a reader ticks or unticks a task-list checkbox.
/// </summary>
/// <param name="Index">The checkbox's position in the document, counted from 0 in reading order.</param>
/// <param name="Checked">Its new state.</param>
/// <param name="Markdown">
/// The source with that one marker rewritten, ready to be stored. It is produced by
/// <see cref="BitMarkdownTaskList.Toggle(string, BitMarkdownTaskCheckboxNode, bool)"/> from the
/// checkbox itself, which carries the line its marker was parsed from - so the marker rewritten is
/// the one the reader clicked, whatever else in the document looks like a task.
/// </param>
public readonly record struct BitMarkdownViewerTaskChangedEventArgs(int Index, bool Checked, string Markdown);
