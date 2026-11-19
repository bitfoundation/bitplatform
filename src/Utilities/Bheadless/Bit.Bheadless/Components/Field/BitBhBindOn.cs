namespace Bit.Bheadless;

/// <summary>When a bound text control writes its value back.</summary>
public enum BitBhBindOn
{
    /// <summary>On the <c>change</c> event (blur / commit).</summary>
    Change,

    /// <summary>On every <c>input</c> event (each keystroke).</summary>
    Input,
}
