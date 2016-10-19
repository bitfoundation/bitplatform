using System;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    public static class IPropertySymbolExtensions
    {
        public static bool IsKey(this IPropertySymbol prop)
        {
            if (prop == null)
                throw new ArgumentNullException(nameof(prop));

            return string.Compare(prop.Name, "Id", StringComparison.InvariantCultureIgnoreCase) == 0 || string.Compare(prop.Name, (prop.ContainingType?.Name + "Id"), StringComparison.InvariantCultureIgnoreCase) == 0 || prop.GetAttributes().Any(att => att.AttributeClass.Name == "KeyAttribute");
        }
    }
}
