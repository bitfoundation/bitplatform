using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bit.BlazorUI.SourceGenerators.Component;

[Generator]
public class ComponentSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var parameterProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Microsoft.AspNetCore.Components.ParameterAttribute",
                predicate: static (node, _) => IsPartialClassProperty(node),
                transform: static (ctx, ct) => ExtractBlazorParameter(ctx, ct))
            .Where(static p => p is not null)
            .Select(static (p, _) => p!.Value);

        var cascadingProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Microsoft.AspNetCore.Components.CascadingParameterAttribute",
                predicate: static (node, _) => IsPartialClassProperty(node),
                transform: static (ctx, ct) => ExtractBlazorParameter(ctx, ct))
            .Where(static p => p is not null)
            .Select(static (p, _) => p!.Value);

        var combined = parameterProvider.Collect()
            .Combine(cascadingProvider.Collect())
            .Select(static (pair, _) =>
                pair.Left
                    .AddRange(pair.Right)
                    .GroupBy(static p => (p.ContainingTypeFullName, p.PropertyName))
                    .Select(static g => g.First())
                    .ToImmutableArray());

        context.RegisterSourceOutput(combined, static (spc, parameters) => Execute(spc, parameters));
    }

    private static bool IsPartialClassProperty(SyntaxNode node)
    {
        return node is PropertyDeclarationSyntax prop &&
               prop.Parent is (ClassDeclarationSyntax or RecordDeclarationSyntax) and TypeDeclarationSyntax typeDecl &&
               typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    private static BlazorParameter? ExtractBlazorParameter(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.TargetSymbol is not IPropertySymbol prop) return null;

        var containingType = prop.ContainingType;
        if (containingType is null) return null;

        var compilation = ctx.SemanticModel.Compilation;
        var componentBaseType = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.ComponentBase");
        var bitComponentBaseType = compilation.GetTypeByMetadataName("Bit.BlazorUI.BitComponentBase");

        // Legacy syntax receiver did not filter by base type; omitting that preserves parity with ISyntaxContextReceiver output volume.

        // Legacy syntax receiver skipped when any member was named SetParametersAsync (method, property, etc.).
        if (ContainingTypeDeclaresSetParametersAsyncName(containingType)) return null;

        var attrs = prop.GetAttributes();
        var resetClassBuilder = attrs.Any(a => a.AttributeClass?.ToDisplayString() == "Bit.BlazorUI.ResetClassBuilderAttribute");
        var resetStyleBuilder = attrs.Any(a => a.AttributeClass?.ToDisplayString() == "Bit.BlazorUI.ResetStyleBuilderAttribute");
        var isTwoWayBound = attrs.Any(a => a.AttributeClass?.ToDisplayString() == "Bit.BlazorUI.TwoWayBoundAttribute");

        var callOnSetAttr = attrs.SingleOrDefault(a => a.AttributeClass?.ToDisplayString() == "Bit.BlazorUI.CallOnSetAttribute");
        var callOnSetName = callOnSetAttr?.ConstructorArguments.FirstOrDefault().Value as string;

        var callOnSetAsyncAttr = attrs.SingleOrDefault(a => a.AttributeClass?.ToDisplayString() == "Bit.BlazorUI.CallOnSetAsyncAttribute");
        var callOnSetAsyncName = callOnSetAsyncAttr?.ConstructorArguments.FirstOrDefault().Value as string;

        var classNameForCode = BuildClassNameForCode(containingType);
        var isBaseTypeComponentBase = containingType.BaseType is not null &&
            componentBaseType is not null &&
            SymbolEqualityComparer.Default.Equals(containingType.BaseType, componentBaseType);
        var inheritsFromBit = InheritsFromBitComponentBase(containingType, bitComponentBaseType);

        return new BlazorParameter(
            ContainingTypeFullName: containingType.ToDisplayString(),
            ClassName: containingType.Name,
            ClassNameForCode: classNameForCode,
            ClassNamespace: containingType.ContainingNamespace.ToDisplayString(),
            IsBaseTypeComponentBase: isBaseTypeComponentBase,
            InheritsFromBitComponentBase: inheritsFromBit,
            PropertyName: prop.Name,
            PropertyType: prop.Type.ToDisplayString(),
            ResetClassBuilder: resetClassBuilder,
            ResetStyleBuilder: resetStyleBuilder,
            IsTwoWayBound: isTwoWayBound,
            CallOnSetMethodName: callOnSetName,
            CallOnSetAsyncMethodName: callOnSetAsyncName);
    }

    private static void Execute(SourceProductionContext spc, ImmutableArray<BlazorParameter> parameters)
    {
        foreach (var group in parameters.GroupBy(p => p.ContainingTypeFullName))
        {
            var list = group.ToList();
            var first = list[0];
            string source = GeneratePartialClass(first, list);
            spc.AddSource($"{EscapeForHint(first.ContainingTypeFullName)}_SetParametersAsync.AutoGenerated.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GeneratePartialClass(BlazorParameter classInfo, List<BlazorParameter> parameters)
    {
        var namespaceName = classInfo.ClassNamespace;
        var className = classInfo.ClassNameForCode;
        var twoWayParameters = parameters.Where(p => p.IsTwoWayBound).ToArray();
        var isBaseTypeComponentBase = classInfo.IsBaseTypeComponentBase;
        var doesSupportParametersViewCache = classInfo.InheritsFromBitComponentBase;

        StringBuilder builder = new StringBuilder($@"using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace {namespaceName}
{{
    public partial class {className}
    {{
");
        builder.AppendLine("        private readonly HashSet<string> __assignedParameters = [];");
        builder.AppendLine("");
        foreach (var par in twoWayParameters)
        {
            builder.AppendLine($"        private bool {par.PropertyName}HasBeenSet;");
            builder.AppendLine($"        [Parameter] public EventCallback<{par.PropertyType}> {par.PropertyName}Changed {{ get; set; }}");
        }
        if (twoWayParameters.Length > 0) builder.AppendLine("");
        builder.AppendLine($@"        [global::System.Diagnostics.DebuggerNonUserCode]
        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public override async Task SetParametersAsync(ParameterView parameters)
        {{");
        builder.AppendLine($"            __assignedParameters.Clear();");
        foreach (var par in twoWayParameters)
        {
            builder.AppendLine($"            {par.PropertyName}HasBeenSet = false;");
        }
        if (doesSupportParametersViewCache)
        {
            builder.AppendLine("            var parametersDictionary = new Dictionary<string, object?>(parameters.ToDictionary());");
            builder.AppendLine("            ParametersCache = parametersDictionary;");
        }
        else
        {
            builder.AppendLine("            var parametersDictionary = new Dictionary<string, object?>(parameters.ToDictionary());");
        }
        builder.AppendLine("            foreach (var parameter in parametersDictionary.ToArray())");
        builder.AppendLine("            {");
        builder.AppendLine("                switch (parameter.Key)");
        builder.AppendLine("                {");
        foreach (var par in parameters)
        {
            var paramName = par.PropertyName;
            var varName = $"@{paramName.ToLowerInvariant()}";
            var paramType = par.PropertyType;
            builder.AppendLine($"                    case nameof({paramName}):");
            builder.AppendLine($"                       __assignedParameters.Add(nameof({paramName}));");
            if (par.IsTwoWayBound)
            {
                builder.AppendLine($"                       {paramName}HasBeenSet = true;");
            }
            builder.AppendLine($"                       var {varName} = parameter.Value is null ? default! : ({paramType})parameter.Value;");
            if (par.ResetClassBuilder || par.ResetStyleBuilder || string.IsNullOrWhiteSpace(par.CallOnSetMethodName) is false || string.IsNullOrWhiteSpace(par.CallOnSetAsyncMethodName) is false)
            {
                builder.AppendLine($"                       var notEquals{paramName} = EqualityComparer<{paramType}>.Default.Equals({paramName}, {varName}) is false;");
            }
            builder.AppendLine($"                       {paramName} = {varName};");
            if (par.ResetClassBuilder)
            {
                builder.AppendLine($"                       if (notEquals{paramName}) ClassBuilder.Reset();");
            }
            if (par.ResetStyleBuilder)
            {
                builder.AppendLine($"                       if (notEquals{paramName}) StyleBuilder.Reset();");
            }
            if (string.IsNullOrWhiteSpace(par.CallOnSetMethodName) is false)
            {
                builder.AppendLine($"                       if (notEquals{paramName}) {par.CallOnSetMethodName}();");
            }
            if (string.IsNullOrWhiteSpace(par.CallOnSetAsyncMethodName) is false)
            {
                builder.AppendLine($"                       if (notEquals{paramName}) await {par.CallOnSetAsyncMethodName}();");
            }
            builder.AppendLine("                       parametersDictionary.Remove(parameter.Key);");
            builder.AppendLine("                       break;");
            if (par.IsTwoWayBound)
            {
                var changedName = $"{paramName}Changed";
                var changedVarName = $"@{changedName.ToLowerInvariant()}";
                builder.AppendLine($"                    case nameof({changedName}):");
                builder.AppendLine($"                       var {changedVarName} = parameter.Value is null ? default! : (EventCallback<{paramType}>)parameter.Value;");
                builder.AppendLine($"                       {changedName} = {changedVarName};");
                builder.AppendLine("                       parametersDictionary.Remove(parameter.Key);");
                builder.AppendLine("                       break;");
            }
        }
        builder.AppendLine("                }");
        builder.AppendLine("            }");
        if (isBaseTypeComponentBase)
        {
            builder.AppendLine("            await base.SetParametersAsync(ParameterView.Empty);");
        }
        else
        {
            if (doesSupportParametersViewCache)
            {
                builder.AppendLine("            await base.SetParametersAsync(ParameterView.Empty);");
            }
            else
            {
                builder.AppendLine("            await base.SetParametersAsync(ParameterView.FromDictionary(parametersDictionary));");
            }
        }
        builder.AppendLine(@"        }");

        builder.AppendLine("");

        builder.AppendLine($@"        [global::System.Diagnostics.DebuggerNonUserCode]
        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public bool HasNotBeenSet(string name)
        {{");
        builder.AppendLine("            return __assignedParameters.Contains(name) is false;");
        builder.AppendLine("        }");

        if (twoWayParameters.Length > 0) builder.AppendLine("");
        foreach (var par in twoWayParameters)
        {
            var paramName = par.PropertyName;
            var paramType = par.PropertyType;
            builder.AppendLine($@"        [global::System.Diagnostics.DebuggerNonUserCode]
        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public async Task<bool> Assign{paramName}({paramType} value)
        {{");
            builder.AppendLine($"            if ({paramName}HasBeenSet && {paramName}Changed.HasDelegate is false) return false;");
            builder.AppendLine($"            if (EqualityComparer<{paramType}>.Default.Equals({paramName}, value) is false)");
            builder.AppendLine("            {");
            builder.AppendLine($"                {paramName} = value;");
            builder.AppendLine($"                await {paramName}Changed.InvokeAsync(value);");
            if (par.ResetClassBuilder)
            {
                builder.AppendLine($"                ClassBuilder.Reset();");
            }
            if (par.ResetStyleBuilder)
            {
                builder.AppendLine($"                StyleBuilder.Reset();");
            }
            if (string.IsNullOrWhiteSpace(par.CallOnSetMethodName) is false)
            {
                builder.AppendLine($"                {par.CallOnSetMethodName}();");
            }
            if (string.IsNullOrWhiteSpace(par.CallOnSetAsyncMethodName) is false)
            {
                builder.AppendLine($"                await {par.CallOnSetAsyncMethodName}();");
            }
            builder.AppendLine("            }");
            builder.AppendLine($"            return true;");
            builder.AppendLine("        }");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string BuildClassNameForCode(INamedTypeSymbol classSymbol)
    {
        if (classSymbol.IsGenericType)
        {
            // Same as legacy GetClassName: use resolved type arguments for generic arity display.
            var typeArgs = string.Join(", ", classSymbol.TypeArguments.Select(s => s.Name));
            return $"{classSymbol.Name}<{typeArgs}>";
        }
        return classSymbol.Name;
    }

    private static bool InheritsFromBitComponentBase(INamedTypeSymbol? typeSymbol, INamedTypeSymbol? frameworkBitComponentBaseSymbol)
    {
        if (typeSymbol is null) return false;
        if (typeSymbol.TypeKind is not TypeKind.Class) return false;
        if (frameworkBitComponentBaseSymbol is null) return false;
        if (SymbolEqualityComparer.Default.Equals(typeSymbol, frameworkBitComponentBaseSymbol)) return true;
        return InheritsFromBitComponentBase(typeSymbol.BaseType, frameworkBitComponentBaseSymbol);
    }

    /// <summary>
    /// Matches the legacy syntax receiver: skip if the declaring type has any member named SetParametersAsync.
    /// </summary>
    private static bool ContainingTypeDeclaresSetParametersAsyncName(INamedTypeSymbol containingType)
        => containingType.GetMembers().Any(m => m.Name == "SetParametersAsync");

    private static string EscapeForHint(string fullyQualifiedName)
        => fullyQualifiedName.Replace('<', '[').Replace('>', ']').Replace(' ', '_');
}
