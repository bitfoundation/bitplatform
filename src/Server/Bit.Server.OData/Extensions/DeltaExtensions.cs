using Bit.Model.Contracts;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNet.OData
{
    public static class DeltaExtensions
    {
        public static void Patch<TDto, TEntity>(this Delta<TDto> sourceDto, TEntity destinationEntity)
            where TDto : class
            where TEntity : class, IEntity
        {
            if (sourceDto == null)
                throw new ArgumentNullException(nameof(sourceDto));

            sourceDto.GetChangedPropertyNames()
                .ToList()
                .ForEach(changedPropName =>
                {
                    PropertyInfo? prop = typeof(TEntity).GetTypeInfo().GetProperty(changedPropName);

                    if (prop == null)
                        return;
                    sourceDto.TryGetPropertyValue(changedPropName, out object obj);
                    if (obj != null && !prop.PropertyType.IsInstanceOfType(obj))
                        return;
                    prop.SetValue(destinationEntity, obj);
                });
        }

        public static bool IsChangedProperty<TDto>(this Delta<TDto> dto, Expression<Func<TDto, object>> prop)
            where TDto : class
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (prop == null)
                throw new ArgumentNullException(nameof(prop));

            string memberName = ((MemberExpression)prop.Body).Member.Name;

            return dto.GetChangedPropertyNames().Any(p => p == memberName);
        }
    }
}
