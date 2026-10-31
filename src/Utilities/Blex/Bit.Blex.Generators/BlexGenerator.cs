using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bit.Blex.Generators;

/// <summary>
/// Generates the reactive plumbing for classes annotated with <c>[Bit.Blex.Store]</c>:
/// reactive properties, memoized computed accessors, named action wrappers, effect wrappers
/// (loading/error lifecycle, cancellation, concurrency modes), JSON snapshot support and the
/// <c>BlexStoreBase</c> base type.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class BlexGenerator : IIncrementalGenerator
{
    private const string BlexStoreAttribute = "Bit.Blex.BlexStoreAttribute";
    private const string BlexStateAttribute = "Bit.Blex.BlexStateAttribute";
    private const string BlexComputedAttribute = "Bit.Blex.BlexComputedAttribute";
    private const string BlexActionAttribute = "Bit.Blex.BlexActionAttribute";
    private const string BlexEffectAttribute = "Bit.Blex.BlexEffectAttribute";
    private const string CancellationTokenType = "System.Threading.CancellationToken";

    /// <summary>Fully-qualified display that keeps nullable reference annotations (e.g. <c>string?</c>).</summary>
    private static readonly SymbolDisplayFormat TypeFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>
    /// Member names the generator itself emits or inherits from <c>BlexStoreBase</c>; a state,
    /// computed, action or effect must not generate one of these.
    /// </summary>
    private static readonly string[] ReservedNames =
    [
        "Name", "Persist", "PersistVersion", "StateChanged", "PropertyChanged", "SerializeState", "DeserializeState",
        "InvalidateComputed", "NotifyRestored", "SetState", "SetEffectState",
        "BeginEffect", "EndEffect", "DebounceEffectAsync", "RunEffectAttemptsAsync", "Dispatch", "DispatchAsync",
        "BeginAction", "DispatchApprovedAsync",
        "Batch", "ResetState", "RestoreState", "IsRestoring", "IsObserved",
        "Dispose", "IsDisposed", "CancelEffects",
        "InstanceKey", "WithInstanceKey", "TimeProvider",
        "ActionDescriptors", "FindAction", "DescribeActions", "EnforcesActions", "TransientStateKeys",
        "CaptureRollbackState", "ApplyRollbackState",
    ];

    /// <summary>The nested class the typed action descriptors are generated into.</summary>
    private const string ActionsClassName = "Actions";

    /// <summary>Names C# rejects as tuple element names (CS8126), plus GetType, which would hide object's.</summary>
    private static readonly string[] ReservedTupleNames = ["CompareTo", "Deconstruct", "Equals", "GetHashCode", "GetType", "Rest", "ToString"];

    private static readonly DiagnosticDescriptor NotPartial = new(
        "BLEX001",
        "Store class must be partial",
        "Blex store '{0}' must be declared 'partial' so the generator can extend it",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor BadActionName = new(
        "BLEX002",
        "Cannot derive action name",
        "Action method '{0}' must start with 'On' or specify [BlexActionAttribute(Name = \"...\")] to derive a public action name",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor BadComputedName = new(
        "BLEX003",
        "Cannot derive computed name",
        "Computed method '{0}' must start with 'Compute' or 'Get' to derive a property name",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ComputedHasParameters = new(
        "BLEX004",
        "Computed method must be parameterless",
        "Computed method '{0}' must be parameterless so it can back a memoized property",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateMember = new(
        "BLEX005",
        "Generated member name collides",
        "Blex store '{0}' would generate more than one member named '{1}' (or collide with an existing/reserved member). Rename the conflicting state, computed, action or effect.",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedClassShape = new(
        "BLEX006",
        "Unsupported store class shape",
        "Blex store '{0}' must be a top-level, non-generic, non-static class. Nested, generic and static stores are not supported.",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EffectMustBeAsync = new(
        "BLEX007",
        "Effect method must return Task or ValueTask",
        "Effect method '{0}' must return Task or ValueTask (non-generic). Use [BlexActionAttribute] for value-returning or synchronous methods.",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MustBeInstanceMember = new(
        "BLEX008",
        "Blex member must be a writable instance member",
        "'{0}' cannot be static, const or readonly; [BlexStateAttribute]/[BlexComputedAttribute]/[BlexActionAttribute]/[BlexEffectAttribute] members must be writable instance members",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor LatestWithoutToken = new(
        "BLEX009",
        "Latest effect cannot cancel without a CancellationToken",
        "Effect '{0}' uses BlexEffectConcurrency.Latest but has no CancellationToken parameter; superseded runs will keep executing, so add a trailing CancellationToken parameter to make cancellation effective",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AsyncVoidAction = new(
        "BLEX010",
        "Action must not be 'async void'",
        "Action method '{0}' is 'async void'; return Task or ValueTask so the dispatch pipeline can await and record it correctly",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ActionReturnsValue = new(
        "BLEX011",
        "Action return value is discarded",
        "Action method '{0}' returns a value that the generated wrapper discards; use void, Task or ValueTask (store results in [BlexStateAttribute] fields instead)",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor StateFieldNameConflict = new(
        "BLEX012",
        "State field name equals generated property name",
        "State field '{0}' would generate a property with the same name; use camelCase or an underscore prefix such as '_{0}'",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedParameterModifier = new(
        "BLEX013",
        "Unsupported parameter modifier",
        "Method '{0}' has a ref/out/in parameter; action and effect parameters must be passed by value so the generated wrapper can capture them",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor GenericMethodNotSupported = new(
        "BLEX014",
        "Generic action/effect/computed methods are not supported",
        "Method '{0}' is generic; the generated member cannot carry type parameters, so make the method non-generic",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor RecordStoreNotSupported = new(
        "BLEX015",
        "Store must be a plain class",
        "Blex store '{0}' is declared as a record; stores are mutable reactive containers and must be plain (non-record) classes",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ComputedReturnsVoid = new(
        "BLEX017",
        "Computed method must return a value",
        "Computed method '{0}' returns void; it must return a value so it can back a memoized property",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MemberOutsideStore = new(
        "BLEX018",
        "Blex member attribute outside a store",
        "'{0}' is marked with a Blex member attribute but its containing type '{1}' is not a Blex store, so nothing is generated for it; add [BlexStoreAttribute] to '{1}' or remove the attribute",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NegativeDebounce = new(
        "BLEX019",
        "Effect debounce cannot be negative",
        "Effect '{0}' sets a negative DebounceMilliseconds; use 0 to disable debouncing",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ActionsNameTaken = new(
        "BLEX020",
        "Typed action descriptors are not generated",
        "Blex store '{0}' already has a member named 'Actions', so its typed action descriptors ({0}.Actions.Xxx) are not generated; rename that member to get them",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateActionLabel = new(
        "BLEX021",
        "Action name is used more than once",
        "Blex store '{0}' dispatches more than one action as '{1}'; the name identifies an action to subscribers, filters, DevTools and dispatch by name, so give each [BlexActionAttribute]/[BlexEffectAttribute] a Name of its own (a state property's direct assignment is dispatched as 'Set <Property>')",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NegativeRetryOrTimeout = new(
        "BLEX022",
        "Effect retry and timeout values cannot be negative",
        "Effect '{0}' sets a negative RetryCount, RetryDelayMilliseconds or TimeoutMilliseconds; use 0 to turn retries or the timeout off",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor TimeoutWithoutToken = new(
        "BLEX023",
        "Effect timeout needs a CancellationToken",
        "Effect '{0}' sets TimeoutMilliseconds but has no CancellationToken parameter, so a timed-out attempt could not be stopped and would go on changing state; add a trailing CancellationToken parameter and pass it to the work it awaits",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConflictingBaseType = new(
        "BLEX016",
        "Store cannot have another base class",
        "Blex store '{0}' derives from '{1}', but the generator needs to make it derive from Bit.Blex.BlexStoreBase; remove the base class or compose it instead",
        "Bit.Blex.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var stores = context.SyntaxProvider.ForAttributeWithMetadataName(
            BlexStoreAttribute,
            // Accept any type declaration so misuse (e.g. records) gets a diagnostic instead of
            // being silently ignored; Transform validates the actual shape.
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (ctx, _) => Transform(ctx));

        // A [BlexStateAttribute]/[BlexComputedAttribute]/[BlexActionAttribute]/[BlexEffectAttribute]
        // member only means something inside a [BlexStoreAttribute] class. Outside one it compiles
        // cleanly and generates nothing at all, which reads exactly like a broken generator -- so
        // say so instead of staying silent.
        foreach (var memberAttribute in new[] { BlexStateAttribute, BlexComputedAttribute, BlexActionAttribute, BlexEffectAttribute })
        {
            var orphans = context.SyntaxProvider.ForAttributeWithMetadataName(
                memberAttribute,
                predicate: static (_, _) => true,
                transform: static (ctx, _) => DetectOrphan(ctx));

            context.RegisterSourceOutput(orphans, static (spc, diagnostic) =>
            {
                if (diagnostic is not null)
                    spc.ReportDiagnostic(diagnostic.ToDiagnostic());
            });
        }

        context.RegisterSourceOutput(stores, static (spc, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
                spc.ReportDiagnostic(diagnostic.ToDiagnostic());

            if (result.Model is { } model)
            {
                // Include the namespace in the hint name so equally-named stores in different
                // namespaces don't collide.
                var hint = model.Namespace is null
                    ? $"{model.ClassName}.Blex.g.cs"
                    : $"{model.Namespace}.{model.ClassName}.Blex.g.cs";
                spc.AddSource(hint, SourceText.From(Emit(model), Encoding.UTF8));
            }
        });
    }

    /// <summary>
    /// Reports BLEX018 when a Blex member attribute sits on a member whose containing type is not
    /// a <c>[BlexStoreAttribute]</c> class. Returns <c>null</c> for the normal (in-store) case.
    /// </summary>
    private static BlexDiagnosticInfo? DetectOrphan(GeneratorAttributeSyntaxContext ctx)
    {
        var member = ctx.TargetSymbol;
        var container = member.ContainingType;
        if (container is not null && HasAttribute(container, BlexStoreAttribute))
            return null;

        return new BlexDiagnosticInfo(
            MemberOutsideStore,
            BlexLocationInfo.From(member.Locations.FirstOrDefault()),
            new BlexEquatableArray<string>([member.Name, container?.Name ?? "<global>"]));
    }

    private static BlexTransformOutput Transform(GeneratorAttributeSyntaxContext ctx)
    {
        var diagnostics = new List<BlexDiagnosticInfo>();
        var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var classDecl = (TypeDeclarationSyntax)ctx.TargetNode;

        void Report(DiagnosticDescriptor descriptor, Location? location, params string[] args)
            => diagnostics.Add(new BlexDiagnosticInfo(descriptor, BlexLocationInfo.From(location), new BlexEquatableArray<string>(args)));

        BlexTransformOutput Fail() => new(null, new BlexEquatableArray<BlexDiagnosticInfo>(diagnostics.ToArray()));

        // Records get value-equality members and a synthesized clone that fight the mutable
        // reactive container model; require a plain class.
        if (symbol.IsRecord || classDecl is not ClassDeclarationSyntax)
        {
            Report(RecordStoreNotSupported, classDecl.Identifier.GetLocation(), symbol.Name);
            return Fail();
        }

        if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            Report(NotPartial, classDecl.Identifier.GetLocation(), symbol.Name);
            return Fail();
        }

        // The emitter writes a flat, top-level partial declaration with instance members, so
        // nested, generic and static stores would produce broken output. Fail with a clear
        // diagnostic instead.
        if (symbol.ContainingType is not null || symbol.IsGenericType || symbol.IsStatic)
        {
            Report(UnsupportedClassShape, classDecl.Identifier.GetLocation(), symbol.Name);
            return Fail();
        }

        // The generated partial adds ": Bit.Blex.BlexStoreBase"; an existing different base class would
        // produce an uncompilable "inherits from two classes" error deep in generated code.
        if (symbol.BaseType is { SpecialType: not SpecialType.System_Object } baseType
            && baseType.ToDisplayString() != "Bit.Blex.BlexStoreBase")
        {
            Report(ConflictingBaseType, classDecl.Identifier.GetLocation(), symbol.Name, baseType.Name);
            return Fail();
        }

        var ns = symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : symbol.ContainingNamespace.ToDisplayString();

        var storeName = symbol.Name;
        var persist = false;
        var version = 0;
        var enforceActions = false;
        var rollbackOnError = false;
        var storeAttr = ctx.Attributes.FirstOrDefault();
        if (storeAttr is not null)
        {
            var named = storeAttr.NamedArguments.FirstOrDefault(a => a.Key == "Name").Value;
            if (named.Value is string s && !string.IsNullOrWhiteSpace(s))
                storeName = s;

            var persistArg = storeAttr.NamedArguments.FirstOrDefault(a => a.Key == "Persist").Value;
            if (persistArg.Value is bool b)
                persist = b;

            var versionArg = storeAttr.NamedArguments.FirstOrDefault(a => a.Key == "Version").Value;
            if (versionArg.Value is int ver)
                version = ver;

            var enforceArg = storeAttr.NamedArguments.FirstOrDefault(a => a.Key == "EnforceActions").Value;
            if (enforceArg.Value is bool e)
                enforceActions = e;

            var rollbackArg = storeAttr.NamedArguments.FirstOrDefault(a => a.Key == "RollbackOnError").Value;
            if (rollbackArg.Value is bool rb)
                rollbackOnError = rb;
        }

        var states = new List<BlexStateModel>();
        var computeds = new List<BlexComputedModel>();
        var actions = new List<BlexActionModel>();
        var effects = new List<BlexEffectModel>();

        foreach (var member in symbol.GetMembers())
        {
            switch (member)
            {
                case IFieldSymbol field when HasAttribute(field, BlexStateAttribute):
                {
                    if (field.IsStatic || field.IsConst || field.IsReadOnly)
                    {
                        Report(MustBeInstanceMember, field.Locations.FirstOrDefault(), field.Name);
                        break;
                    }

                    var propertyName = ToPropertyName(field.Name);
                    if (propertyName == field.Name)
                    {
                        Report(StateFieldNameConflict, field.Locations.FirstOrDefault(), field.Name);
                        break;
                    }

                    // [BlexState(Persist = false)] keeps the field out of durable persistence.
                    var stateAttr = field.GetAttributes().First(a => a.AttributeClass?.ToDisplayString() == BlexStateAttribute);
                    var persistField = stateAttr.NamedArguments.FirstOrDefault(a => a.Key == "Persist").Value.Value is not false;

                    states.Add(new BlexStateModel(
                        field.Name,
                        propertyName,
                        field.Type.ToDisplayString(TypeFormat),
                        persistField));
                    break;
                }

                case IMethodSymbol cm when cm.MethodKind == MethodKind.Ordinary && HasAttribute(cm, BlexComputedAttribute):
                {
                    if (cm.IsStatic)
                    {
                        Report(MustBeInstanceMember, cm.Locations.FirstOrDefault(), cm.Name);
                        break;
                    }

                    if (cm.Parameters.Length > 0)
                    {
                        Report(ComputedHasParameters, cm.Locations.FirstOrDefault(), cm.Name);
                        break;
                    }

                    // The memoized property cannot carry a type parameter: emitted anyway, it fails to compile in
                    // generated code the user never wrote (CS0246 / CS0411).
                    if (cm.IsGenericMethod)
                    {
                        Report(GenericMethodNotSupported, cm.Locations.FirstOrDefault(), cm.Name);
                        break;
                    }

                    // A void computed would emit `public void Xxx { get { ... } }`, which does not
                    // compile; the memoized property needs something to memoize.
                    if (cm.ReturnsVoid)
                    {
                        Report(ComputedReturnsVoid, cm.Locations.FirstOrDefault(), cm.Name);
                        break;
                    }

                    var propName = ToComputedName(cm.Name);
                    if (propName is null)
                    {
                        Report(BadComputedName, cm.Locations.FirstOrDefault(), cm.Name);
                        break;
                    }

                    computeds.Add(new BlexComputedModel(
                        cm.Name,
                        propName,
                        cm.ReturnType.ToDisplayString(TypeFormat)));
                    break;
                }

                case IMethodSymbol am when am.MethodKind == MethodKind.Ordinary && HasAttribute(am, BlexActionAttribute):
                {
                    if (am.IsStatic)
                    {
                        Report(MustBeInstanceMember, am.Locations.FirstOrDefault(), am.Name);
                        break;
                    }

                    if (am.IsAsync && am.ReturnsVoid)
                    {
                        Report(AsyncVoidAction, am.Locations.FirstOrDefault(), am.Name);
                        break;
                    }

                    if (am.IsGenericMethod)
                    {
                        Report(GenericMethodNotSupported, am.Locations.FirstOrDefault(), am.Name);
                        break;
                    }

                    var attr = am.GetAttributes().First(a => a.AttributeClass?.ToDisplayString() == BlexActionAttribute);
                    var explicitName = attr.NamedArguments
                        .FirstOrDefault(a => a.Key == "Name").Value.Value as string;

                    var wrapper = DeriveWrapperName(am.Name, explicitName);
                    if (wrapper is null)
                    {
                        Report(BadActionName, am.Locations.FirstOrDefault(), am.Name);
                        break;
                    }

                    var isAsync = IsTaskReturning(am.ReturnType);
                    if (ReturnsDiscardedValue(am, isAsync))
                        Report(ActionReturnsValue, am.Locations.FirstOrDefault(), am.Name);

                    // The label is for display (DevTools / time-travel) and may contain spaces;
                    // the wrapper is a C# identifier and is kept separate.
                    var label = string.IsNullOrWhiteSpace(explicitName) ? wrapper : explicitName!;

                    var isValueTask = IsValueTaskReturning(am.ReturnType);
                    var prms = BuildParams(am, am.Parameters, Report);
                    if (prms is null)
                        break;

                    actions.Add(new BlexActionModel(
                        am.Name,
                        wrapper,
                        label,
                        isAsync,
                        isValueTask,
                        new BlexEquatableArray<BlexParamModel>(prms)));
                    break;
                }

                case IMethodSymbol em when em.MethodKind == MethodKind.Ordinary && HasAttribute(em, BlexEffectAttribute):
                {
                    if (em.IsStatic)
                    {
                        Report(MustBeInstanceMember, em.Locations.FirstOrDefault(), em.Name);
                        break;
                    }

                    var attr = em.GetAttributes().First(a => a.AttributeClass?.ToDisplayString() == BlexEffectAttribute);
                    var explicitName = attr.NamedArguments
                        .FirstOrDefault(a => a.Key == "Name").Value.Value as string;

                    var wrapper = DeriveWrapperName(em.Name, explicitName);
                    if (wrapper is null)
                    {
                        Report(BadActionName, em.Locations.FirstOrDefault(), em.Name);
                        break;
                    }

                    if (em.IsGenericMethod)
                    {
                        Report(GenericMethodNotSupported, em.Locations.FirstOrDefault(), em.Name);
                        break;
                    }

                    // Effects must be task-returning with no result so the lifecycle wrapper can
                    // own loading/error without losing a return value.
                    var returnName = em.ReturnType.ToDisplayString();
                    var isTask = returnName == "System.Threading.Tasks.Task" || returnName == "System.Threading.Tasks.ValueTask";
                    if (!isTask)
                    {
                        Report(EffectMustBeAsync, em.Locations.FirstOrDefault(), em.Name);
                        break;
                    }

                    var concurrency = 0;
                    var concurrencyArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "Concurrency").Value;
                    if (concurrencyArg.Value is int c)
                        concurrency = c;

                    var debounce = 0;
                    var debounceArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "DebounceMilliseconds").Value;
                    if (debounceArg.Value is int d)
                        debounce = d;

                    // Task.Delay rejects a negative delay at runtime, deep inside generated code.
                    if (debounce < 0)
                    {
                        Report(NegativeDebounce, em.Locations.FirstOrDefault(), em.Name);
                        break;
                    }

                    var retryCount = attr.NamedArguments.FirstOrDefault(a => a.Key == "RetryCount").Value.Value is int r ? r : 0;
                    var retryDelay = attr.NamedArguments.FirstOrDefault(a => a.Key == "RetryDelayMilliseconds").Value.Value is int rd ? rd : 1000;
                    var timeout = attr.NamedArguments.FirstOrDefault(a => a.Key == "TimeoutMilliseconds").Value.Value is int t ? t : 0;
                    if (retryCount < 0 || retryDelay < 0 || timeout < 0)
                    {
                        Report(NegativeRetryOrTimeout, em.Locations.FirstOrDefault(), em.Name);
                        break;
                    }

                    // A trailing CancellationToken parameter is supplied by the wrapper (it does
                    // not appear on the public method) and enables CancelXxx() / Latest semantics.
                    var parameters = em.Parameters;
                    var hasToken = parameters.Length > 0
                        && parameters[parameters.Length - 1].Type.ToDisplayString() == CancellationTokenType;
                    if (hasToken)
                        parameters = System.Collections.Immutable.ImmutableArray.CreateRange(parameters.Take(parameters.Length - 1));

                    if (concurrency == 1 && !hasToken)
                        Report(LatestWithoutToken, em.Locations.FirstOrDefault(), em.Name);

                    // Without a token, a timeout could only stop waiting for the body, which would carry on
                    // and change state after its run had already failed.
                    if (timeout > 0 && !hasToken)
                    {
                        Report(TimeoutWithoutToken, em.Locations.FirstOrDefault(), em.Name);
                        break;
                    }

                    var label = string.IsNullOrWhiteSpace(explicitName) ? wrapper : explicitName!;
                    var isValueTaskEffect = returnName == "System.Threading.Tasks.ValueTask";
                    var prms = BuildParams(em, parameters, Report);
                    if (prms is null)
                        break;

                    effects.Add(new BlexEffectModel(
                        em.Name,
                        wrapper,
                        label,
                        isValueTaskEffect,
                        concurrency,
                        hasToken,
                        debounce,
                        retryCount,
                        retryDelay,
                        timeout,
                        new BlexEquatableArray<BlexParamModel>(prms)));
                    break;
                }
            }
        }

        // A name collision would emit uncompilable code; report BLEX005 and emit nothing.
        var beforeDuplicates = diagnostics.Count;
        var memberNames = DetectDuplicates(storeName, states, computeds, actions, effects, symbol, Report);
        if (diagnostics.Count > beforeDuplicates)
            return Fail();

        DetectDuplicateLabels(storeName, states, actions, effects, enforceActions, symbol, Report);

        // The typed descriptors live in a nested "Actions" class. A store that already uses the name --
        // for one of its own members or one generated for it -- keeps compiling exactly as before; it
        // only goes without the typed surface, and is told so.
        var emitActions = actions.Count + effects.Count > 0;
        if (emitActions && memberNames.Contains(ActionsClassName))
        {
            Report(ActionsNameTaken, classDecl.Identifier.GetLocation(), symbol.Name);
            emitActions = false;
        }

        var tracked = TrackComputedReads(symbol, ctx.SemanticModel.Compilation, states, computeds);

        var model = new BlexStoreModel(
            ns,
            symbol.Name,
            storeName,
            persist,
            version,
            enforceActions,
            rollbackOnError,
            emitActions,
            new BlexEquatableArray<BlexStateModel>(states.ToArray()),
            new BlexEquatableArray<BlexComputedModel>(tracked),
            new BlexEquatableArray<BlexActionModel>(actions.ToArray()),
            new BlexEquatableArray<BlexEffectModel>(effects.ToArray()));

        return new BlexTransformOutput(model, new BlexEquatableArray<BlexDiagnosticInfo>(diagnostics.ToArray()));
    }

    /// <summary>
    /// Finds which state properties each computed reads, so a write invalidates only the computeds that read it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The state properties a computed reads (<c>Count</c>) are generated by this very generator, so the semantic model
    /// cannot resolve them -- nor anything chained off them. Only the root names of expressions are looked at: a state
    /// property or its backing field, or another computed, is a dependency; what is accessed off it (<c>Items.Sum</c>'s
    /// <c>Sum</c>) says nothing about which state is read.
    /// </para>
    /// <para>
    /// Anything else makes the computed read "everything" -- invalidated by every write, as all computeds were before:
    /// an instance member that is not state (a helper method, a service field, a base class's member), an extension
    /// called on the store, a primary-constructor parameter, <c>this</c> passed along, <c>base</c>, or a name nothing
    /// resolves (an effect's generated flags). Only what cannot be store state is left out: locals, lambda and
    /// local-function parameters, local functions, types, namespaces and static members.
    /// </para>
    /// </remarks>
    private static BlexComputedModel[] TrackComputedReads(
        INamedTypeSymbol store,
        Compilation compilation,
        List<BlexStateModel> states,
        List<BlexComputedModel> computeds)
    {
        var stateByName = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var state in states)
        {
            stateByName[state.PropertyName] = state.PropertyName;
            stateByName[state.FieldName] = state.PropertyName;
        }

        var computedByName = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < computeds.Count; i++)
        {
            computedByName[computeds[i].PropertyName] = i;
            computedByName[computeds[i].MethodName] = i;
        }

        var direct = new (HashSet<string> States, HashSet<int> Computeds, bool Everything)[computeds.Count];
        for (var i = 0; i < computeds.Count; i++)
        {
            var method = store.GetMembers(computeds[i].MethodName).OfType<IMethodSymbol>().FirstOrDefault(m => m.Parameters.Length == 0);
            direct[i] = method is null ? ([], [], true) : ReadsOf(method);
        }

        // Through the computeds a computed reads; a cycle (which would recurse forever at run time) reads everything.
        var resolved = new (SortedSet<string> States, bool Everything)?[computeds.Count];
        var visiting = new HashSet<int>();
        var result = new BlexComputedModel[computeds.Count];
        for (var i = 0; i < computeds.Count; i++)
        {
            var (reads, everything) = Resolve(i);
            result[i] = computeds[i] with
            {
                Reads = new BlexEquatableArray<string>(everything ? [] : [.. reads]),
                ReadsEverything = everything,
            };
        }

        return result;

        (SortedSet<string> States, bool Everything) Resolve(int index)
        {
            if (resolved[index] is { } done)
                return done;

            if (!visiting.Add(index))
                return ([], true);

            var (statesRead, computedsRead, everything) = direct[index];
            var reads = new SortedSet<string>(statesRead, StringComparer.Ordinal);
            foreach (var other in computedsRead)
            {
                if (other == index || everything)
                    continue;

                var (otherReads, otherEverything) = Resolve(other);
                everything |= otherEverything;
                reads.UnionWith(otherReads);
            }

            visiting.Remove(index);
            var outcome = (reads, everything);
            resolved[index] = outcome;
            return outcome;
        }

        (HashSet<string> States, HashSet<int> Computeds, bool Everything) ReadsOf(IMethodSymbol method)
        {
            var statesRead = new HashSet<string>(StringComparer.Ordinal);
            var computedsRead = new HashSet<int>();
            var everything = false;

            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is not MethodDeclarationSyntax declaration
                    || ((SyntaxNode?)declaration.Body ?? declaration.ExpressionBody) is not { } body)
                {
                    everything = true;
                    continue;
                }

                var semantic = compilation.GetSemanticModel(declaration.SyntaxTree);
                foreach (var node in body.DescendantNodesAndSelf())
                {
                    switch (node)
                    {
                        case BaseExpressionSyntax:
                        case ThisExpressionSyntax when node.Parent is not MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax }:
                            everything = true;
                            break;

                        case SimpleNameSyntax name when IsRootName(name):
                            Classify(name, semantic);
                            break;
                    }

                    if (everything)
                        break;
                }
            }

            return (statesRead, computedsRead, everything);

            void Classify(SimpleNameSyntax name, SemanticModel semantic)
            {
                var text = name.Identifier.ValueText;
                if (stateByName.TryGetValue(text, out var state))
                {
                    statesRead.Add(state);
                    return;
                }

                if (computedByName.TryGetValue(text, out var computed))
                {
                    computedsRead.Add(computed);
                    return;
                }

                switch (semantic.GetSymbolInfo(name).Symbol)
                {
                    case null:
                        // Nothing resolves it: a member generated for the store (an effect's flags), whose reads
                        // cannot be seen.
                        everything = true;
                        break;

                    case ILocalSymbol or IRangeVariableSymbol or ITypeSymbol or INamespaceSymbol or ILabelSymbol or IAliasSymbol or IDiscardSymbol:
                        break;

                    case IParameterSymbol parameter:
                        // A primary-constructor parameter is a field in all but name: whatever it holds is not tracked.
                        if (parameter.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor
                            && SymbolEqualityComparer.Default.Equals(constructor.ContainingType, store))
                        {
                            everything = true;
                        }

                        break;

                    case IMethodSymbol { MethodKind: MethodKind.LocalFunction or MethodKind.AnonymousFunction }:
                        break;

                    // An extension called on the store (this.Total()) reads whatever it likes off it.
                    case IMethodSymbol { MethodKind: MethodKind.ReducedExtension }:
                    case IMethodSymbol { IsExtensionMethod: true }:
                        everything = true;
                        break;

                    case { IsStatic: true }:
                        break;

                    // A member the computed only writes -- an evaluation counter it bumps, a value it stashes -- as a
                    // statement of its own: whatever the member holds, the value the computed returns cannot depend on
                    // it. Reading it back anywhere else in the body is classified like any other read.
                    case IFieldSymbol or IPropertySymbol when IsDiscardedWrite(name):
                        break;

                    // Anything else -- an instance member of the store or a base class it declares, an extension member,
                    // a member of another object named without one -- could read the store's state in ways not seen here.
                    default:
                        everything = true;
                        break;
                }
            }
        }

        // The start of an expression -- not the member accessed off something (a.B, a?.B), the name of a named
        // argument or a property pattern, or the right side of a qualified name. "this.B" counts: B is the store's.
        static bool IsRootName(SimpleNameSyntax name) => name.Parent switch
        {
            MemberAccessExpressionSyntax access when access.Name == name => access.Expression is ThisExpressionSyntax,
            MemberBindingExpressionSyntax => false,
            NameColonSyntax => false,
            QualifiedNameSyntax qualified when qualified.Right == name => false,
            _ => true,
        };

        // X++, --X, X = ..., X += ... (or this.X ...) as an expression statement: the old value only feeds the
        // new one, and the result of the expression is thrown away.
        static bool IsDiscardedWrite(SimpleNameSyntax name)
        {
            ExpressionSyntax target = name.Parent is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access && access.Name == name
                ? access
                : name;

            return target.Parent switch
            {
                PostfixUnaryExpressionSyntax { Parent: ExpressionStatementSyntax } postfix
                    => postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression),
                PrefixUnaryExpressionSyntax { Parent: ExpressionStatementSyntax } prefix
                    => prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression),
                AssignmentExpressionSyntax { Parent: ExpressionStatementSyntax } assignment
                    => assignment.Left == target,
                _ => false,
            };
        }
    }

    /// <summary>
    /// Builds parameter models for a wrapper, propagating default values and <c>params</c>.
    /// Returns null (and reports BLEX013) when a by-ref parameter makes wrapping impossible.
    /// </summary>
    private static BlexParamModel[]? BuildParams(
        IMethodSymbol method,
        System.Collections.Immutable.ImmutableArray<IParameterSymbol> parameters,
        System.Action<DiagnosticDescriptor, Location?, string[]> report)
    {
        var models = new BlexParamModel[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (p.RefKind != RefKind.None)
            {
                report(UnsupportedParameterModifier, method.Locations.FirstOrDefault(), [method.Name]);
                return null;
            }

            models[i] = new BlexParamModel(
                p.Type.ToDisplayString(TypeFormat),
                p.Name,
                FormatDefaultValue(p),
                p.IsParams);
        }

        return models;
    }

    /// <summary>Formats a parameter's explicit default value as a C# literal, or null when none.</summary>
    private static string? FormatDefaultValue(IParameterSymbol p)
    {
        if (!p.HasExplicitDefaultValue)
            return null;

        var value = p.ExplicitDefaultValue;
        if (value is null)
            return p.Type.IsValueType ? "default" : "null";

        // Unwrap Nullable<T> so enum defaults emit a proper cast: `Mode? m = (Mode)1` compiles,
        // `Mode? m = 1` does not.
        var type = p.Type;
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            type = nullable.TypeArguments[0];

        if (type.TypeKind == TypeKind.Enum)
            return $"({type.ToDisplayString(TypeFormat)})({System.Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)})";

        return value switch
        {
            bool b => b ? "true" : "false",
            string s => SymbolDisplay.FormatLiteral(s, quote: true),
            char c => SymbolDisplay.FormatLiteral(c, quote: true),
            float f when float.IsNaN(f) => "float.NaN",
            float f when float.IsPositiveInfinity(f) => "float.PositiveInfinity",
            float f when float.IsNegativeInfinity(f) => "float.NegativeInfinity",
            float f => f.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "F",
            double d when double.IsNaN(d) => "double.NaN",
            double d when double.IsPositiveInfinity(d) => "double.PositiveInfinity",
            double d when double.IsNegativeInfinity(d) => "double.NegativeInfinity",
            double d => d.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "D",
            decimal m => m.ToString(System.Globalization.CultureInfo.InvariantCulture) + "M",
            long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L",
            ulong ul => ul.ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL",
            uint ui => ui.ToString(System.Globalization.CultureInfo.InvariantCulture) + "U",
            _ => System.Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "default",
        };
    }

    private static bool ReturnsDiscardedValue(IMethodSymbol method, bool isAsync)
    {
        if (method.ReturnsVoid)
            return false;

        var name = method.ReturnType.ToDisplayString();
        if (name == "System.Threading.Tasks.Task" || name == "System.Threading.Tasks.ValueTask")
            return false;

        // Task<T> / ValueTask<T> results and plain sync return values are dropped by the wrapper.
        return isAsync
            ? name.StartsWith("System.Threading.Tasks.Task<", System.StringComparison.Ordinal)
                || name.StartsWith("System.Threading.Tasks.ValueTask<", System.StringComparison.Ordinal)
            : true;
    }

    /// <summary>
    /// Reports BLEX005 for every generated member that would collide, and returns the names of every
    /// member the store ends up with: its own, the base class's and the generated ones.
    /// </summary>
    private static HashSet<string> DetectDuplicates(
        string storeName,
        List<BlexStateModel> states,
        List<BlexComputedModel> computeds,
        List<BlexActionModel> actions,
        List<BlexEffectModel> effects,
        INamedTypeSymbol symbol,
        System.Action<DiagnosticDescriptor, Location?, string[]> report)
    {
        var seen = new HashSet<string>(System.StringComparer.Ordinal);

        // Generated members must not collide with base-class/reserved members...
        foreach (var reserved in ReservedNames)
            seen.Add(reserved);

        // ...nor with anything the user already declared on the store.
        foreach (var member in symbol.GetMembers())
        {
            if (!member.IsImplicitlyDeclared)
                seen.Add(member.Name);
        }

        void Check(string name)
        {
            if (!seen.Add(name))
                report(DuplicateMember, symbol.Locations.FirstOrDefault(), [storeName, name]);
        }

        foreach (var s in states)
            Check(s.PropertyName);
        foreach (var c in computeds)
        {
            Check(c.PropertyName);
            Check($"__{c.PropertyName}Valid");
            Check($"__{c.PropertyName}Value");
        }

        foreach (var a in actions)
            Check(a.WrapperName);
        foreach (var e in effects)
        {
            Check(e.WrapperName);
            Check(e.WrapperName + "IsLoading");
            Check(e.WrapperName + "Error");
            Check($"__{e.WrapperName}Pending");
            Check($"__{e.WrapperName}Error");
            if (e.HasCancellationToken || e.DebounceMilliseconds > 0)
                Check("Cancel" + e.WrapperName);
            if (e.HasCancellationToken)
                Check($"__{e.WrapperName}Cts");

            if (e.Concurrency == 1)
                Check($"__{e.WrapperName}Version");
            if (e.Concurrency == 3)
                Check($"__{e.WrapperName}Queue");
            if (e.DebounceMilliseconds > 0)
                Check($"__{e.WrapperName}Debounce");
        }

        // Private plumbing behind DescribeActions and TransientStateKeys.
        Check("__blexActionDescriptors");
        if (states.Any(s => !s.Persist))
            Check("__blexTransientKeys");

        return seen;
    }

    /// <summary>
    /// Reports BLEX021 when two actions are dispatched under one name. The name is what identifies an
    /// action to subscribers, filters, DevTools and dispatch by name, and wrapper names alone cannot
    /// catch it: a display label (<c>Name = "Save it"</c>) is free text, and a state property's direct
    /// assignment is recorded as <c>"Set X"</c> -- except in a store that enforces actions, which records none.
    /// </summary>
    private static void DetectDuplicateLabels(
        string storeName,
        List<BlexStateModel> states,
        List<BlexActionModel> actions,
        List<BlexEffectModel> effects,
        bool enforceActions,
        INamedTypeSymbol symbol,
        System.Action<DiagnosticDescriptor, Location?, string[]> report)
    {
        var seen = new HashSet<string>(System.StringComparer.Ordinal);
        if (!enforceActions)
        {
            foreach (var s in states)
                seen.Add("Set " + s.PropertyName);
        }

        foreach (var label in actions.Select(a => a.ActionLabel).Concat(effects.Select(e => e.ActionLabel)))
        {
            if (!seen.Add(label))
                report(DuplicateActionLabel, symbol.Locations.FirstOrDefault(), [storeName, label]);
        }
    }

    private static bool HasAttribute(ISymbol symbol, string fullName)
        => symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == fullName);

    private static bool IsTaskReturning(ITypeSymbol type)
    {
        var name = type.ToDisplayString();
        return name == "System.Threading.Tasks.Task"
            || name.StartsWith("System.Threading.Tasks.Task<", System.StringComparison.Ordinal)
            || name == "System.Threading.Tasks.ValueTask"
            || name.StartsWith("System.Threading.Tasks.ValueTask<", System.StringComparison.Ordinal);
    }

    private static bool IsValueTaskReturning(ITypeSymbol type)
    {
        var name = type.ToDisplayString();
        return name == "System.Threading.Tasks.ValueTask"
            || name.StartsWith("System.Threading.Tasks.ValueTask<", System.StringComparison.Ordinal);
    }

    private static string ToPropertyName(string fieldName)
    {
        var trimmed = fieldName.TrimStart('_');
        if (trimmed.Length == 0)
            trimmed = fieldName;
        if (trimmed.Length == 0 || !(char.IsLetter(trimmed[0]) || trimmed[0] == '_'))
            return "_" + trimmed;
        return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
    }

    private static string? ToComputedName(string methodName)
    {
        string? core = null;
        if (methodName.StartsWith("Compute", System.StringComparison.Ordinal) && methodName.Length > 7)
            core = methodName.Substring(7);
        else if (methodName.StartsWith("Get", System.StringComparison.Ordinal) && methodName.Length > 3)
            core = methodName.Substring(3);

        if (core is null || core.Length == 0)
            return null;
        return char.ToUpperInvariant(core[0]) + core.Substring(1);
    }

    private static string? DeriveWrapperName(string methodName, string? explicitName)
    {
        // An explicit name may be used as the wrapper identifier only when it is a valid
        // C# identifier. Otherwise it is treated purely as a display label and the wrapper
        // is derived from the conventional "On" prefix.
        if (!string.IsNullOrWhiteSpace(explicitName) && IsValidIdentifier(explicitName!))
            return explicitName;
        if (methodName.StartsWith("On", System.StringComparison.Ordinal) && methodName.Length > 2
            && char.IsUpper(methodName[2]))
            return methodName.Substring(2);
        return null;
    }

    private static bool IsValidIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        if (!(char.IsLetter(value[0]) || value[0] == '_'))
            return false;
        for (var i = 1; i < value.Length; i++)
        {
            if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_'))
                return false;
        }

        // Reserved keywords ("lock", "class", ...) would emit `public void lock()`. Contextual
        // keywords ("value", "async") are fine as identifiers.
        return SyntaxFacts.GetKeywordKind(value) == SyntaxKind.None;
    }

    /// <summary>
    /// Escapes a string for embedding inside a C# string literal (quotes, backslashes, control
    /// characters, ...).
    /// </summary>
    private static string Escape(string value)
    {
        var quoted = SymbolDisplay.FormatLiteral(value, quote: true);
        return quoted.Substring(1, quoted.Length - 2);
    }

    /// <summary>
    /// Escapes a string for embedding inside an XML doc comment: XML entities are encoded and
    /// control characters (which would break out of the <c>///</c> line) become spaces.
    /// </summary>
    private static string DocEscape(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '&': sb.Append("&amp;"); break;
                default: sb.Append(char.IsControl(ch) ? ' ' : ch); break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes an identifier with <c>@</c> when it is a reserved keyword (a legal parameter or
    /// field can be declared as <c>@lock</c>, and the symbol name comes back as <c>lock</c>).
    /// </summary>
    private static string Id(string name)
        => SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;

    /// <summary>Renders a wrapper's parameter list, preserving params modifiers and default values.</summary>
    private static string ParamList(BlexEquatableArray<BlexParamModel> parameters)
        => string.Join(", ", parameters.Select(p =>
            $"{(p.IsParams ? "params " : "")}{p.TypeFqn} {Id(p.Name)}{(p.DefaultLiteral is null ? "" : " = " + p.DefaultLiteral)}"));

    /// <summary>Builds the BlexActionArg[] expression for a wrapper's parameters, or null when parameterless.</summary>
    private static string? ArgsExpression(BlexEquatableArray<BlexParamModel> parameters)
    {
        if (parameters.Count == 0)
            return null;

        var items = string.Join(", ", parameters.Select(p =>
            $"new global::Bit.Blex.BlexActionArg(\"{Escape(p.Name)}\", {Id(p.Name)})"));
        return $"IsObserved ? new global::Bit.Blex.BlexActionArg[] {{ {items} }} : null";
    }

    /// <summary>
    /// Emits the nested <c>Actions</c> class -- one typed descriptor per action and effect -- and the
    /// <c>DescribeActions</c> override that hands those, plus a <c>"Set X"</c> descriptor per assignable
    /// state property, to the runtime for dispatch by name.
    /// </summary>
    private static void EmitActionDescriptors(StringBuilder sb, BlexStoreModel m, string body)
    {
        const string Descriptor = "global::Bit.Blex.BlexActionDescriptor";

        var storeType = m.Namespace is null ? $"global::{m.ClassName}" : $"global::{m.Namespace}.{m.ClassName}";
        var described = new List<(string Wrapper, string Label, string Type, string Arguments, string Signature)>();

        foreach (var a in m.Actions)
        {
            var (type, arguments) = DescribeAction(storeType, a.ActionLabel, a.WrapperName, a.IsAsync, a.Parameters);
            described.Add((a.WrapperName, a.ActionLabel, type, arguments, Signature(a.WrapperName, a.Parameters)));
        }

        foreach (var e in m.Effects)
        {
            var (type, arguments) = DescribeAction(storeType, e.ActionLabel, e.WrapperName, returnsTask: true, e.Parameters);
            described.Add((e.WrapperName, e.ActionLabel, type, arguments, Signature(e.WrapperName, e.Parameters)));
        }

        var entries = new List<string>();
        if (m.EmitActions)
        {
            sb.AppendLine($"{body}/// <summary>");
            sb.AppendLine($"{body}/// Typed descriptors of this store's actions and effects: match them in subscribers, filters and");
            sb.AppendLine($"{body}/// tests by descriptor rather than by name, read their payloads as the types they take, and");
            sb.AppendLine($"{body}/// dispatch them as data.");
            sb.AppendLine($"{body}/// </summary>");
            sb.AppendLine($"{body}public static class {ActionsClassName}");
            sb.AppendLine($"{body}{{");
            foreach (var d in described)
            {
                sb.AppendLine($"{body}    /// <summary>The <c>{DocEscape(d.Label)}</c> action: <c>{DocEscape(d.Signature)}</c>.</summary>");
                sb.AppendLine($"{body}    public static readonly {d.Type} {d.Wrapper} = new({d.Arguments});");
                sb.AppendLine();
            }

            sb.AppendLine($"{body}}}");
            sb.AppendLine();

            entries.AddRange(described.Select(d => $"{ActionsClassName}.{d.Wrapper}"));
        }
        else
        {
            // The name "Actions" is taken (BLEX020): no typed surface, but the actions stay
            // dispatchable by name.
            entries.AddRange(described.Select(d => $"new {d.Type}({d.Arguments})"));
        }

        // A direct assignment records as "Set X", so it is replayable and dispatchable too -- except in
        // a store that enforces actions, where no outside assignment can ever have been recorded.
        if (!m.EnforceActions)
        {
            foreach (var s in m.States)
            {
                var label = Escape("Set " + s.PropertyName);
                var name = Escape(s.PropertyName);
                entries.Add(
                    $"new global::Bit.Blex.BlexAction<{storeType}, {s.TypeFqn}>(\"{label}\", new string[] {{ \"{name}\" }}, " +
                    $"static (__s, __p) => {{ __s.{s.PropertyName} = __p; return global::System.Threading.Tasks.Task.CompletedTask; }}, " +
                    $"static __a => {Descriptor}.GetArg<{s.TypeFqn}>(__a, 0, \"{name}\"))");
            }
        }

        if (entries.Count == 0)
            return;

        sb.AppendLine($"{body}private static {Descriptor}[]? __blexActionDescriptors;");
        sb.AppendLine($"{body}/// <inheritdoc />");
        sb.AppendLine($"{body}protected override global::System.Collections.Generic.IReadOnlyList<{Descriptor}> DescribeActions()");
        sb.AppendLine($"{body}    => __blexActionDescriptors ??= new {Descriptor}[]");
        sb.AppendLine($"{body}    {{");
        foreach (var entry in entries)
            sb.AppendLine($"{body}        {entry},");
        sb.AppendLine($"{body}    }};");
        sb.AppendLine();

        static string Signature(string wrapper, BlexEquatableArray<BlexParamModel> parameters)
            => $"{wrapper}({string.Join(", ", parameters.Select(p => $"{ShortType(p.TypeFqn)} {p.Name}"))})";

        static string ShortType(string fqn) => fqn.Replace("global::", "");
    }

    /// <summary>
    /// The descriptor type and constructor arguments for one action or effect: a
    /// <c>BlexAction&lt;TStore&gt;</c> when it takes no arguments, otherwise a
    /// <c>BlexAction&lt;TStore, TPayload&gt;</c> whose payload is the single parameter's type or a tuple
    /// of all of them, named after the parameters.
    /// </summary>
    private static (string Type, string Arguments) DescribeAction(
        string storeType,
        string label,
        string wrapper,
        bool returnsTask,
        BlexEquatableArray<BlexParamModel> parameters)
    {
        const string CompletedTask = "global::System.Threading.Tasks.Task.CompletedTask";

        string Invoke(string lambdaParameters, string arguments)
            => returnsTask
                ? $"static {lambdaParameters} => __s.{wrapper}({arguments})"
                : $"static {lambdaParameters} => {{ __s.{wrapper}({arguments}); return {CompletedTask}; }}";

        if (parameters.Count == 0)
            return ($"global::Bit.Blex.BlexAction<{storeType}>", $"\"{Escape(label)}\", {Invoke("__s", "")}");

        // An optional parameter a payload leaves out takes its declared default, as in a direct call.
        // A null default is forgiven: the generated file is #nullable enable, and the parameter may not be.
        string Read(int i)
        {
            var p = parameters[i];
            return p.DefaultLiteral is null
                ? $"global::Bit.Blex.BlexActionDescriptor.GetArg<{p.TypeFqn}>(__a, {i}, \"{Escape(p.Name)}\")"
                : $"global::Bit.Blex.BlexActionDescriptor.GetArgOrDefault<{p.TypeFqn}>(__a, {i}, \"{Escape(p.Name)}\", {(p.DefaultLiteral == "null" ? "null!" : p.DefaultLiteral)})";
        }

        string payloadType;
        string arguments;
        string read;
        if (parameters.Count == 1)
        {
            payloadType = parameters[0].TypeFqn;
            arguments = "__p";
            read = Read(0);
        }
        else
        {
            var elements = TupleElementNames(parameters);
            payloadType = "(" + string.Join(", ", parameters.Select((p, i) => elements is null ? p.TypeFqn : $"{p.TypeFqn} {elements[i]}")) + ")";
            arguments = string.Join(", ", parameters.Select((_, i) => "__p." + (elements is null ? $"Item{i + 1}" : elements[i])));
            read = "(" + string.Join(", ", parameters.Select((_, i) => Read(i))) + ")";
        }

        var names = string.Join(", ", parameters.Select(p => $"\"{Escape(p.Name)}\""));
        return (
            $"global::Bit.Blex.BlexAction<{storeType}, {payloadType}>",
            $"\"{Escape(label)}\", new string[] {{ {names} }}, {Invoke("(__s, __p)", arguments)}, static __a => {read}");
    }

    /// <summary>
    /// PascalCased parameter names for a payload tuple (<c>(string Name, int Quantity)</c>), or
    /// <c>null</c> when one of them cannot name a tuple element -- a reserved name, an <c>ItemN</c> out
    /// of position, two parameters that only differ in case -- and the tuple stays unnamed.
    /// </summary>
    private static string[]? TupleElementNames(BlexEquatableArray<BlexParamModel> parameters)
    {
        var names = new string[parameters.Count];
        var seen = new HashSet<string>(System.StringComparer.Ordinal);
        for (var i = 0; i < parameters.Count; i++)
        {
            var raw = parameters[i].Name;
            var name = char.ToUpperInvariant(raw[0]) + raw.Substring(1);

            if (ReservedTupleNames.Contains(name))
                return null;

            if (name.Length > 4 && name.StartsWith("Item", System.StringComparison.Ordinal)
                && name.Substring(4).All(char.IsDigit) && name != $"Item{i + 1}")
            {
                return null;
            }

            if (!seen.Add(name))
                return null;

            names[i] = name;
        }

        return names;
    }

    private static string Emit(BlexStoreModel m)
    {
        const string JsonObject = "global::System.Text.Json.Nodes.JsonObject";
        const string JsonSerializer = "global::System.Text.Json.JsonSerializer";

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        var indent = "";
        if (m.Namespace is not null)
        {
            sb.AppendLine($"namespace {m.Namespace}");
            sb.AppendLine("{");
            indent = "    ";
        }

        sb.AppendLine($"{indent}partial class {m.ClassName} : global::Bit.Blex.BlexStoreBase");
        sb.AppendLine($"{indent}{{");

        var body = indent + "    ";

        // Name. Base-qualified so a member of the same name on the store cannot capture the call;
        // a scoped instance appends its key (see BlexManager.CreateScope).
        sb.AppendLine($"{body}/// <inheritdoc />");
        sb.AppendLine($"{body}public override string Name => base.WithInstanceKey(\"{Escape(m.StoreName)}\");");
        sb.AppendLine();

        if (m.Persist)
        {
            sb.AppendLine($"{body}/// <inheritdoc />");
            sb.AppendLine($"{body}public override bool Persist => true;");
            sb.AppendLine();
        }

        if (m.Version > 0)
        {
            sb.AppendLine($"{body}/// <inheritdoc />");
            sb.AppendLine($"{body}public override int PersistVersion => {m.Version};");
            sb.AppendLine();
        }

        if (m.EnforceActions)
        {
            sb.AppendLine($"{body}/// <inheritdoc />");
            sb.AppendLine($"{body}protected override bool EnforcesActions => true;");
            sb.AppendLine();
        }

        // State properties. Field accesses are `this.`-qualified and @-escaped so fields named
        // like locals/keywords ("state", "value", "@lock") bind correctly. EnforceActions makes the
        // setter private: assigning state from outside the store becomes a compile error.
        var setter = m.EnforceActions ? "private set" : "set";
        foreach (var s in m.States)
        {
            var field = "this." + Id(s.FieldName);
            var access = m.EnforceActions ? " Changed only by the store's own actions." : "";
            sb.AppendLine($"{body}/// <summary>Reactive state backed by <c>{DocEscape(s.FieldName)}</c>.{access}</summary>");
            sb.AppendLine($"{body}public {s.TypeFqn} {s.PropertyName}");
            sb.AppendLine($"{body}{{");
            sb.AppendLine($"{body}    get => {field};");
            sb.AppendLine($"{body}    {setter} => SetState(ref {field}, value, \"{Escape(s.PropertyName)}\");");
            sb.AppendLine($"{body}}}");
            sb.AppendLine();
        }

        var transient = m.States.Where(s => !s.Persist).ToList();
        if (transient.Count > 0)
        {
            var keys = string.Join(", ", transient.Select(s => $"\"{Escape(s.PropertyName)}\""));
            sb.AppendLine($"{body}private static readonly string[] __blexTransientKeys = new string[] {{ {keys} }};");
            sb.AppendLine($"{body}/// <inheritdoc />");
            sb.AppendLine($"{body}protected override global::System.Collections.Generic.IReadOnlyCollection<string> TransientStateKeys => __blexTransientKeys;");
            sb.AppendLine();
        }

        // Computed properties (memoized)
        foreach (var c in m.Computeds)
        {
            var validField = $"__{c.PropertyName}Valid";
            var valueField = $"__{c.PropertyName}Value";
            sb.AppendLine($"{body}private bool {validField};");
            sb.AppendLine($"{body}private {c.TypeFqn} {valueField} = default!;");
            sb.AppendLine($"{body}/// <summary>Memoized computed value from <c>{c.MethodName}()</c>.</summary>");
            sb.AppendLine($"{body}public {c.TypeFqn} {c.PropertyName}");
            sb.AppendLine($"{body}{{");
            sb.AppendLine($"{body}    get");
            sb.AppendLine($"{body}    {{");
            sb.AppendLine($"{body}        if (!{validField})");
            sb.AppendLine($"{body}        {{");
            sb.AppendLine($"{body}            {valueField} = {c.MethodName}();");
            sb.AppendLine($"{body}            {validField} = true;");
            sb.AppendLine($"{body}        }}");
            sb.AppendLine($"{body}        return {valueField};");
            sb.AppendLine($"{body}    }}");
            sb.AppendLine($"{body}}}");
            sb.AppendLine();
        }

        // Action wrappers
        foreach (var a in m.Actions)
        {
            var paramList = ParamList(a.Parameters);
            var argList = string.Join(", ", a.Parameters.Select(p => Id(p.Name)));
            var label = Escape(a.ActionLabel);
            var argsExpr = ArgsExpression(a.Parameters);
            var trailing = argsExpr is null ? "" : $", {argsExpr}";
            sb.AppendLine($"{body}/// <summary>Dispatches the <c>{DocEscape(a.ActionLabel)}</c> action.</summary>");
            if (a.IsAsync)
            {
                // ValueTask / ValueTask<T> aren't assignable to Task, so normalize via AsTask().
                var call = a.IsValueTask ? $"{a.ImplName}({argList}).AsTask()" : $"{a.ImplName}({argList})";
                sb.AppendLine($"{body}public global::System.Threading.Tasks.Task {a.WrapperName}({paramList})");
                sb.AppendLine($"{body}    => DispatchAsync(\"{label}\", () => {call}{trailing});");
            }
            else
            {
                sb.AppendLine($"{body}public void {a.WrapperName}({paramList})");
                sb.AppendLine($"{body}    => Dispatch(\"{label}\", () => {a.ImplName}({argList}){trailing});");
            }

            sb.AppendLine();
        }

        // Effect wrappers (auto-managed loading/error lifecycle, cancellation, concurrency)
        foreach (var e in m.Effects)
        {
            var pendingField = $"__{e.WrapperName}Pending";
            var errorField = $"__{e.WrapperName}Error";
            var ctsField = $"__{e.WrapperName}Cts";
            var queueField = $"__{e.WrapperName}Queue";
            var versionField = $"__{e.WrapperName}Version";
            var paramList = ParamList(e.Parameters);
            var argList = string.Join(", ", e.Parameters.Select(p => Id(p.Name)));
            // Wrapper locals carry a __blex prefix so user parameters (even "__ct") can't collide.
            var implArgs = e.HasCancellationToken
                ? (argList.Length == 0 ? "__blexCt" : $"{argList}, __blexCt")
                : argList;
            var call = e.IsValueTask ? $"{e.ImplName}({implArgs}).AsTask()" : $"{e.ImplName}({implArgs})";
            if (e.RetryCount > 0 || e.TimeoutMilliseconds > 0)
            {
                // The body runs once per attempt, inside the one recorded action; with a timeout, each
                // attempt gets a token of its own, linked to the effect's.
                var attemptArgs = e.HasCancellationToken
                    ? (argList.Length == 0 ? "__blexAttemptCt" : $"{argList}, __blexAttemptCt")
                    : argList;
                var attempt = e.IsValueTask ? $"{e.ImplName}({attemptArgs}).AsTask()" : $"{e.ImplName}({attemptArgs})";
                var token = e.HasCancellationToken ? "__blexCt" : "default";
                call = $"base.RunEffectAttemptsAsync(__blexAttemptCt => {attempt}, {token}, {e.RetryCount}, {e.RetryDelayMilliseconds}, {e.TimeoutMilliseconds})";
            }
            var label = Escape(e.ActionLabel);
            var argsExpr = ArgsExpression(e.Parameters);
            var isLatest = e.Concurrency == 1;
            var isDrop = e.Concurrency == 2;
            var isQueue = e.Concurrency == 3;
            var debounceField = $"__{e.WrapperName}Debounce";
            var isDebounced = e.DebounceMilliseconds > 0;

            var cancellationNote = e.HasCancellationToken
                ? " Cancellations via the effect's own token are not treated as errors."
                : "";
            sb.AppendLine($"{body}private int {pendingField};");
            sb.AppendLine($"{body}/// <summary>True while any run of the <c>{DocEscape(e.ActionLabel)}</c> effect is in flight.</summary>");
            sb.AppendLine($"{body}public bool {e.WrapperName}IsLoading => {pendingField} > 0;");
            sb.AppendLine($"{body}private global::System.Exception? {errorField};");
            sb.AppendLine($"{body}/// <summary>The exception thrown by the last <c>{DocEscape(e.ActionLabel)}</c> run, or <c>null</c>.{cancellationNote}</summary>");
            sb.AppendLine($"{body}public global::System.Exception? {e.WrapperName}Error => {errorField};");

            if (e.HasCancellationToken)
                sb.AppendLine($"{body}private global::System.Threading.CancellationTokenSource? {ctsField};");

            // A debounced invocation still waiting out its quiet period is as much "in flight" as a running one:
            // a Clear button that cancels only the run would let the pending one start a request a moment later.
            if (e.HasCancellationToken && isDebounced)
            {
                sb.AppendLine($"{body}/// <summary>Cancels the in-flight <c>{DocEscape(e.ActionLabel)}</c> run (the most recently started one, when runs overlap) and the invocation still waiting out its debounce, if any.</summary>");
                sb.AppendLine($"{body}public void Cancel{e.WrapperName}()");
                sb.AppendLine($"{body}{{");
                sb.AppendLine($"{body}    {debounceField}?.Cancel();");
                sb.AppendLine($"{body}    {ctsField}?.Cancel();");
                sb.AppendLine($"{body}}}");
            }
            else if (e.HasCancellationToken)
            {
                sb.AppendLine($"{body}/// <summary>Cancels the in-flight <c>{DocEscape(e.ActionLabel)}</c> run (the most recently started one, when runs overlap).</summary>");
                sb.AppendLine($"{body}public void Cancel{e.WrapperName}() => {ctsField}?.Cancel();");
            }
            else if (isDebounced)
            {
                sb.AppendLine($"{body}/// <summary>Cancels the <c>{DocEscape(e.ActionLabel)}</c> invocation still waiting out its debounce, if any. A run that already started is not cancellable: the effect takes no <c>CancellationToken</c>.</summary>");
                sb.AppendLine($"{body}public void Cancel{e.WrapperName}() => {debounceField}?.Cancel();");
            }

            if (isLatest)
                sb.AppendLine($"{body}private int {versionField};");

            if (isQueue)
                sb.AppendLine($"{body}private global::System.Threading.Tasks.Task {queueField} = global::System.Threading.Tasks.Task.CompletedTask;");

            if (isDebounced)
                sb.AppendLine($"{body}private global::System.Threading.CancellationTokenSource? {debounceField};");

            var debounceNote = isDebounced
                ? $" Waits for a quiet period of {e.DebounceMilliseconds} ms first; a newer invocation supersedes this one, which then returns without running."
                : "";
            sb.AppendLine($"{body}/// <summary>Runs the <c>{DocEscape(e.ActionLabel)}</c> effect, managing its loading/error state.{debounceNote}</summary>");
            sb.AppendLine($"{body}public async global::System.Threading.Tasks.Task {e.WrapperName}({paramList})");
            sb.AppendLine($"{body}{{");

            if (isDebounced)
            {
                // The gate runs before anything observable: a superseded invocation must not flip
                // the loading counter, clear the error, reach middleware or record an action -- it
                // never happened as far as the rest of the app is concerned. The Drop check below
                // deliberately sits after it, so the "is one already running" question is asked
                // about the invocation that actually survived the quiet period.
                sb.AppendLine($"{body}    var __blexDebounceCts = new global::System.Threading.CancellationTokenSource();");
                sb.AppendLine($"{body}    var __blexDebouncePrevious = {debounceField};");
                sb.AppendLine($"{body}    {debounceField} = __blexDebounceCts;");
                // Cancel the predecessor but leave its disposal to its own finally: disposing a
                // source another frame is still awaiting on is the kind of thing that only breaks
                // in production.
                sb.AppendLine($"{body}    __blexDebouncePrevious?.Cancel();");
                sb.AppendLine($"{body}    try");
                sb.AppendLine($"{body}    {{");
                // No ConfigureAwait(false): resume on the dispatch context (the Blazor renderer)
                // so the body mutates state on the thread every other action runs on. The wait runs
                // on the manager's clock, so a test advances a fake TimeProvider instead of sleeping,
                // and is skipped while a recorded log is replayed.
                sb.AppendLine($"{body}        await base.DebounceEffectAsync({e.DebounceMilliseconds}, __blexDebounceCts.Token);");
                sb.AppendLine($"{body}    }}");
                sb.AppendLine($"{body}    catch (global::System.OperationCanceledException)");
                sb.AppendLine($"{body}    {{");
                sb.AppendLine($"{body}        return; // superseded by a newer invocation");
                sb.AppendLine($"{body}    }}");
                sb.AppendLine($"{body}    finally");
                sb.AppendLine($"{body}    {{");
                // Only the invocation still holding the field may clear it; a superseded one would
                // otherwise erase its successor's source and break the next supersession.
                sb.AppendLine($"{body}        if (object.ReferenceEquals({debounceField}, __blexDebounceCts))");
                sb.AppendLine($"{body}            {debounceField} = null;");
                sb.AppendLine($"{body}        __blexDebounceCts.Dispose();");
                sb.AppendLine($"{body}    }}");
                sb.AppendLine();
            }

            if (isDrop) // ignore while one is running
            {
                sb.AppendLine($"{body}    if ({pendingField} > 0)");
                sb.AppendLine($"{body}        return;");
            }

            // Take the veto decision before touching anything observable. A vetoed action must
            // leave no trace, and every step below this point is a side effect: the loading
            // counter, the cleared error, the cancelled predecessor (Latest) and the queue slot
            // (Queue). The body then runs through DispatchApprovedAsync so the pipeline is not
            // consulted a second time.
            sb.AppendLine($"{body}    global::Bit.Blex.BlexActionArg[]? __blexArgs = {argsExpr ?? "null"};");
            sb.AppendLine($"{body}    if (!BeginAction(\"{label}\", __blexArgs))");
            sb.AppendLine($"{body}        return;");

            if (isLatest) // stale runs must not clobber the newest run's error state
                sb.AppendLine($"{body}    var __blexVersion = ++{versionField};");

            if (e.HasCancellationToken)
            {
                if (isLatest) // cancel the previous run
                    sb.AppendLine($"{body}    {ctsField}?.Cancel();");
                sb.AppendLine($"{body}    var __blexCts = new global::System.Threading.CancellationTokenSource();");
                sb.AppendLine($"{body}    {ctsField} = __blexCts;");
                sb.AppendLine($"{body}    var __blexCt = __blexCts.Token;");
            }

            if (isQueue) // run after the previous invocation completes
            {
                sb.AppendLine($"{body}    var __blexPrevious = {queueField};");
                sb.AppendLine($"{body}    var __blexGate = new global::System.Threading.Tasks.TaskCompletionSource(global::System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);");
                sb.AppendLine($"{body}    {queueField} = __blexGate.Task;");
            }

            sb.AppendLine($"{body}    try");
            sb.AppendLine($"{body}    {{");
            // Inside the try so a throwing StateChanged subscriber can't skip the finally
            // (which would brick a Queue gate or leak the pending counter).
            sb.AppendLine($"{body}        BeginEffect(ref {pendingField});");
            if (isQueue)
            {
                // Wait for the predecessor BEFORE clearing the error: clearing first would let a
                // failed predecessor overwrite this (successful) run's cleared state out of order.
                sb.AppendLine($"{body}        await __blexPrevious.ConfigureAwait(true);");
            }

            sb.AppendLine($"{body}        SetEffectState(ref {errorField}, null);");
            sb.AppendLine($"{body}        await DispatchApprovedAsync(\"{label}\", () => {call}, __blexArgs).ConfigureAwait(true);");
            sb.AppendLine($"{body}    }}");
            if (e.HasCancellationToken)
            {
                // Only *our* cancellation is benign; a foreign OperationCanceledException (e.g. an
                // HttpClient timeout) is a real failure and falls through to the error handler.
                sb.AppendLine($"{body}    catch (global::System.OperationCanceledException) when (__blexCt.IsCancellationRequested)");
                sb.AppendLine($"{body}    {{");
                sb.AppendLine($"{body}        // Cancelled via Cancel{e.WrapperName}() or supersession -- a normal outcome, not an error.");
                sb.AppendLine($"{body}    }}");
            }

            sb.AppendLine($"{body}    catch (global::System.Exception __blexEx)");
            sb.AppendLine($"{body}    {{");
            if (isLatest)
            {
                sb.AppendLine($"{body}        // A superseded (stale) run must not clobber the newest run's error state.");
                sb.AppendLine($"{body}        if (__blexVersion == {versionField})");
                sb.AppendLine($"{body}            SetEffectState(ref {errorField}, __blexEx);");
            }
            else
            {
                sb.AppendLine($"{body}        SetEffectState(ref {errorField}, __blexEx);");
            }

            sb.AppendLine($"{body}    }}");
            sb.AppendLine($"{body}    finally");
            sb.AppendLine($"{body}    {{");
            if (isQueue)
                sb.AppendLine($"{body}        __blexGate.SetResult();");
            if (e.HasCancellationToken)
            {
                sb.AppendLine($"{body}        if (object.ReferenceEquals({ctsField}, __blexCts))");
                sb.AppendLine($"{body}            {ctsField} = null;");
                sb.AppendLine($"{body}        __blexCts.Dispose();");
            }

            sb.AppendLine($"{body}        EndEffect(ref {pendingField});");
            sb.AppendLine($"{body}    }}");
            sb.AppendLine($"{body}}}");
            sb.AppendLine();
        }

        // Cancellation on disposal. Only the sources are cancelled, never disposed: the frame that
        // created one owns it and disposes it in its own finally, and disposing it from here would
        // race that frame's `catch (OperationCanceledException) when (ct.IsCancellationRequested)`.
        var cancellable = m.Effects.Where(e => e.HasCancellationToken || e.DebounceMilliseconds > 0).ToList();
        if (cancellable.Count > 0)
        {
            sb.AppendLine($"{body}/// <inheritdoc />");
            sb.AppendLine($"{body}protected override void CancelEffects()");
            sb.AppendLine($"{body}{{");
            foreach (var e in cancellable)
            {
                if (e.HasCancellationToken)
                    sb.AppendLine($"{body}    __{e.WrapperName}Cts?.Cancel();");
                if (e.DebounceMilliseconds > 0)
                    sb.AppendLine($"{body}    __{e.WrapperName}Debounce?.Cancel();");
            }

            sb.AppendLine($"{body}    base.CancelEffects();");
            sb.AppendLine($"{body}}}");
            sb.AppendLine();
        }

        if (m.Computeds.Count > 0)
        {
            sb.AppendLine($"{body}/// <inheritdoc />");
            sb.AppendLine($"{body}protected override void InvalidateComputed()");
            sb.AppendLine($"{body}{{");
            foreach (var c in m.Computeds)
                sb.AppendLine($"{body}    __{c.PropertyName}Valid = false;");
            sb.AppendLine($"{body}}}");
            sb.AppendLine();

            // A write invalidates the computeds that read the property written -- and those whose reads could not be
            // told apart, which every write invalidates. Anything else (a hand-written SetState) invalidates them all.
            sb.AppendLine($"{body}/// <inheritdoc />");
            sb.AppendLine($"{body}protected override void InvalidateComputed(string? propertyName)");
            sb.AppendLine($"{body}{{");
            sb.AppendLine($"{body}    switch (propertyName)");
            sb.AppendLine($"{body}    {{");
            foreach (var s in m.States)
            {
                sb.AppendLine($"{body}        case \"{Escape(s.PropertyName)}\":");
                foreach (var c in m.Computeds)
                {
                    if (c.ReadsEverything || c.Reads.Contains(s.PropertyName))
                        sb.AppendLine($"{body}            __{c.PropertyName}Valid = false;");
                }

                sb.AppendLine($"{body}            break;");
            }

            sb.AppendLine($"{body}        default:");
            sb.AppendLine($"{body}            InvalidateComputed();");
            sb.AppendLine($"{body}            break;");
            sb.AppendLine($"{body}    }}");
            sb.AppendLine($"{body}}}");
            sb.AppendLine();
        }

        // RollbackOnError: a shallow copy of the state fields, taken as each synchronous action starts and put
        // back when it throws. Field reads and writes only -- no notification, no serialization.
        if (m.RollbackOnError && m.States.Count > 0)
        {
            var fields = string.Join(", ", m.States.Select(s => "this." + Id(s.FieldName)));
            sb.AppendLine($"{body}/// <inheritdoc />");
            sb.AppendLine($"{body}protected override object?[]? CaptureRollbackState() => new object?[] {{ {fields} }};");
            sb.AppendLine();
            sb.AppendLine($"{body}/// <inheritdoc />");
            sb.AppendLine($"{body}protected override void ApplyRollbackState(object?[] state)");
            sb.AppendLine($"{body}{{");
            var slot = 0;
            foreach (var s in m.States)
                sb.AppendLine($"{body}    this.{Id(s.FieldName)} = ({s.TypeFqn})state[{slot++}]!;");
            sb.AppendLine($"{body}}}");
            sb.AppendLine();
        }

        EmitActionDescriptors(sb, m, body);

        // SerializeState
        sb.AppendLine($"{body}/// <inheritdoc />");
        sb.AppendLine($"{body}public override {JsonObject} SerializeState()");
        sb.AppendLine($"{body}{{");
        sb.AppendLine($"{body}    var __o = new {JsonObject}();");
        foreach (var s in m.States)
            sb.AppendLine($"{body}    __o[\"{Escape(s.PropertyName)}\"] = {JsonSerializer}.SerializeToNode(this.{Id(s.FieldName)}, global::Bit.Blex.BlexJson.Options);");
        sb.AppendLine($"{body}    return __o;");
        sb.AppendLine($"{body}}}");
        sb.AppendLine();

        // DeserializeState
        sb.AppendLine($"{body}/// <inheritdoc />");
        sb.AppendLine($"{body}public override void DeserializeState({JsonObject} state)");
        sb.AppendLine($"{body}{{");
        // Every field is read before any is assigned: a payload that fails partway -- a value of the wrong
        // type, an enum member that no longer exists -- leaves the store exactly as it was, rather than
        // half-restored with nothing notified about the half that moved.
        var idx = 0;
        foreach (var s in m.States)
        {
            // A property that is present but JSON-null must restore the field to default/null;
            // only a *missing* property leaves the current value untouched.
            var node = $"__n{idx}";
            var value = $"__v{idx++}";
            sb.AppendLine($"{body}    {s.TypeFqn} {value} = this.{Id(s.FieldName)};");
            sb.AppendLine($"{body}    if (state.TryGetPropertyValue(\"{Escape(s.PropertyName)}\", out var {node}))");
            sb.AppendLine($"{body}        {value} = {node} is null ? default! : {JsonSerializer}.Deserialize<{s.TypeFqn}>({node}, global::Bit.Blex.BlexJson.Options)!;");
        }

        idx = 0;
        foreach (var s in m.States)
            sb.AppendLine($"{body}    this.{Id(s.FieldName)} = __v{idx++};");

        sb.AppendLine($"{body}}}");

        sb.AppendLine($"{indent}}}");
        if (m.Namespace is not null)
            sb.AppendLine("}");

        return sb.ToString();
    }
}
