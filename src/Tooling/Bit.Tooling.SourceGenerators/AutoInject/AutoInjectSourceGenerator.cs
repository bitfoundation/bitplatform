using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Bit.Tooling.SourceGenerators;

[Generator]
public class AutoInjectSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new AutoInjectSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if ((context.SyntaxContextReceiver is AutoInjectSyntaxReceiver receiver) is false)
            return;

        INamedTypeSymbol? attributeSymbol = context.Compilation.GetTypeByMetadataName(AutoInjectConstantInformation.AttributeName);

        foreach (IGrouping<INamedTypeSymbol, ISymbol> group in receiver.EligibleMembers
                     .GroupBy<ISymbol, INamedTypeSymbol>(f => f.ContainingType, SymbolEqualityComparer.Default))
        {
            string? partialClassSource = GenerateSource(attributeSymbol, group.Key, group.ToList());

            if (string.IsNullOrEmpty(partialClassSource) is false)
                context.AddSource($"{group.Key.Name}_autoInject.g.cs",
                    SourceText.From(partialClassSource!, Encoding.UTF8));
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
            AutoInjectClassType.RazorComponent => AutoInjectRazorComponentHandler.Generate(attributeSymbol,
                classSymbol, eligibleMembers),
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
        return @class.AllInterfaces.Any(o =>
            o.ToDisplayString() == "Microsoft.AspNetCore.Components.IComponent");
    }
}
