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

    /// <summary>
    /// Returns every <c>Class.method</c> in <paramref name="text"/> whose header declares it promise-returning.
    /// </summary>
    public static HashSet<string> CollectFromSource(string text)
    {
        // Everything runs against a copy with comments and literals blanked out (line breaks kept, so positions
        // are unchanged): a signature that is quoted or commented out is never read as a declaration, and no
        // brace or parenthesis inside a literal is ever counted.
        var masked = NonCodeRegex.Replace(text, m => Regex.Replace(m.Value, @"[^\r\n]", " "));

        var classes = TsClassRegex.Matches(masked)
            .Select(m => (Name: m.Groups["class"].Value, Index: m.Index))
            .ToList();

        var result = new HashSet<string>(StringComparer.Ordinal);

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

            var bodyClose = FindClose(masked, bodyOpen, '{', '}');
            if (bodyClose < 0) break;

            // Resume after the body, so nothing declared inside it is read as the next header.
            position = bodyClose + 1;

            var owningClass = classes.LastOrDefault(c => c.Index < header.Index).Name;
            if (owningClass is null) continue;

            if (header.Groups["async"].Success || AnnotationHasTopLevelPromise(annotation))
            {
                result.Add($"{owningClass}.{header.Groups["method"].Value}");
            }
        }

        return result;
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
