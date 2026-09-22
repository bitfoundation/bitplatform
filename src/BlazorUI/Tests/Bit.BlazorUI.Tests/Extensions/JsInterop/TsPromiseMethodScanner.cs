using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

/// <summary>
/// Finds the static methods of a TypeScript source that return a Promise, reading nothing but their headers.
///
/// <para>
/// The contract: every Promise-returning interop method must be declared <c>async</c> or annotated
/// <c>: Promise&lt;...&gt;</c>. A method that returns a promise without saying so in its header (an unannotated
/// <c>return fetch(url)</c>, a wrapper that hands back what an async sibling returns) is NOT detected. The header
/// is what a reader and the compiler see, so it is the one thing the source is asked to keep truthful; in return
/// this stays a few regexes over comment- and string-stripped text instead of a TypeScript parser.
/// </para>
///
/// <para>
/// <c>Promise</c> counts only at the top level of the return type: <c>{ p: Promise&lt;void&gt; }</c>,
/// <c>Array&lt;Promise&lt;void&gt;&gt;</c> and <c>() =&gt; Promise&lt;void&gt;</c> are not methods that return a
/// Promise themselves. Static getters/setters and arrow-function properties are not methods and are ignored.
/// </para>
/// </summary>
internal static class TsPromiseMethodScanner
{
    private static readonly Regex TsClassRegex = new(@"\bclass\s+(?<class>\w+)", RegexOptions.Compiled);

    // A static method header up to and including the opening parenthesis of its parameter list. Requiring the
    // "(" right after the name (and optional type parameters, which may nest one level: "<T extends Map<K, V>>")
    // keeps static fields, arrow-function properties ("static foo = () => {}") and getters/setters
    // ("static get foo()") out.
    private static readonly Regex TsStaticMethodHeaderRegex =
        new(@"\bstatic\s+(?<async>async\s+)?(?<method>\w+)\s*(?:<(?:[^<>]|<[^<>]*>)*>)?\s*\(", RegexOptions.Compiled);

    // Comments and string/template literals. Ordinary strings end at a line break (a stray quote, e.g. inside a
    // regex literal, blanks at most the rest of its line); template literals may span lines.
    private static readonly Regex NonCodeRegex =
        new(@"//[^\n]*|/\*.*?\*/|""(?:\\.|[^""\\\n])*""|'(?:\\.|[^'\\\n])*'|`(?:\\.|[^`\\])*`",
            RegexOptions.Compiled | RegexOptions.Singleline);

    // A parameter that may be left out: one marked optional ("id?: string") or given a default ("gap = 0").
    // The "=" has to be the assignment, not the arrow of a function type ("cb: () => void") or a comparison.
    private static readonly Regex DefaultValueRegex = new(@"(?<![=!<>])=(?![=>])", RegexOptions.Compiled);

    /// <summary>What a static TypeScript method's header says about it.</summary>
    /// <param name="ReturnsPromise">Whether it is declared <c>async</c> or annotated with a top-level Promise.</param>
    /// <param name="RequiredParameters">Parameters that are neither optional nor defaulted.</param>
    /// <param name="OptionalParameters">Parameters marked <c>?</c> or given a default value.</param>
    /// <param name="HasRestParameter">Whether it ends in a <c>...rest</c> parameter, so it takes any number.</param>
    public readonly record struct TsMethodSignature(
        bool ReturnsPromise,
        int RequiredParameters,
        int OptionalParameters,
        bool HasRestParameter);

    /// <summary>
    /// Returns every <c>Class.method</c> in <paramref name="text"/> whose header declares it promise-returning.
    /// </summary>
    public static HashSet<string> CollectFromSource(string text)
    {
        return [.. CollectSignaturesFromSource(text).Where(p => p.Value.ReturnsPromise).Select(p => p.Key)];
    }

    /// <summary>
    /// Returns every <c>Class.method</c> in <paramref name="text"/> declared as a static method, whether it
    /// returns a Promise or not. This is the set of JavaScript functions interop can name, which is what
    /// <c>JsInteropIdentifierContractTests</c> resolves every <c>BitBlazorUI.*</c> identifier against.
    /// </summary>
    public static HashSet<string> CollectStaticMethodsFromSource(string text)
    {
        return [.. CollectSignaturesFromSource(text).Keys];
    }

