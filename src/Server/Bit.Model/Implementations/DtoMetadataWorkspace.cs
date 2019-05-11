using Bit.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Bit.Model.Implementations
{
    public class DtoMetadataWorkspace
    {
        private static DtoMetadataWorkspace _current;

        public static DtoMetadataWorkspace Current
        {
            get
            {
                if (_current == null)
                    _current = new DtoMetadataWorkspace();
                return _current;
            }
            set => _current = value;
        }

        public virtual bool IsDto(TypeInfo type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type.IsClass && type.GetInterface(nameof(IDto)) != null;
        }

        public virtual bool IsComplexType(TypeInfo type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type.IsClass && type.GetCustomAttribute<ComplexTypeAttribute>() != null;
        }

        public virtual TypeInfo GetFinalDtoType(TypeInfo type)
        {
            if (type.IsGenericParameter && type.GetGenericParameterConstraints().Any())
            {
                Type finalDtoType = type.GetGenericParameterConstraints().ExtendedSingleOrDefault($"Finding dto of {type.Name}", t => IsDto(t?.GetTypeInfo()));
                if (finalDtoType != null)
                    return finalDtoType.GetTypeInfo();
                return null;
            }
            else
                return type;
        }

        public virtual PropertyInfo[] GetKeyColums(TypeInfo typeInfo)
        {
            PropertyInfo[] props = typeInfo.GetProperties();

            PropertyInfo[] keys = props
                .Where(p => p.GetCustomAttribute<KeyAttribute>() != null)
                .ToArray();

            if (keys.Any())
                return keys;
            else
                return props.Where(p => p.Name == "Id" || p.Name == $"{typeInfo.Name}Id").ToArray();
        }

        public virtual object[] GetKeys(IDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            TypeInfo dtoType = dto.GetType().GetTypeInfo();

            PropertyInfo[] props = GetKeyColums(dtoType);

            return props.Select(p => p.GetValue(dto)).ToArray();
        }
    }
}
