﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Tooling.SourceGenerators;

[Generator]
public class AutoInjectSourceGenerator : ISourceGenerator
{
    private static readonly string AutoInjectAttributeName = typeof(AutoInjectAttribute).FullName;
    
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new AutoInjectSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if ((context.SyntaxContextReceiver is AutoInjectSyntaxReceiver receiver) is false)
            return;

        INamedTypeSymbol? attributeSymbol = context.Compilation.GetTypeByMetadataName(AutoInjectAttributeName);

        foreach (IGrouping<INamedTypeSymbol, ISymbol> group in receiver.EligibleMembers
                     .GroupBy<ISymbol, INamedTypeSymbol>(f => f.ContainingType, SymbolEqualityComparer.Default))
        {
            string? partialClassSource = GenerateSource(attributeSymbol, group.Key, group.ToList());

            if (string.IsNullOrEmpty(partialClassSource) is false)
                context.AddSource($"{group.Key.Name}_autoInject.g.cs",
                    SourceText.From(partialClassSource!, Encoding.UTF8));
        }

        foreach (var @class in receiver.EligibleClassesWithBaseClassUsedAutoInject)
        {
            string? partialClassSource = GenerateSource(attributeSymbol, @class, new List<ISymbol>());

            if (string.IsNullOrEmpty(partialClassSource) is false)
                context.AddSource($"{@class.Name}_autoInject.g.cs",
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