    /// <summary>
    /// Returns what every static method's header in <paramref name="text"/> declares, keyed
    /// <c>Class.method</c>. A name declared more than once keeps the first header, which is the one a lookup
    /// by name could not tell apart anyway.
    /// </summary>
    public static Dictionary<string, TsMethodSignature> CollectSignaturesFromSource(string text)
    {
        // Everything runs against a copy with comments and literals blanked out (line breaks kept, so positions
        // are unchanged): a signature that is quoted or commented out is never read as a declaration, and no
        // brace or parenthesis inside a literal is ever counted.
        var masked = NonCodeRegex.Replace(text, m => Regex.Replace(m.Value, @"[^\r\n]", " "));

        var classes = TsClassRegex.Matches(masked)
            .Select(m => (Name: m.Groups["class"].Value, Index: m.Index))
            .ToList();

        var result = new Dictionary<string, TsMethodSignature>(StringComparer.Ordinal);

        var position = 0;
        while (position < masked.Length)
        {
            var header = TsStaticMethodHeaderRegex.Match(masked, position);
            if (!header.Success) break;

            position = header.Index + header.Length;

            var closeParen = FindClose(masked, position - 1, '(', ')');
            if (closeParen < 0) break;

            var bodyOpen = SkipReturnAnnotation(masked, closeParen + 1, out var annotation);
            if (bodyOpen >= masked.Length || masked[bodyOpen] != '{') continue; // a bodiless overload signature

            // Resume just inside the body rather than past it. Skipping the body would mean counting braces
            // through code this scanner does not parse, and a regular-expression literal - /[\s(\[{‘]/,
            // say - carries an unbalanced "{" that no comment- or string-masking catches. One of those makes
            // the "body" run to the end of the class and swallow every method declared after it: it cost
            // BitRichTextEditor.ts 66 of its 137, none of which any contract test could then see.
            // Scanning the body instead costs nothing here: a header only matches "static &lt;name&gt;(",
            // which inside a method body would have to be a nested class's member - the library has none -
            // and a "static" written in a comment or a literal is already blanked out above.
            position = bodyOpen + 1;

            var owningClass = classes.LastOrDefault(c => c.Index < header.Index).Name;
            if (owningClass is null) continue;

            var key = $"{owningClass}.{header.Groups["method"].Value}";
            if (result.ContainsKey(key)) continue;

            var (required, optional, hasRest) = ReadParameters(masked, header.Index + header.Length, closeParen);

            result[key] = new TsMethodSignature(
                header.Groups["async"].Success || AnnotationHasTopLevelPromise(annotation),
                required,
                optional,
                hasRest);
        }

        return result;
    }

    /// <summary>
    /// Blanks out everything that is not code - comments and string/template literals - keeping every line
    /// break, so positions in the result still point at the same place in the original. Shared with the C#
    /// side's scanning, which needs the same "find the top-level comma" reading over its own sources.
    /// </summary>
    public static string MaskNonCode(string text)
    {
        return NonCodeRegex.Replace(text, m => Regex.Replace(m.Value, @"[^\r\n]", " "));
    }

    /// <summary>
    /// Splits the text between two delimiters on its top-level commas, returning each piece's start and end
    /// index. <paramref name="masked"/> must be the masked copy, so a comma inside a comment or a literal is
    /// not read as a separator.
    /// </summary>
    public static List<(int Start, int End)> SplitTopLevel(string masked, int openIndex, int closeIndex)
    {
        var result = new List<(int Start, int End)>();

        var depth = 0;
        var start = openIndex + 1;

        for (var i = start; i < closeIndex; i++)
        {
            var c = masked[i];

            // The ">" of an arrow ("cb: () => void", a lambda argument) closes nothing; counting it would
            // push the depth below zero and hide every comma after it.
            if (c is '(' or '[' or '{' or '<') depth++;
            else if (c is ')' or ']' or '}') depth = Math.Max(0, depth - 1);
            else if (c == '>' && masked[i - 1] != '=') depth = Math.Max(0, depth - 1);
            else if (c == ',' && depth == 0)
            {
                result.Add((start, i));
                start = i + 1;
            }
        }

        result.Add((start, closeIndex));

        return result;
    }

