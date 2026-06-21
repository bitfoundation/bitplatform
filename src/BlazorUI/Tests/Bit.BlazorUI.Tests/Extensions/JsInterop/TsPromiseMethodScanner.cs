using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

/// <summary>
/// Best-effort static scan of TypeScript implementation files for static methods that return a Promise.
///
/// <para>
/// Primary detection is cheap and reliable: <c>async</c> modifier or <c>: Promise&lt;...&gt;</c> return type.
/// A secondary body scan catches the residual case where a method is neither annotated nor async but returns
/// <c>fetch(...)</c>, <c>Promise.*</c>, or <c>new Promise(...)</c> at the top level. That scan is intentionally
/// conservative (false negatives over false positives) because it gates CI.
/// </para>
///
/// <para>
/// Same-class delegation (e.g. <c>return Extras.loadResource(...)</c>) is propagated only from explicitly
/// annotated/async callees — body-detected methods do not participate in the fixpoint, so heuristic false
/// positives cannot compound through delegation chains.
/// </para>
///
/// <para>
/// Not modeled: cross-file delegation, local-variable indirection (<c>const p = fetch(); return p;</c>),
/// static getters/setters (<c>static get foo()</c> — the method-header regex requires a parameter list),
/// regex literals and division in backward token scanning, and nested-closure returns (ignored by design).
/// </para>
///
/// <para>
/// The riskiest logic is reverse scanning in <see cref="IsNestedFunctionBodyOpen"/>; it exists solely to
/// support the speculative body scan and does not affect primary async/: Promise detection.
/// </para>
/// </summary>
internal static class TsPromiseMethodScanner
{
    private static readonly Regex TsClassRegex =
        new(@"\bclass\s+(?<class>\w+)", RegexOptions.Compiled);

    // Matches a static method header up to (and including) its opening parameter parenthesis.
    // Requires "(" so static fields and arrow-function properties like "static foo = () => {}" are excluded,
    // and static getters/setters ("static get foo()") are not matched.
    private static readonly Regex TsStaticMethodHeaderRegex =
        new(@"\bstatic\s+(?<async>async\s+)?(?<method>\w+)\s*(?:<[^>]+>)?\s*\(", RegexOptions.Compiled);

    private sealed record TsStaticMethod(string Class, string Method, string Body, bool DeclaredAsync, bool AnnotatedPromise);

    /// <summary>
    /// Returns every static method in <paramref name="text"/> considered promise-returning.
    /// </summary>
    public static HashSet<string> CollectFromSource(string text)
    {
        var classMatches = TsClassRegex.Matches(text)
            .Select(m => (Name: m.Groups["class"].Value, Index: m.Index))
            .OrderBy(c => c.Index)
            .ToList();

        var methods = new List<TsStaticMethod>();

        foreach (Match header in TsStaticMethodHeaderRegex.Matches(text))
        {
            var openParen = header.Index + header.Length - 1;
            var closeParen = FindMatching(text, openParen, '(', ')');
            if (closeParen < 0) continue;

            var bodyOpenBrace = SkipReturnAnnotation(text, closeParen + 1, out var returnAnnotation);
            if (bodyOpenBrace >= text.Length || text[bodyOpenBrace] != '{') continue;

            var bodyCloseBrace = FindMatching(text, bodyOpenBrace, '{', '}');
            if (bodyCloseBrace < 0) continue;

            var declaredAsync = header.Groups["async"].Success;
            var annotatedPromise = returnAnnotation.Contains("Promise", StringComparison.Ordinal);

            var owningClass = classMatches.LastOrDefault(c => c.Index < header.Index);
            if (owningClass.Name is null) continue;

            var body = text.Substring(bodyOpenBrace, bodyCloseBrace - bodyOpenBrace + 1);
            methods.Add(new TsStaticMethod(
                owningClass.Name,
                header.Groups["method"].Value,
                body,
                declaredAsync,
                annotatedPromise));
        }

        var explicitPromiseMethods = new HashSet<string>(StringComparer.Ordinal);
        var promiseMethods = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in methods)
        {
            if (!method.DeclaredAsync && !method.AnnotatedPromise) continue;

            var key = $"{method.Class}.{method.Method}";
            explicitPromiseMethods.Add(key);
            promiseMethods.Add(key);
        }

