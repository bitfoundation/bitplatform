using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Bit.Tooling.SourceGenerators;

public class AutoInjectHelper
{
    public static IReadOnlyCollection<ISymbol> GetBaseClassEligibleMembers(
        INamedTypeSymbol? classSymbol,
        INamedTypeSymbol? attributeSymbol)
    {
        if (classSymbol is null)
            throw new ArgumentNullException(nameof(classSymbol));
        
        if(attributeSymbol is null)
            throw new ArgumentNullException(nameof(attributeSymbol));
            
        
        bool hasBase = false;
        List<ISymbol> result = new List<ISymbol>();
        INamedTypeSymbol? currentClass = classSymbol;

        do
        {
            if (currentClass.BaseType != null)
            {
                INamedTypeSymbol baseType = currentClass.BaseType;
                string baseMetadataName = baseType.ToDisplayString();
                if (baseMetadataName != "System.Object")
                {
                    var baseEligibleFields = baseType
                        .GetMembers()
                        .Where(m =>
                            m.Kind == SymbolKind.Field &&
                            m.GetAttributes()
                                .Any(a => a.AttributeClass != null &&
                                          a.AttributeClass.MetadataName == attributeSymbol.MetadataName))
                        .ToList();

                    var baseEligibleProperties = baseType
                        .GetMembers()
                        .Where(m =>
                            m.Kind == SymbolKind.Property &&
                            m.GetAttributes()
                                .Any(a => a.AttributeClass != null &&
                                          a.AttributeClass.MetadataName == attributeSymbol.MetadataName))
                        .ToList();

                    result.AddRange(baseEligibleFields);
                    result.AddRange(baseEligibleProperties);
                    currentClass = baseType;
                    hasBase = true;
                }
                else
                {
                    hasBase = false;
                }
            }
            else
            {
                hasBase = false;
            }
        } while (hasBase);

        return result.OrderBy(o => o.Name).ToList();
    }

    public static string FormatMemberName(string? memberName)
    {
        if (memberName is null)
            throw new ArgumentNullException(nameof(memberName));
        
        memberName = memberName.TrimStart('_');
        if (memberName.Length == 0)
            return string.Empty;

        if (memberName.Length == 1)
            return memberName.ToUpper(CultureInfo.InvariantCulture);

        return memberName.Substring(0, 1).ToUpper(CultureInfo.InvariantCulture) + memberName.Substring(1);
    }

    public static bool IsContainingSymbolEqualToContainingNamespace(INamedTypeSymbol? @class)
    {
        if(@class is null)
            throw new ArgumentNullException(nameof(@class));
            
        return @class.ContainingSymbol.Equals(@class.ContainingNamespace, SymbolEqualityComparer.Default);
    }
}
