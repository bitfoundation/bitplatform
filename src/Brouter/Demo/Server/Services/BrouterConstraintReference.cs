using System.Text;
using Bit.Brouter.Demo.Client;

namespace Bit.Brouter.Demo.Server.Services;

/// <summary>
/// The route constraints as one Markdown table, for the tool and the
/// <c>brouter://constraints</c> resource alike.
/// <para>
/// The rows come from the same <see cref="ConstraintCatalog"/> the site's interactive constraint
/// tester is built from - and that its routes are declared from - so a constraint cannot be
/// documented here without being demonstrable there. A table rather than a list of objects: every
/// row is the same five fields, which is what a table already says once in its header instead of
/// repeating on every row.
/// </para>
/// </summary>
public static class BrouterConstraintReference
{
    private static readonly Lazy<string> _markdown = new(Render_);

    public static string Render() => _markdown.Value;

    private static string Render_()
    {
        var builder = new StringBuilder("# Bit.Brouter route constraints\n\n");

        builder.AppendLine("Written after a parameter name: `{id:int}`, `{code:length(2,4)}`. Chaining is allowed - the LAST type ")
               .AppendLine("constraint decides the type the value binds as, and the rest just gate it. Check a finished template with ")
               .AppendLine("`InspectBrouterRouteTemplates`; a constraint this table does not list is a parse error, not a silent pass.")
               .AppendLine();

        builder.AppendLine("| Constraint | Category | Rule | Passes | Fails |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");

        foreach (var constraint in ConstraintCatalog.All)
        {
            builder.AppendLine($"| `{{value:{constraint.Token}}}` | {constraint.Category} | {constraint.Rule} " +
                               $"| `{constraint.PassExample}` | `{constraint.FailExample}` |");
        }

        builder.AppendLine();
        builder.AppendLine("Categories: **type** validates AND converts the bound value; **validation** accepts or rejects while the ")
               .AppendLine("value stays a string; **custom** is registered on `BrouterOptions.Constraints` (this site registers `slug`); ")
               .AppendLine("**chain** is several of them applied in order.");

        return builder.ToString();
    }
}
