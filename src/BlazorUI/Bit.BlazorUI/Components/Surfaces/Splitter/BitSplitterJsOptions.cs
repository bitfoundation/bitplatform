namespace Bit.BlazorUI;

/// <summary>
/// Everything the browser side of a <see cref="BitSplitter"/> is driven with.
/// </summary>
/// <remarks>
/// It is handed over as a whole, both at setup and on every change, so the two sides can never end up
/// disagreeing about one of these while agreeing about the rest. It is also what the component compares
/// one render against the last with, so a value equality is what it is a record struct for.
/// </remarks>
internal readonly record struct BitSplitterJsOptions(bool Vertical,
                                                     bool Disabled,
                                                     bool Collapsible,
                                                     bool CollapseSecond,
                                                     bool Collapsed,
                                                     int CollapsedSize,
                                                     int KeyboardStep,
                                                     int DragStep,
                                                     int SnapSize,
                                                     bool LazyResize,
                                                     bool ResetOnDoubleClick,
                                                     bool NotifyResize,
                                                     bool NotifyDoubleClick,
                                                     double? Percent,
                                                     string? PersistKey,
                                                     bool PersistSession);