        // Propagate delegation only from explicit async/: Promise sources (per-file; one class per file today).
        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var method in methods)
            {
                var key = $"{method.Class}.{method.Method}";
                if (promiseMethods.Contains(key)) continue;

                if (!BodyDelegatesToKnownMethods(method.Body, method.Class, explicitPromiseMethods)) continue;

                promiseMethods.Add(key);
                changed = true;
            }
        }

        foreach (var method in methods)
        {
            var key = $"{method.Class}.{method.Method}";
            if (promiseMethods.Contains(key)) continue;

            if (BodyHasDirectTopLevelPromiseReturn(method.Body))
            {
                promiseMethods.Add(key);
            }
        }

        // promiseMethods is the fully-populated result set; return it directly rather than copying.
        return promiseMethods;
    }

    /// <summary>
    /// True when the body has a top-level <c>return fetch(...)</c>, <c>return Promise.*</c>, or
    /// <c>return new Promise(...)</c>. Returns inside nested functions/closures are ignored.
    /// </summary>
    public static bool BodyHasDirectTopLevelPromiseReturn(string body) =>
        ScanTopLevelReturns(body, expressionStart => IsDirectPromiseReturnExpression(body, expressionStart));

    /// <summary>
    /// True when the body has a top-level <c>return</c> that delegates to a method in
    /// <paramref name="knownPromiseMethods"/>. Returns inside nested functions/closures are ignored.
    /// </summary>
    public static bool BodyDelegatesToKnownMethods(string body, string className, HashSet<string> knownPromiseMethods) =>
        ScanTopLevelReturns(body, (expressionStart) =>
            ReturnExpressionDelegatesToPromiseMethod(body, expressionStart, className, knownPromiseMethods));

    private static bool ScanTopLevelReturns(string body, Func<int, bool> expressionMatches)
    {
        var nestedFunctionDepth = 0;
        var braceStack = new Stack<bool>();

        for (var i = 0; i < body.Length; i++)
        {
            i = SkipNonCode(body, i, out var skipped);
            if (skipped) { i--; continue; }
            if (i >= body.Length) break;

            var c = body[i];

            if (c == '{')
            {
                var isFunctionBody = i > 0 && IsNestedFunctionBodyOpen(body, i);
                braceStack.Push(isFunctionBody);
                if (isFunctionBody) nestedFunctionDepth++;
                continue;
            }

            if (c == '}')
            {
                if (braceStack.Count > 0 && braceStack.Pop())
                {
                    nestedFunctionDepth--;
                }

                continue;
            }

            if (nestedFunctionDepth > 0) continue;

            if (!IsIdentifierAt(body, i, "return")) continue;

            i += "return".Length;
            while (i < body.Length && char.IsWhiteSpace(body[i])) i++;
            if (i >= body.Length) continue;

            if (expressionMatches(i)) return true;

            // Non-promise return expressions (e.g. object literals) must not leave inner '{' unpaired on the stack.
            SkipNonMatchingReturnExpression(body, ref i);
            continue;
        }

        return false;
    }

    /// <summary>
    /// Advances <paramref name="index"/> past a non-matching return expression so inner braces/parens are not
    /// mistaken for method structure during the outer scan.
    /// </summary>
    private static void SkipNonMatchingReturnExpression(string body, ref int index)
    {
        if (index >= body.Length) return;

        index = SkipNonCode(body, index, out var skipped);
        if (skipped)
        {
            SkipNonMatchingReturnExpression(body, ref index);
            return;
        }

        if (index >= body.Length) return;

        if (body[index] == '{')
        {
            var closeBrace = FindMatching(body, index, '{', '}');
            if (closeBrace >= 0) index = closeBrace;
            return;
        }

        if (body[index] == '(')
        {
            var closeParen = FindMatching(body, index, '(', ')');
            if (closeParen >= 0) index = closeParen;
        }
    }

    private static bool IsDirectPromiseReturnExpression(string body, int expressionStart)
    {
        if (IsIdentifierAt(body, expressionStart, "fetch") &&
            expressionStart + "fetch".Length < body.Length &&
            body[expressionStart + "fetch".Length] == '(')
        {
            return true;
        }

        if (IsIdentifierAt(body, expressionStart, "Promise") &&
            expressionStart + "Promise".Length < body.Length &&
            body[expressionStart + "Promise".Length] == '.')
        {
            return true;
        }

        if (IsIdentifierAt(body, expressionStart, "new"))
        {
            var i = expressionStart + "new".Length;
            while (i < body.Length && char.IsWhiteSpace(body[i])) i++;
            if (IsIdentifierAt(body, i, "Promise")) return true;
        }

        return false;
    }

    /// <summary>
    /// Heuristically decides whether the <c>{</c> at <paramref name="braceIndex"/> opens a function body
    /// (arrow <c>=&gt;</c>, <c>function (...)</c>, or <c>async function (...)</c>) rather than an object literal
    /// or block. This is a secondary check used only to skip returns inside nested closures — it never drives the
    /// primary async/Promise detection, which relies on explicit annotations.
    /// <para>
    /// It scans backward via <see cref="SkipNonCodeBackward"/> and <see cref="FindMatchingBackward"/>, which, as
    /// documented on <see cref="SkipNonCodeBackward"/>, cannot reliably distinguish regex literals (e.g.
    /// <c>/pattern/</c>) from division, template literals with embedded <c>${...}</c> expressions, or other
    /// edge-case token boundaries. Such ambiguity can misclassify a brace here.
    /// </para>
    /// <para>
    /// The tradeoff is deliberate: prefer false negatives (occasionally missing a nested function, so a few inner
    /// returns get scanned) over false positives (wrongly treating real code as a nested body and silently
    /// dropping a valid top-level return). Maintainers should not "fix" this with more aggressive backward parsing,
    /// since that trades safe misses for incorrect matches.
    /// </para>
    /// </summary>
    private static bool IsNestedFunctionBodyOpen(string text, int braceIndex)
    {
        var i = braceIndex - 1;
        while (i >= 0)
        {
            i = SkipNonCodeBackward(text, i, out var skipped);
            if (skipped) continue;
            break;
        }

        if (i < 0) return false;

        if (text[i] == '>' && i > 0 && text[i - 1] == '=') return true;

        if (text[i] != ')') return false;

        var openParen = FindMatchingBackward(text, i, '(', ')');
        if (openParen < 0) return false;

        i = openParen - 1;
        while (i >= 0)
        {
            i = SkipNonCodeBackward(text, i, out var skipped);
            if (skipped) continue;
            break;
        }

        if (i >= 0 && IsIdentifierEndingAt(text, i, "function")) return true;

        if (i >= 0 && IsIdentifierEndingAt(text, i, "async"))
        {
            i -= "async".Length;
            while (i >= 0)
            {
                i = SkipNonCodeBackward(text, i, out var skippedBeforeAsync);
                if (skippedBeforeAsync) continue;
                break;
            }

            if (i >= 0 && IsIdentifierEndingAt(text, i, "function")) return true;
        }

        return false;
    }

    private static bool ReturnExpressionDelegatesToPromiseMethod(string body, int expressionStart, string className, HashSet<string> knownPromiseMethods)
    {
        var i = expressionStart;

        string? qualifier = null;
        if (char.IsLetter(body[i]) || body[i] == '_')
        {
            var identifierStart = i;
            while (i < body.Length && (char.IsLetterOrDigit(body[i]) || body[i] == '_')) i++;

            if (i < body.Length && body[i] == '.')
            {
                qualifier = body.Substring(identifierStart, i - identifierStart);
                i++;
                while (i < body.Length && char.IsWhiteSpace(body[i])) i++;
            }
            else
            {
                i = identifierStart;
            }
        }

        if (i >= body.Length || !(char.IsLetter(body[i]) || body[i] == '_')) return false;

        var methodStart = i;
        while (i < body.Length && (char.IsLetterOrDigit(body[i]) || body[i] == '_')) i++;
        var methodName = body.Substring(methodStart, i - methodStart);

        while (i < body.Length && char.IsWhiteSpace(body[i])) i++;
        if (i >= body.Length || body[i] != '(') return false;

        if (qualifier is null)
        {
            return knownPromiseMethods.Contains($"{className}.{methodName}");
        }

        if (string.Equals(qualifier, "this", StringComparison.Ordinal) ||
            string.Equals(qualifier, className, StringComparison.Ordinal))
        {
            return knownPromiseMethods.Contains($"{className}.{methodName}");
        }

        return false;
    }

    private static bool IsIdentifierEndingAt(string text, int index, string identifier)
    {
        var start = index - identifier.Length + 1;
        if (start < 0) return false;

        return IsIdentifierAt(text, start, identifier);
    }

    private static bool IsIdentifierAt(string text, int index, string identifier)
    {
        if (index < 0 || index >= text.Length || !text.AsSpan(index).StartsWith(identifier)) return false;

        var end = index + identifier.Length;
        if (index > 0 && (char.IsLetterOrDigit(text[index - 1]) || text[index - 1] == '_')) return false;
        if (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_')) return false;

        return true;
    }

    private static int FindMatching(string text, int openIndex, char open, char close)
    {
        var depth = 0;
        for (var i = openIndex; i < text.Length; i++)
        {
            i = SkipNonCode(text, i, out var skipped);
            if (skipped) { i--; continue; }
            if (i >= text.Length) break;

            var c = text[i];
            if (c == open) depth++;
            else if (c == close)
            {
                depth--;
                if (depth == 0) return i;
            }
        }

        return -1;
    }

    private static int FindMatchingBackward(string text, int closeIndex, char open, char close)
    {
        var depth = 0;
        for (var i = closeIndex; i >= 0; i--)
        {
            i = SkipNonCodeBackward(text, i, out var skipped);
            if (skipped) { i++; continue; }
            if (i < 0) break;

            var c = text[i];
            if (c == close) depth++;
            else if (c == open)
            {
                depth--;
                if (depth == 0) return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Best-effort backward skip used only by <see cref="IsNestedFunctionBodyOpen"/> (the speculative body scan).
    /// This is the riskiest part of the scanner: reverse scanning cannot reliably classify comments, string
    /// literals, regex literals, or division. Misclassification affects nested-function heuristics only —
    /// explicit <c>async</c> / <c>: Promise&lt;...&gt;</c> annotations are unaffected.
    /// </summary>
    private static int SkipNonCodeBackward(string text, int i, out bool skipped)
    {
        skipped = false;
        if (i >= text.Length) return i;

        while (i >= 0 && char.IsWhiteSpace(text[i])) i--;
        if (i < 0) return i;

        if (i >= 1 && text[i - 1] == '/' && text[i] == '/')
        {
            skipped = true;
            i -= 2;
            while (i >= 0 && text[i] != '\n') i--;
            return i;
        }

        if (i >= 1 && text[i] == '/' && text[i - 1] == '*')
        {
            skipped = true;
            i -= 2;
            while (i >= 1 && !(text[i - 1] == '/' && text[i] == '*')) i--;
            return Math.Max(-1, i - 2);
        }

        var c = text[i];
        if (c is '"' or '\'' or '`')
        {
            skipped = true;
            var quote = c;
            i--;
            while (i >= 0 && text[i] != quote)
            {
                if (text[i] == '\\') i--;
                i--;
            }

            return i - 1;
        }

        return i;
    }

    private static int SkipReturnAnnotation(string text, int start, out string annotation)
    {
        annotation = string.Empty;

        var i = start;
        while (i < text.Length && char.IsWhiteSpace(text[i])) i++;

        if (i >= text.Length || text[i] != ':')
        {
            return i;
        }

        var annotationStart = ++i;
        while (i < text.Length && text[i] != '{' && text[i] != ';')
        {
            i = SkipNonCode(text, i, out var skipped);
            if (skipped) continue;
            i++;
        }

        annotation = text.Substring(annotationStart, Math.Max(0, i - annotationStart));
        return i;
    }

    private static int SkipNonCode(string text, int i, out bool skipped)
    {
        skipped = false;
        if (i >= text.Length) return i;

        var c = text[i];

        if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
        {
            skipped = true;
            i += 2;
            while (i < text.Length && text[i] != '\n') i++;
            return i;
        }

        if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
        {
            skipped = true;
            i += 2;
            while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/')) i++;
            return Math.Min(text.Length, i + 2);
        }

        if (c is '"' or '\'' or '`')
        {
            skipped = true;
            var quote = c;
            i++;
            while (i < text.Length && text[i] != quote)
            {
                if (text[i] == '\\') i++;
                i++;
            }
            return Math.Min(text.Length, i + 1);
        }

        return i;
    }
}