    /// <summary>Index of the bracket closing the one at <paramref name="openIndex"/>, or -1.</summary>
    public static int FindCloseBracket(string masked, int openIndex, char open, char close)
    {
        return FindClose(masked, openIndex, open, close);
    }

    // How many parameters the header between its "(" and ")" declares, split into the ones that must be
    // passed and the ones that may be left out, plus whether it ends in a rest parameter (which takes any
    // number). Read off the masked copy, so a comma inside a comment in the parameter list - the library
    // documents its wider signatures that way - is not read as a separator.
    private static (int Required, int Optional, bool HasRest) ReadParameters(string masked, int openParenEnd, int closeParen)
    {
        var required = 0;
        var optional = 0;
        var hasRest = false;

        foreach (var (start, end) in SplitTopLevel(masked, openParenEnd - 1, closeParen))
        {
            var parameter = masked[start..end].Trim();
            if (parameter.Length == 0) continue;

            var colon = parameter.IndexOf(':');
            var name = (colon < 0 ? parameter : parameter[..colon]).Trim();

            if (name.StartsWith("...", StringComparison.Ordinal)) hasRest = true;
            else if (name.EndsWith('?') || DefaultValueRegex.IsMatch(parameter)) optional++;
            else required++;
        }

        return (required, optional, hasRest);
    }

    // Returns the index just past the return-type annotation (the body's "{" when the method has one) and the
    // annotation's text. The nesting of (), [] and <> is tracked so that a "{" belonging to the type - an
    // object-literal type "{ [id: string]: State }", or one nested in a generic argument "Promise<{ x: number }>" -
    // is not taken for the body: at depth zero a "{" opens the body unless a type is still expected there (right
    // after the ":" or a type operator), in which case it is an object-literal type and is skipped whole.
    private static int SkipReturnAnnotation(string masked, int start, out string annotation)
    {
        annotation = string.Empty;

        var i = start;
        while (i < masked.Length && char.IsWhiteSpace(masked[i])) i++;
        if (i >= masked.Length || masked[i] != ':') return i;

        var annotationStart = ++i;
        var depth = 0;
        var expectingType = true;

        for (; i < masked.Length; i++)
        {
            var c = masked[i];
            if (char.IsWhiteSpace(c)) continue;

            if (c is '(' or '[' or '<') { depth++; expectingType = true; continue; }
            if (c is ')' or ']' or '>') { if (depth > 0) depth--; expectingType = false; continue; }

            if (depth == 0)
            {
                if (c == ';') break;

                if (c == '{')
                {
                    if (!expectingType) break;

                    var close = FindClose(masked, i, '{', '}');
                    if (close < 0) break;

                    i = close;
                    expectingType = false;
                    continue;
                }
            }

            expectingType = c is '|' or '&' or ',';
        }

        annotation = masked.Substring(annotationStart, i - annotationStart);
        return i;
    }

    // True when the token "Promise" appears at the top level of the return type, i.e. the method itself returns
    // a Promise. A top-level "=>" means the return type is a function type, whose own return type is not this
    // method's; nothing after it counts.
    private static bool AnnotationHasTopLevelPromise(string annotation)
    {
        var topLevel = new StringBuilder();
        var depth = 0;

        for (var i = 0; i < annotation.Length; i++)
        {
            var c = annotation[i];

            if (depth == 0 && c == '=' && i + 1 < annotation.Length && annotation[i + 1] == '>') break;

            if (c is '(' or '[' or '{' or '<') depth++;
            else if (c is ')' or ']' or '}' or '>') depth = Math.Max(0, depth - 1);
            else if (depth == 0) topLevel.Append(c);
        }

        return Regex.IsMatch(topLevel.ToString(), @"\bPromise\b");
    }

    // Index of the bracket closing the one at openIndex, or -1. Only valid over masked text.
    private static int FindClose(string masked, int openIndex, char open, char close)
    {
        var depth = 0;
        for (var i = openIndex; i < masked.Length; i++)
        {
            if (masked[i] == open) depth++;
            else if (masked[i] == close && --depth == 0) return i;
        }

        return -1;
    }
}
