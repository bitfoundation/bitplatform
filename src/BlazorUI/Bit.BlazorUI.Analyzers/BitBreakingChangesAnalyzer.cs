using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Bit.BlazorUI.Analyzers;

/// <summary>
/// Fails an app's build on the names of the bit BlazorUI packages' API that are gone (BreakingChanges.json), saying what
/// to write instead.
/// <para>
/// Markup is read where Razor leaves it: in the code the Razor compiler generates for a .razor file, which the compiler
/// hands analyzers like any other source. A component's attribute is a <c>__builder.AddComponentParameter(seq, "Name",
/// value)</c> after the <c>__builder.OpenComponent&lt;TComponent&gt;(seq)</c> of its tag, so the component is known
/// exactly, generic or derived by the app. A generic component whose type arguments Razor infers is opened by a
/// <c>TypeInference.CreateXxx_N</c> helper instead, called where the tag is written with the attributes' values as its
/// arguments; the helper's body says which argument is which attribute. A name that is still a parameter is written
/// there as <c>nameof(...)</c> and is left alone; a name that is not is a string literal, which Razor puts in a hidden
/// region with no position of its own, so the position is found in the .razor file (an additional file of the
/// compilation) after the nearest one the generated code does map. A removed component's tag compiles to no component at
/// all, so the .razor files are read for it directly, outside the C# the generated code maps back to them. C# that names
/// a type or a member that is gone already fails to compile; there the diagnostic only adds what to write instead.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BitBreakingChangesAnalyzer : DiagnosticAnalyzer
{
    public const string MemberDiagnosticId = "BITBC001";
    public const string TypeDiagnosticId = "BITBC002";

    private const string HelpLink = "https://github.com/bitfoundation/bitplatform/blob/develop/src/BlazorUI/BreakingChanges.md";

    public static readonly DiagnosticDescriptor MemberRule = new(
        MemberDiagnosticId,
        title: "A parameter or member removed from bit BlazorUI is used",
        messageFormat: "'{0}' of {1} is gone since bit BlazorUI {2}. {3}",
        category: "BitBlazorUI.BreakingChanges",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A parameter written on a component that no longer has it still compiles - the component renders it as an HTML attribute - and does nothing. Rename it as the message says. Turn the check off with <BitBlazorUIBreakingChangeAnalyzer>false</BitBlazorUIBreakingChangeAnalyzer> in the project file.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor TypeRule = new(
        TypeDiagnosticId,
        title: "A component or type removed from bit BlazorUI is used",
        messageFormat: "'{0}' is gone since bit BlazorUI {1}. {2}",
        category: "BitBlazorUI.BreakingChanges",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The tag of a component that no longer exists compiles with only the warning RZ10012 and renders as an unknown HTML element. Replace it as the message says. Turn the check off with <BitBlazorUIBreakingChangeAnalyzer>false</BitBlazorUIBreakingChangeAnalyzer> in the project file.",
        helpLinkUri: HelpLink);

    /// <summary>A tag that may be a component's in a .razor file.</summary>
    private static readonly Regex ComponentTag = new(@"<([A-Z][A-Za-z0-9_]*)(?=[\s/>])");

    /// <summary>A comment or a start or end tag of static markup: the end tag's slash, the name, the self-closing slash.</summary>
    private static readonly Regex MarkupTag = new(@"<!--[\s\S]*?-->|<(/?)([A-Za-z][A-Za-z0-9:_.-]*)(?:[^>""']|""[^""]*""|'[^']*')*?(/?)>");

    private static readonly Regex RazorComment = new(@"@\*[\s\S]*?\*@");

    private static readonly Regex HtmlComment = new(@"<!--[\s\S]*?-->");

    /// <summary>The elements that have no end tag, so a start tag of one opens nothing.</summary>
    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
    };

    private readonly BitBreakingChanges _changes;

    public BitBreakingChangesAnalyzer() : this(BitBreakingChanges.Default) { }

    /// <summary>For tests: an analyzer of the given changes rather than of the embedded ones.</summary>
    public BitBreakingChangesAnalyzer(BitBreakingChanges changes) => _changes = changes;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MemberRule, TypeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            // Nothing to look for in a project that does not reference the packages.
            if (start.Compilation.ReferencedAssemblyNames.Any(a => a.Name.StartsWith("Bit.BlazorUI", StringComparison.Ordinal)) is false) return;

            // The tree generated for each .razor file, found once per compilation rather than once per file.
            var compilation = start.Compilation;
            var generated = new Lazy<List<(string Path, SyntaxTree Tree)>>(() => RazorTrees(compilation), LazyThreadSafetyMode.PublicationOnly);

            start.RegisterSemanticModelAction(AnalyzeTree);
            start.RegisterAdditionalFileAction(ctx => AnalyzeRazorFile(ctx, generated.Value));
        });
    }

    // ---- C# and the code generated for markup -------------------------------------------------------------------------

    /// <summary>
    /// What the builder has open: a component (its type) or an element (null). A component a type-inference helper opens
    /// is open for the arguments of the call (its child content), up to <see cref="ScopeEnd"/>; one the builder opens
    /// itself, until its <c>CloseComponent</c> (-1).
    /// </summary>
    private readonly struct Frame(ITypeSymbol? component, int scopeEnd)
    {
        public ITypeSymbol? Component { get; } = component;

        public int ScopeEnd { get; } = scopeEnd;
    }

    private void AnalyzeTree(SemanticModelAnalysisContext ctx)
    {
        var model = ctx.SemanticModel;
        var tree = model.SyntaxTree;
        var root = tree.GetRoot(ctx.CancellationToken);
        var razor = new RazorMapping(tree, ctx.Options.AdditionalFiles, ctx.CancellationToken);

        // Innermost last.
        var frames = new List<Frame>();

        foreach (var node in root.DescendantNodes())
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            CloseScopes(frames, node.SpanStart);

            switch (node)
            {
                case InvocationExpressionSyntax invocation:
                    AnalyzeBuilderCall(ctx, model, invocation, frames, razor);
                    break;
                case MemberAccessExpressionSyntax access:
                    AnalyzeMemberAccess(ctx, model, access.Name, access.Expression);
                    break;
                case MemberBindingExpressionSyntax binding when ConditionalReceiver(binding) is { } receiver:
                    AnalyzeMemberAccess(ctx, model, binding.Name, receiver);
                    break;
                case AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax, Left: IdentifierNameSyntax left } assignment:
                    AnalyzeInitializer(ctx, model, assignment, left);
                    break;
                case IdentifierNameSyntax or GenericNameSyntax:
                    AnalyzeTypeName(ctx, model, (SimpleNameSyntax)node);
                    break;
            }
        }
    }

    /// <summary>Closes the components of the type-inference calls that end before <paramref name="position"/>, and all inside them.</summary>
    private static void CloseScopes(List<Frame> frames, int position)
    {
        for (var i = frames.Count - 1; i >= 0 && i < frames.Count; i--)
        {
            if (frames[i].ScopeEnd >= 0 && frames[i].ScopeEnd <= position) frames.RemoveRange(i, frames.Count - i);
        }
    }

    private void AnalyzeBuilderCall(SemanticModelAnalysisContext ctx, SemanticModel model, InvocationExpressionSyntax invocation,
                                    List<Frame> frames, RazorMapping razor)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member) return;

        var name = member.Name.Identifier.ValueText;

        if (IsTypeInferenceClass(member.Expression))
        {
            AnalyzeTypeInferenceCall(ctx, model, invocation, frames, razor);
            return;
        }

        if (name is not ("OpenComponent" or "CloseComponent" or "OpenElement" or "CloseElement" or "AddComponentParameter" or "AddAttribute" or "AddMarkupContent")) return;

        if (model.GetSymbolInfo(invocation, ctx.CancellationToken).Symbol is not IMethodSymbol method
            || method.ContainingType?.Name != "RenderTreeBuilder") return;

        // The body of a type-inference helper is read at its call, where the tag is written.
        if (IsInTypeInferenceHelper(invocation)) return;

        var args = invocation.ArgumentList.Arguments;

        switch (name)
        {
            case "OpenComponent":
                frames.Add(new Frame(method.IsGenericMethod ? method.TypeArguments[0] : null, -1));
                return;
            case "OpenElement":
                {
                    var tag = args.Count > 1 ? Constant(model, args[1].Expression) : null;
                    var component = ParentComponent(frames);
                    frames.Add(new Frame(null, -1));
                    // A template parameter that is gone, written as a child content tag (<NotMatched>...</NotMatched>):
                    // Razor no longer knows it and takes it for an element of the content. Only a tag right inside the
                    // component's content is one: Razor binds no template to a tag nested in an element there.
                    if (tag is not null && component is not null && char.IsUpper(tag[0]))
                        ReportMember(ctx, component, tag, args[1].Expression, razor, childTag: true, exactCase: true);
                    return;
                }
            case "CloseComponent":
            case "CloseElement":
                if (frames.Count > 0 && frames[frames.Count - 1].ScopeEnd < 0) frames.RemoveAt(frames.Count - 1);
                return;
            case "AddMarkupContent":
                {
                    var component = ParentComponent(frames);
                    var markup = args.Count > 1 ? Constant(model, args[1].Expression) : null;
                    if (component is null || markup is null) return;
                    // Static markup is collapsed into one string, child content tags that are gone included.
                    foreach (var tag in TopLevelTags(markup))
                        ReportMember(ctx, component, tag, args[1].Expression, razor, childTag: true, exactCase: true);
                    return;
                }
            default: // AddComponentParameter, AddAttribute
                {
                    if (ParentComponent(frames) is not { } component) return; // an element's attribute
                    if (args.Count < 2) return;
                    var attribute = Constant(model, args[1].Expression);
                    if (attribute is null) return;
                    ReportMember(ctx, component, attribute, args[1].Expression, razor, childTag: false, exactCase: false);
                    return;
                }
        }
    }

    /// <summary>
    /// A generic component whose type arguments Razor infers: <c>TypeInference.CreateBitDropdown_0(__builder, seq,
    /// __seq0, __arg0, __seq1, __arg1, ...)</c>, called where the tag is written, whose body - at the end of the generated
    /// file - opens the component and adds each argument as the attribute it is. So the attributes are read off the body
    /// and reported at their arguments, and the call's arguments (its child content among them) are inside the component.
    /// </summary>
    private void AnalyzeTypeInferenceCall(SemanticModelAnalysisContext ctx, SemanticModel model, InvocationExpressionSyntax invocation,
                                          List<Frame> frames, RazorMapping razor)
    {
        if (model.GetSymbolInfo(invocation, ctx.CancellationToken).Symbol is not IMethodSymbol { IsStatic: true } method) return;

        var body = method.OriginalDefinition.DeclaringSyntaxReferences
                         .Where(r => r.SyntaxTree == model.SyntaxTree)
                         .Select(r => r.GetSyntax(ctx.CancellationToken))
                         .OfType<MethodDeclarationSyntax>()
                         .FirstOrDefault();
        if (body is null) return;

        ITypeSymbol? component = null;
        var attributes = new List<(string Name, ExpressionSyntax At)>();
        var callArgs = invocation.ArgumentList.Arguments;

        foreach (var call in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (call.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: var callName }) continue;

            if (callName == "OpenComponent")
            {
                // The first one is the component of the tag; the helper opens no other.
                if (component is null && model.GetSymbolInfo(call, ctx.CancellationToken).Symbol is IMethodSymbol { IsGenericMethod: true } open)
                    component = open.TypeArguments[0];
                continue;
            }

            if (callName is not ("AddComponentParameter" or "AddAttribute")) continue;

            var args = call.ArgumentList.Arguments;
            if (args.Count < 2 || Constant(model, args[1].Expression) is not { } attribute) continue;

            // Reported at the sequence-number argument of the attribute: in the hidden code right before its value, so
            // the search for its name stays between the values of the attributes around it, as at a builder call.
            var at = args[0].Expression is IdentifierNameSyntax seq ? Argument(method, callArgs, seq.Identifier.ValueText) : null;
            attributes.Add((attribute, at ?? invocation));
        }

        if (component is null) return;

        // In the order they are written, which the search through the .razor file goes by.
        foreach (var (attribute, at) in attributes.OrderBy(a => a.At.SpanStart))
            ReportMember(ctx, component, attribute, at, razor, childTag: false, exactCase: false);

        frames.Add(new Frame(component, invocation.Span.End));
    }

    /// <summary>The argument of a call that is passed for the parameter named <paramref name="parameter"/>.</summary>
    private static ExpressionSyntax? Argument(IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> args, string parameter)
    {
        foreach (var arg in args)
        {
            if (arg.NameColon?.Name.Identifier.ValueText == parameter) return arg.Expression;
        }

        for (var i = 0; i < method.Parameters.Length && i < args.Count; i++)
        {
            if (args[i].NameColon is null && method.Parameters[i].Name == parameter) return args[i].Expression;
        }

        return null;
    }

    /// <summary>Whether <paramref name="expression"/> names the class of Razor's type-inference helpers (<c>...TypeInference</c>).</summary>
    private static bool IsTypeInferenceClass(ExpressionSyntax expression)
        => expression switch
        {
            MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText == "TypeInference",
            IdentifierNameSyntax i => i.Identifier.ValueText == "TypeInference",
            AliasQualifiedNameSyntax a => a.Name.Identifier.ValueText == "TypeInference",
            QualifiedNameSyntax q => q.Right.Identifier.ValueText == "TypeInference",
            _ => false
        };

    private static bool IsInTypeInferenceHelper(SyntaxNode node)
        => node.Ancestors().OfType<ClassDeclarationSyntax>().Any(c => c.Identifier.ValueText == "TypeInference");

    /// <summary>The component whose content <paramref name="frames"/> is right inside, or null inside an element.</summary>
    private static ITypeSymbol? ParentComponent(List<Frame> frames)
        => frames.Count > 0 ? frames[frames.Count - 1].Component : null;

    /// <summary>The start tags of static markup that are not inside another element of it, with an upper-case name.</summary>
    private static IEnumerable<string> TopLevelTags(string markup)
    {
        var depth = 0;

        foreach (Match tag in MarkupTag.Matches(markup))
        {
            if (tag.Groups[2].Success is false) continue; // a comment

            var name = tag.Groups[2].Value;

            if (tag.Groups[1].Length > 0)
            {
                if (depth > 0) depth--;
                continue;
            }

            if (depth == 0 && char.IsUpper(name[0])) yield return name;

            if (tag.Groups[3].Length == 0 && VoidElements.Contains(name) is false) depth++;
        }
    }

    private void ReportMember(SemanticModelAnalysisContext ctx, ITypeSymbol component, string written, ExpressionSyntax at,
                              RazorMapping razor, bool childTag, bool exactCase)
    {
        // @bind-Old writes Old and OldChanged.
        var name = written;
        var suffix = string.Empty;
        if (_changes.MembersByName.ContainsKey(name) is false && name.EndsWith("Changed", StringComparison.Ordinal))
        {
            name = name.Substring(0, name.Length - "Changed".Length);
            suffix = "Changed";
        }

        if (_changes.MembersByName.TryGetValue(name, out var candidates) is false) return;

        // A name the component still takes is its parameter, whatever it used to be. Not for a child content tag: Razor
        // binds one to a template parameter of its name, so a tag it left as an element has none, whatever else that
        // name is now (BitMediaQuery's Matched, a template once, is the bool state now).
        if (childTag is false && HasParameter(component, written)) return;

        var change = Match(component, candidates, exactCase ? name : null);
        if (change is null) return;

        var location = razor.Locate(at, written, childTag, out var found);

        // @bind-Old writes Old and then OldChanged, and only Old is in the .razor file: the one error on Old says both.
        if (found is false && suffix.Length > 0 && razor.LastReported is { } last
            && SymbolEqualityComparer.Default.Equals(last.Component, component) && string.Equals(last.Name, name, StringComparison.OrdinalIgnoreCase))
            return;

        razor.LastReported = (component, name);

        var hint = suffix.Length > 0 && change.Use.Length > 0 ? $"{change.Hint} ({written} is {change.Use}{suffix}.)" : change.Hint;
        ctx.ReportDiagnostic(Diagnostic.Create(MemberRule, location ?? at.GetLocation(), written, component.Name, change.Since, hint));
    }

    private static BitBreakingChange? Match(ITypeSymbol type, ImmutableArray<BitBreakingChange> candidates, string? exactName)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            foreach (var change in candidates)
            {
                if (exactName is not null && string.Equals(change.Name, exactName, StringComparison.Ordinal) is false) continue;
                if (change.On.Contains(t.Name) && IsLibraryType(t)) return change;
            }
        }
        return null;
    }

    /// <summary>A type of the packages (not one of the app's own that happens to share a name).</summary>
    private static bool IsLibraryType(ITypeSymbol type)
        => type.ContainingAssembly?.Name.StartsWith("Bit.BlazorUI", StringComparison.Ordinal) is true;

    private static bool HasParameter(ITypeSymbol component, string name)
    {
        for (var t = component; t is not null; t = t.BaseType)
        {
            foreach (var member in t.GetMembers())
            {
                if (member is IPropertySymbol p
                    && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)
                    && p.GetAttributes().Any(a => a.AttributeClass?.Name is "ParameterAttribute" or "CascadingParameterAttribute"))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// The receiver of <c>x?.Name</c>: the expression of the conditional access whose part after the <c>?</c> the
    /// binding is in (<c>a</c> for both bindings of <c>a?.b?.c</c>'s <c>.b</c>, <c>.b</c> for <c>.c</c>).
    /// </summary>
    private static ExpressionSyntax? ConditionalReceiver(MemberBindingExpressionSyntax binding)
    {
        for (var node = binding.Parent; node is not null; node = node.Parent)
        {
            if (node is ConditionalAccessExpressionSyntax access && access.WhenNotNull.Span.Contains(binding.Span)) return access.Expression;
        }
        return null;
    }

    /// <summary><c>receiver.Name</c> and <c>receiver?.Name</c> alike.</summary>
    private void AnalyzeMemberAccess(SemanticModelAnalysisContext ctx, SemanticModel model, SimpleNameSyntax nameNode, ExpressionSyntax receiverExpression)
    {
        var name = nameNode.Identifier.ValueText;
        if (_changes.MembersByName.TryGetValue(name, out var candidates) is false) return;
        if (IsResolved(model, nameNode, ctx)) return;

        var receiver = Unwrap(model.GetTypeInfo(receiverExpression, ctx.CancellationToken).Type);
        if (receiver is null) return;

        var change = Match(receiver, candidates, name);
        if (change is null) return;

        ctx.ReportDiagnostic(Diagnostic.Create(MemberRule, nameNode.GetLocation(), name, receiver.Name, change.Since, change.Hint));
    }

    private void AnalyzeInitializer(SemanticModelAnalysisContext ctx, SemanticModel model, AssignmentExpressionSyntax assignment, IdentifierNameSyntax left)
    {
        var name = left.Identifier.ValueText;
        if (_changes.MembersByName.TryGetValue(name, out var candidates) is false) return;
        if (IsResolved(model, left, ctx)) return;

        // new T { Old = ... }, x with { Old = ... } and the nested new() { Old = { ... } } of a name selector alike.
        var created = Unwrap(assignment.Parent?.Parent switch
        {
            BaseObjectCreationExpressionSyntax creation => model.GetTypeInfo(creation, ctx.CancellationToken).Type,
            WithExpressionSyntax with => model.GetTypeInfo(with.Expression, ctx.CancellationToken).Type,
            AssignmentExpressionSyntax outer => model.GetTypeInfo(outer.Left, ctx.CancellationToken).Type,
            _ => null
        });
        if (created is null) return;

        var change = Match(created, candidates, name);
        if (change is null) return;

        ctx.ReportDiagnostic(Diagnostic.Create(MemberRule, left.GetLocation(), name, created.Name, change.Since, change.Hint));
    }

    /// <summary>The <c>T</c> of a <c>T?</c> that is a value type; anything else as it is.</summary>
    private static ITypeSymbol? Unwrap(ITypeSymbol? type)
        => type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : type;

    private void AnalyzeTypeName(SemanticModelAnalysisContext ctx, SemanticModel model, SimpleNameSyntax node)
    {
        var name = node.Identifier.ValueText;
        if (_changes.TypesByName.TryGetValue(name, out var change) is false) return;

        switch (node.Parent)
        {
            // A member, which AnalyzeMemberAccess or AnalyzeInitializer reads - not a type, whatever else the name is: the
            // part after the dot of a.b or a?.b (unless a is the packages' namespace), the member an initializer sets, the
            // name of an argument or of an anonymous type's member.
            case MemberAccessExpressionSyntax m when m.Name == node && IsPackagesNamespace(model, m.Expression, ctx) is false:
            case QualifiedNameSyntax q when q.Right == node && IsPackagesNamespace(model, q.Left, ctx) is false:
            case AliasQualifiedNameSyntax a when a.Name == node && IsPackagesNamespace(model, a.Alias, ctx) is false:
            case MemberBindingExpressionSyntax:
            case AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } assignment when assignment.Left == node:
            case NameColonSyntax:
            case NameEqualsSyntax:
                return;
        }

        if (IsResolved(model, node, ctx)) return;

        ctx.ReportDiagnostic(Diagnostic.Create(TypeRule, node.GetLocation(), name, change.Since, change.Hint));
    }

    /// <summary>Whether <paramref name="expression"/> is the packages' namespace, however written: Bit.BlazorUI, global::Bit.BlazorUI, an alias of it.</summary>
    private static bool IsPackagesNamespace(SemanticModel model, ExpressionSyntax expression, SemanticModelAnalysisContext ctx)
    {
        var symbol = model.GetSymbolInfo(expression, ctx.CancellationToken).Symbol;
        if (symbol is IAliasSymbol alias) symbol = alias.Target;
        if (symbol is INamespaceSymbol ns) return ns.ToDisplayString() == "Bit.BlazorUI";

        // Unresolved: as it is written.
        return expression.ToString() is "Bit.BlazorUI" or "global::Bit.BlazorUI";
    }

    /// <summary>
    /// Whether the name binds to something the code can use. A member or type the packages made internal is found but
    /// inaccessible, which is as gone as a removed one to the app.
    /// </summary>
    private static bool IsResolved(SemanticModel model, ExpressionSyntax node, SemanticModelAnalysisContext ctx)
    {
        var info = model.GetSymbolInfo(node, ctx.CancellationToken);
        return info.Symbol is not null || (info.CandidateSymbols.Length > 0 && info.CandidateReason != CandidateReason.Inaccessible);
    }

    private static string? Constant(SemanticModel model, ExpressionSyntax expression)
        => model.GetConstantValue(expression) is { HasValue: true, Value: string s } && s.Length > 0 ? s : null;

    // ---- the tags of removed components in .razor files ----------------------------------------------------------------

    private void AnalyzeRazorFile(AdditionalFileAnalysisContext ctx, List<(string Path, SyntaxTree Tree)> generated)
    {
        var path = ctx.AdditionalFile.Path;
        if (path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) is false) return;

        var text = ctx.AdditionalFile.GetText(ctx.CancellationToken);
        if (text is null) return;

        var content = WithoutComments(text.ToString());

        // The C# of the file (@code, @{ }, an expression, a directive): its own tree's diagnostics cover what is written
        // there, and a string or a type argument there is no tag. The generated code maps each piece of it back here.
        var code = CodeSpans(generated, path, text, ctx.CancellationToken);

        foreach (Match tag in ComponentTag.Matches(content))
        {
            var name = tag.Groups[1].Value;
            if (_changes.TypesByName.TryGetValue(name, out var change) is false || change.Component is false) continue;
            if (code.Any(s => s.Contains(tag.Index))) continue;
            // An app's own component of that name, or one the packages have again.
            if (IsKnownType(ctx.Compilation, name)) continue;

            var span = new TextSpan(tag.Groups[1].Index, name.Length);
            var location = Location.Create(path, span, text.Lines.GetLinePositionSpan(span));
            ctx.ReportDiagnostic(Diagnostic.Create(TypeRule, location, name, change.Since, change.Hint));
        }
    }

    /// <summary>The spans of a .razor file that its generated tree maps C# to.</summary>
    private static List<TextSpan> CodeSpans(List<(string Path, SyntaxTree Tree)> generated, string path, SourceText text, CancellationToken cancellationToken)
    {
        var spans = new List<TextSpan>();

        foreach (var (razorPath, tree) in generated)
        {
            if (SamePath(razorPath, path) is false) continue;

            foreach (var mapping in tree.GetLineMappings(cancellationToken))
            {
                if (mapping.IsHidden || mapping.MappedSpan.HasMappedPath is false || SamePath(mapping.MappedSpan.Path, path) is false) continue;

                var start = Position(text, mapping.MappedSpan.StartLinePosition);
                var end = Position(text, mapping.MappedSpan.EndLinePosition);
                if (end > start) spans.Add(TextSpan.FromBounds(start, end));
            }
        }

        return spans;
    }

    private static int Position(SourceText text, LinePosition position)
    {
        if (position.Line >= text.Lines.Count) return text.Length;
        var line = text.Lines[position.Line];
        return Math.Min(line.Start + position.Character, line.EndIncludingLineBreak);
    }

    /// <summary>The trees the Razor compiler generated, by the .razor file each is generated from.</summary>
    private static List<(string Path, SyntaxTree Tree)> RazorTrees(Compilation compilation)
    {
        var trees = new List<(string, SyntaxTree)>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            if (RazorSourcePath(tree, CancellationToken.None) is { } path) trees.Add((path, tree));
        }
        return trees;
    }

    /// <summary>The file a tree is generated from: the Razor compiler names it in the #pragma checksum it opens the file with.</summary>
    private static string? RazorSourcePath(SyntaxTree tree, CancellationToken cancellationToken)
        => tree.GetRoot(cancellationToken)
               .GetFirstToken(includeZeroWidth: true)
               .LeadingTrivia
               .Where(t => t.IsKind(SyntaxKind.PragmaChecksumDirectiveTrivia))
               .Select(t => t.GetStructure())
               .OfType<PragmaChecksumDirectiveTriviaSyntax>()
               .Select(p => p.File.ValueText)
               .FirstOrDefault();

    /// <summary>
    /// Whether two paths name one file: the same full path, or one relative to the project (as a generator may name it)
    /// that the other ends with.
    /// </summary>
    private static bool SamePath(string a, string b)
    {
        var aRooted = IsRooted(a);
        var bRooted = IsRooted(b);
        a = NormalizePath(a, aRooted);
        b = NormalizePath(b, bRooted);
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase)
               || (bRooted is false && a.EndsWith("/" + b, StringComparison.OrdinalIgnoreCase))
               || (aRooted is false && b.EndsWith("/" + a, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRooted(string path)
    {
        try
        {
            return System.IO.Path.IsPathRooted(path);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string NormalizePath(string path, bool rooted)
    {
        try
        {
            if (rooted) path = System.IO.Path.GetFullPath(path);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or System.IO.PathTooLongException)
        {
        }
        return path.Replace('\\', '/').TrimStart('.', '/');
    }

    private static bool IsKnownType(Compilation compilation, string name)
    {
        if (compilation.GetSymbolsWithName(name, SymbolFilter.Type).Any()) return true;
        for (var arity = 0; arity <= 3; arity++)
        {
            var metadataName = arity == 0 ? $"Bit.BlazorUI.{name}" : $"Bit.BlazorUI.{name}`{arity}";
            if (compilation.GetTypeByMetadataName(metadataName) is not null) return true;
        }
        return false;
    }

    /// <summary>Blanks out @* ... *@ comments, keeping every other character where it was.</summary>
    public static string WithoutRazorComments(string text) => RazorComment.Replace(text, Blank);

    /// <summary>Blanks out @* ... *@ and &lt;!-- ... --&gt; comments, keeping every other character where it was.</summary>
    public static string WithoutComments(string text) => HtmlComment.Replace(WithoutRazorComments(text), Blank);

    private static string Blank(Match m) => new(m.Value.Select(c => c is '\r' or '\n' ? c : ' ').ToArray());

    private static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';

    /// <summary>
    /// Where a name the generated code holds in a hidden region is in the .razor file it was generated from: after the
    /// last position the generated code maps before it, the next occurrence of the name that a previous one has not
    /// already taken.
    /// </summary>
    private sealed class RazorMapping
    {
        private readonly SyntaxTree _tree;
        private readonly ImmutableArray<AdditionalText> _files;
        private readonly CancellationToken _cancellationToken;
        private List<(int GeneratedLine, LinePosition RazorStart, LinePosition RazorEnd)>? _anchors;
        private string? _path;
        private SourceText? _text;
        private string? _content;
        private int _consumed;

        public RazorMapping(SyntaxTree tree, ImmutableArray<AdditionalText> files, CancellationToken cancellationToken)
        {
            _tree = tree;
            _files = files;
            _cancellationToken = cancellationToken;
        }

        /// <summary>The component and the member of the last report, for the OldChanged of a @bind-Old.</summary>
        public (ITypeSymbol Component, string Name)? LastReported { get; set; }

        /// <summary>
        /// The position of <paramref name="name"/> in the .razor file, or null for the node's own position.
        /// <paramref name="found"/> says whether the name itself was found there, rather than only the region it is in.
        /// </summary>
        public Location? Locate(SyntaxNode node, string name, bool childTag, out bool found)
        {
            found = true;

            // Written in C# by hand, or mapped by the generator itself: the node's own position is the place.
            if (_tree.GetLineVisibility(node.SpanStart, _cancellationToken) != LineVisibility.Hidden) return null;

            found = false;
            EnsureAnchors();
            if (_path is null) return null;

            // Between the last position mapped before the name and the first one after it: the C# values of the markup
            // around it (the value bound by a @bind- is one), which keeps the search inside the tag it belongs to.
            var line = _tree.GetLineSpan(node.Span, _cancellationToken).StartLinePosition.Line;
            var anchor = new LinePosition(0, 0);
            LinePosition? bound = null;
            foreach (var a in _anchors!)
            {
                if (a.GeneratedLine > line)
                {
                    bound = a.RazorStart;
                    break;
                }
                anchor = a.RazorEnd;
            }

            if (_text is null || _content is null)
                return Location.Create(_path, default, new LinePositionSpan(anchor, anchor));

            var from = Math.Max(Position(_text, anchor), _consumed);
            var to = bound is { } b ? Math.Max(from, Position(_text, b)) : _content.Length;
            var index = Find(_content, name, from, to, childTag);
            if (index < 0)
                return Location.Create(_path, default, new LinePositionSpan(anchor, anchor));

            found = true;
            _consumed = index + name.Length;
            var span = new TextSpan(index, name.Length);
            return Location.Create(_path, span, _text.Lines.GetLinePositionSpan(span));
        }

        /// <summary>
        /// The first occurrence of <paramref name="name"/> in [<paramref name="from"/>, <paramref name="to"/>): as a child
        /// content tag (right after a &lt;, case kept), or as an attribute - the name on its own or after "@bind-", case
        /// ignored, never part of a longer name or of a dotted one.
        /// </summary>
        private static int Find(string content, string name, int from, int to, bool childTag)
        {
            var comparison = childTag ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            for (var i = from; to - i >= name.Length; i++)
            {
                i = content.IndexOf(name, i, to - i, comparison);
                if (i < 0) return -1;

                var before = i > 0 ? content[i - 1] : '\0';
                var end = i + name.Length;
                var after = end < content.Length ? content[end] : '\0';

                var isName = childTag
                    ? before == '<' && (char.IsWhiteSpace(after) || after is '/' or '>')
                    : IsWordCharacter(before) is false && before != '.' && IsWordCharacter(after) is false;

                if (isName) return i;
            }

            return -1;
        }

        private void EnsureAnchors()
        {
            if (_anchors is not null) return;
            _anchors = [];

            var mappings = _tree.GetLineMappings(_cancellationToken)
                                .Where(m => m.MappedSpan.HasMappedPath && m.IsHidden is false)
                                .ToList();

            // The .razor file the tree is generated from. Without a #pragma checksum, the file most of its positions map
            // to - not the first: the @usings of an _Imports.razor come first, mapped to that file.
            _path = RazorSourcePath(_tree, _cancellationToken);

            _path ??= mappings.GroupBy(m => m.MappedSpan.Path, StringComparer.OrdinalIgnoreCase)
                              .OrderByDescending(g => g.Count())
                              .Select(g => g.Key)
                              .FirstOrDefault();

            if (_path is null) return;

            foreach (var m in mappings)
            {
                if (SamePath(m.MappedSpan.Path, _path) is false) continue;
                _anchors.Add((m.Span.Start.Line, m.MappedSpan.StartLinePosition, m.MappedSpan.EndLinePosition));
            }
            _anchors.Sort((a, b) => a.GeneratedLine.CompareTo(b.GeneratedLine));

            var file = _files.FirstOrDefault(f => SamePath(f.Path, _path!));
            _text = file?.GetText(_cancellationToken);
            _content = _text is null ? null : WithoutComments(_text.ToString());
        }
    }
}
