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

            if (prop.IsKeyByConfiguration())
                return true;

            if (prop.IsKeyByConvention() && prop.ContainingType.GetMembers().OfType<IPropertySymbol>().All(p => !p.IsKeyByConfiguration()))
                return true;

            return false;
        }

        private static bool IsKeyByConvention(this IPropertySymbol prop)
        {
            if (prop == null)
                throw new ArgumentNullException(nameof(prop));

            return string.Compare(prop.Name, "Id", StringComparison.InvariantCultureIgnoreCase) == 0 || string.Compare(prop.Name, (prop.ContainingType?.Name + "Id"), StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        private static bool IsKeyByConfiguration(this IPropertySymbol prop)
        {
            if (prop == null)
                throw new ArgumentNullException(nameof(prop));

            return prop.GetAttributes().Any(att => att.AttributeClass.Name == "KeyAttribute");
        }

        public static string GetViewType(this IPropertySymbol prop)
        {
            if (prop == null)
                throw new ArgumentNullException(nameof(prop));

            AttributeData dataTypeAtt = prop.GetAttributes()
                .SingleOrDefault(att => att.AttributeClass.Name == "DataTypeAttribute");

            if (dataTypeAtt == null)
                return null;

            int value = Convert.ToInt32(dataTypeAtt.ConstructorArguments.Single().Value);

            return dataTypeAtt.ConstructorArguments.Single().Type.GetMembers()
                .OfType<IFieldSymbol>()
                .Single(f => Convert.ToInt32(f.ConstantValue) == value).Name;
        }
    }
}
