namespace Bit.BlazorUI;

/// <summary>
/// What the viewer reports when a reader ticks or unticks a task-list checkbox.
/// </summary>
/// <param name="Index">The checkbox's position in the document, counted from 0 in reading order.</param>
/// <param name="Checked">Its new state.</param>
/// <param name="Markdown">
/// The source with that one marker rewritten, ready to be stored. It is produced by
/// <see cref="BitMarkdownTaskList.Toggle"/>, which counts the same markers the viewer drew.
/// </param>
public readonly record struct BitMarkdownViewerTaskChangedEventArgs(int Index, bool Checked, string Markdown);
