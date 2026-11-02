namespace Bit.Borm;

/// <summary>
/// An expression that could not be parsed. It derives from <see cref="FormatException"/>, so
/// <see cref="BormJsonSchemaImporter.TryImport"/> reports a broken expression inside a document the
/// same way it reports broken JSON.
/// </summary>
public sealed class BormExpressionException : FormatException
{
    public BormExpressionException(string message, string expression, int position)
        : base($"{message} (at position {position + 1} of '{Abbreviate(expression)}')")
    {
        Expression = expression;
        Position = position;
    }

    /// <summary>The text that failed to parse.</summary>
    public string Expression { get; }

    /// <summary>The zero-based character position the problem was found at.</summary>
    public int Position { get; }

    // An expression refused for its length is, by definition, too long to repeat in full.
    private static string Abbreviate(string text) => text.Length <= 120 ? text : text[..117] + "...";
}
