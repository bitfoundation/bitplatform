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

            return string.Compare(prop.Name, "Id", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(prop.Name, (prop.ContainingType?.Name + "Id"), StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static bool IsKeyByConfiguration(this IPropertySymbol prop)
        {
            if (prop == null)
                throw new ArgumentNullException(nameof(prop));

            return prop.GetAttributes().Any(att => att.AttributeClass.Name == "KeyAttribute");
        }

        public static bool HasConcurrencyCheck(this IPropertySymbol prop)
        {
            if (prop == null)
                throw new ArgumentNullException(nameof(prop));

            return prop.GetAttributes().Any(att => att.AttributeClass.Name == "ConcurrencyCheckAttribute");
        }

        public static string GetViewType(this IPropertySymbol prop)
        {
            if (prop == null)
                throw new ArgumentNullException(nameof(prop));

            AttributeData dataTypeAtt = prop.GetAttributes()
                .ExtendedSingleOrDefault($"Looking for DataTypeAttribute on {prop.Name}", att => att.AttributeClass.Name == "DataTypeAttribute");

            if (dataTypeAtt == null)
                return null;

            TypedConstant dataTypeAttributeValue = dataTypeAtt.ConstructorArguments.ExtendedSingle("Getting parameter of DataTypeAttribute");

            int value = Convert.ToInt32(dataTypeAttributeValue.Value);

            return dataTypeAttributeValue.Type.GetMembers()
                .OfType<IFieldSymbol>()
                .ExtendedSingle($"Looking for {value} in DataTypeAttribute field members", fld => Convert.ToInt32(fld.ConstantValue) == value).Name;
        }

        public static bool IsAssociationProperty(this IPropertySymbol prop)
        {
            if (prop.Type.IsCollectionType() || prop.Type.IsQueryableType())
            {
                return IsAssociationProperty_Internal(prop.Type.GetElementType()); // List<CustomerDto>
            }
            else
            {
                return IsAssociationProperty_Internal(prop.Type); // CustomerDto
            }
        }

        private static bool IsAssociationProperty_Internal(this ITypeSymbol symbol)
        {
            string typeEdmName = symbol.GetEdmTypeName(useArrayForIEnumerableTypes: true);
            bool typeIsSimpleType = typeEdmName.StartsWith("$data") || typeEdmName.StartsWith("Edm");
            return !typeIsSimpleType && !symbol.IsEnum() && !symbol.IsComplexType();
        }

        public static string GetInversePropertyName(this IPropertySymbol prop)
        {
            AttributeData inversePropertyAtt = prop.GetAttributes().SingleOrDefault(att => att.AttributeClass.Name == "InversePropertyAttribute");

            if (inversePropertyAtt != null)
                return inversePropertyAtt.ConstructorArguments.Single().Value.ToString();
            else
            {
                ITypeSymbol thisClass = prop.ContainingType;

                ITypeSymbol otherClass = prop.Type.IsCollectionType() ? prop.Type.GetElementType() : prop.Type;

                string otherPropName = otherClass
                    .GetMembers()
                    .OfType<IPropertySymbol>()
                    .Select(p => new { p.Name, p.Type })
                    .Select(p => new { p.Name, Type = p.Type.IsCollectionType() ? p.Type.GetElementType() : p.Type })
                    .ExtendedSingleOrDefault($"Finding inverse property for {prop.Name} of {thisClass.Name}", p => p.Type == thisClass)?.Name;

                return otherPropName ?? "$$unbound";
            }
        }
    }
}
