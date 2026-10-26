namespace Bit.Borm;

/// <summary>
/// A small, safe expression over the form's values — <c>{Quantity} * {UnitPrice}</c>,
/// <c>{Country} = 'US' and age({BirthDate}) &gt;= 21</c>, <c>sum({Lines[].Total})</c> — that a
/// schema can carry as plain text. It is what lets a form described in JSON compute a value, decide
/// what to show and judge an answer without any code behind it.
/// </summary>
/// <remarks>
/// <para>
/// The language is deliberately not a programming language. There are values, field references,
/// operators and a fixed list of functions (see <see cref="BormExpressionFunctions"/>); there are no
/// variables, assignments, loops, member access or reflection, so evaluating an expression from an
/// untrusted document can read the form's values and nothing else, and always finishes. Parsing is
/// bounded too: an expression longer than <see cref="MaxLength"/> characters, nested deeper than
/// <see cref="MaxDepth"/> or larger than <see cref="MaxNodes"/> nodes is refused.
/// </para>
/// <para>
/// A field is referenced in braces — <c>{Email}</c>, <c>{Address.City}</c>, <c>{Lines[0].Product}</c> —
/// and the path resolves against the object that owns the field the expression is written on before
/// the root, exactly as a condition, a computed dependency or a cascading option does. So
/// <c>{Quantity} * {UnitPrice}</c> on a repeater's row template means <em>that row</em>. <c>[]</c>
/// stands for every row: <c>{Lines[].Total}</c> is the list of every line's total, which is what the
/// aggregate functions (<c>sum</c>, <c>count</c>, <c>min</c>, <c>max</c>, <c>avg</c>) take.
/// </para>
/// <para>
/// A missing answer is null, and null is "unknown" the way it is in SQL: an ordering comparison or an
/// arithmetic operator with a null operand is null, <c>and</c>/<c>or</c> follow three-valued logic,
/// and a condition that comes out null is false. <c>=</c> and <c>!=</c> treat null as a value of its
/// own, so <c>{Notes} = null</c> asks whether it is empty. Text joins with <c>+</c> and a missing part
/// joins as nothing. A validation rule that comes out null passes — a rule comparing two dates says
/// nothing while either is missing; <c>Required()</c> is what says something is missing.
/// </para>
/// </remarks>
public sealed class BormExpression
{
    /// <summary>The longest expression text accepted, in characters.</summary>
    public const int MaxLength = 4096;

    /// <summary>How deeply sub-expressions may nest.</summary>
    public const int MaxDepth = 48;

    /// <summary>How many nodes (values, references, operators and calls) one expression may contain.</summary>
    public const int MaxNodes = 1024;

    private readonly BormExpressionNode _root;

    private BormExpression(string text, BormExpressionNode root, IReadOnlyList<string> dependencies,
        IReadOnlyList<string> referencedPaths)
    {
        Text = text;
        _root = root;
        Dependencies = dependencies;
        ReferencedPaths = referencedPaths;
    }

    /// <summary>The text the expression was parsed from, exactly as written.</summary>
    public string Text { get; }

    /// <summary>
    /// The field paths the expression reads, so the form re-evaluates it only when one of them changes.
    /// A reference across every row (<c>{Lines[].Total}</c>) depends on the list itself
    /// (<c>Lines</c>), which covers a row being added, removed or edited.
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Every field path the expression names, as written, with <c>[]</c> kept — what
    /// <see cref="BormDefinition.Validate"/> checks against the schema.
    /// </summary>
    internal IReadOnlyList<string> ReferencedPaths { get; }

    /// <summary>Parses <paramref name="text"/>.</summary>
    /// <exception cref="BormExpressionException">The text is not a valid expression; the message says where and why.</exception>
    public static BormExpression Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var root = BormExpressionParser.Parse(text);

        var dependencies = new List<string>();
        var referenced = new List<string>();
        root.CollectPaths(referenced);
        foreach (var path in referenced)
        {
            // A reference across every row reads the list as a whole: a row appearing, going or
            // changing anywhere in it is a change to what the expression sees.
            var wildcard = path.IndexOf("[]", StringComparison.Ordinal);
            var dependency = wildcard < 0 ? path : path[..wildcard];
            if (dependency.Length > 0 && !dependencies.Contains(dependency, StringComparer.OrdinalIgnoreCase))
                dependencies.Add(dependency);
        }

        return new BormExpression(text, root,
            dependencies, referenced.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    /// <summary>
    /// Parses <paramref name="text"/>, returning false with the reason instead of throwing — the shape
    /// a schema editor wants behind a box someone is typing an expression into.
    /// </summary>
    public static bool TryParse(string text, out BormExpression? expression, out string? error)
    {
        try
        {
            expression = Parse(text);
            error = null;
            return true;
        }
        catch (BormExpressionException ex)
        {
            expression = null;
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Evaluates the expression against <paramref name="data"/>. Never throws for the values it reads:
    /// an operation that has no answer — text multiplied by a number, a division by zero, a date
    /// that does not parse — is null.
    /// </summary>
    public object? Evaluate(IBormDataReader data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return BormExpressionValue.Collapse(_root.Evaluate(data));
    }

    /// <summary>
    /// Evaluates the expression as a condition: true only when it comes out true. An answer that
    /// cannot be reached because a value it compares is missing is false.
    /// </summary>
    public bool IsTrue(IBormDataReader data) => Test(data) == true;

    /// <summary>
    /// Evaluates the expression as a truth value: true, false, or null when it compares a value that
    /// is missing — the distinction a validation rule needs, since "unknown" is not "wrong".
    /// </summary>
    internal bool? Test(IBormDataReader data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return BormExpressionValue.ToBoolean(_root.Evaluate(data));
    }

    /// <summary>A condition that holds while this expression is true, for <c>VisibleWhen</c> and friends.</summary>
    public BormExpressionCondition ToCondition() => new(this);

    /// <summary>
    /// A computed value that is this expression, evaluated against the object owning the field. The
    /// delegate's target is this expression, which is how a schema exporter recognises it and writes
    /// it back out as text.
    /// </summary>
    public BormComputedValue ToComputed() => Compute;

    private object? Compute(BormComputedContext context)
        => Evaluate(BormScopedDataReader.For(context.Data, context.ScopePath));

    public override string ToString() => Text;
}
