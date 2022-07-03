using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bit.SourceGenerators;

[Generator]
public class AutoInjectSourceGenerator : ISourceGenerator
{
    private static readonly DiagnosticDescriptor NonPartialClassError = new DiagnosticDescriptor(id: "BITGEN001",
                                                                                              title: "The class use AutoInject must be partial",
                                                                                              messageFormat: "{0} is not partial. The AutoInject attribute needs to be used only in partial classes.",
                                                                                              category: "Bit.SourceGenerators",
                                                                                              DiagnosticSeverity.Error,
                                                                                              isEnabledByDefault: true);

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new AutoInjectSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if ((context.SyntaxContextReceiver is AutoInjectSyntaxReceiver receiver) is false)
            return;

        INamedTypeSymbol? attributeSymbol = context.Compilation.GetTypeByMetadataName(AutoInjectHelper.AutoInjectAttributeFullName);

        foreach (IGrouping<INamedTypeSymbol, ISymbol> group in receiver.EligibleMembers
                     .GroupBy<ISymbol, INamedTypeSymbol>(f => f.ContainingType, SymbolEqualityComparer.Default))
        {
            GenerateErrorWhenClassNonPartial(context, group.Key);

            string? partialClassSource = GenerateSource(attributeSymbol, group.Key, group.ToList());

            if (string.IsNullOrEmpty(partialClassSource) is false)
                context.AddSource($"{group.Key.Name}_autoInject.g.cs",
                    SourceText.From(partialClassSource!, Encoding.UTF8));
        }

        foreach (var @class in receiver.EligibleClassesWithBaseClassUsedAutoInject)
        {
            GenerateErrorWhenClassNonPartial(context, @class);

            var baseClass = @class.BaseType!;
            GenerateErrorWhenClassNonPartial(context, baseClass);

            string? partialClassSource = GenerateSource(attributeSymbol, @class, new List<ISymbol>());

            if (string.IsNullOrEmpty(partialClassSource) is false)
                context.AddSource($"{@class.Name}_autoInject.g.cs",
                  SourceText.From(partialClassSource!, Encoding.UTF8));
        }
    }

    private static void GenerateErrorWhenClassNonPartial(GeneratorExecutionContext context, INamedTypeSymbol @class)
    {
        var syntaxReferences = @class.DeclaringSyntaxReferences;
        foreach (var refrence in syntaxReferences)
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)refrence.GetSyntax();
            var classHasPartial = classDeclarationSyntax.Modifiers.Any(o => o.IsKind(SyntaxKind.PartialKeyword));
            if (classHasPartial is false)
            {
                context.ReportDiagnostic(Diagnostic.Create(NonPartialClassError, classDeclarationSyntax.GetLocation(), @class.Name));
            }
        }
    }

    private static string? GenerateSource(
        INamedTypeSymbol? attributeSymbol,
        INamedTypeSymbol? classSymbol,
        IReadOnlyCollection<ISymbol> eligibleMembers)
    {
        AutoInjectClassType env = FigureOutTypeOfEnvironment(classSymbol);
        return env switch
        {
            AutoInjectClassType.NormalClass => AutoInjectNormalClassHandler.Generate(attributeSymbol, classSymbol,
                eligibleMembers),
            AutoInjectClassType.RazorComponent => AutoInjectRazorComponentHandler.Generate(classSymbol, eligibleMembers),
            _ => ""
        };
    }

    private static AutoInjectClassType FigureOutTypeOfEnvironment(INamedTypeSymbol? @class)
    {
        if (@class is null)
            throw new ArgumentNullException(nameof(@class));

        if (IsClassIsRazorComponent(@class))
            return AutoInjectClassType.RazorComponent;
        else
            return AutoInjectClassType.NormalClass;
    }

    private static bool IsClassIsRazorComponent(INamedTypeSymbol @class)
    {
        bool isInheritIComponent = @class.AllInterfaces.Any(o =>
            o.ToDisplayString() == "Microsoft.AspNetCore.Components.IComponent");

        if (isInheritIComponent)
            return true;

        var classFilePaths = @class.Locations
            .Where(o => o.SourceTree != null)
            .Select(o => o.SourceTree?.FilePath)
            .ToList();

        string razorFileName = $"{@class.Name}.razor";

        foreach (var path in classFilePaths)
        {
            string directoryPath = Path.GetDirectoryName(path) ?? "";
            string filePath = Path.Combine(directoryPath, razorFileName);
            if (File.Exists(filePath))
                return true;
        }

        return false;
    }
}
