using System.Linq;
using System.Reflection;
using Bit.Model.Contracts;

namespace System.Web.OData
{
    public static class DeltaExtensions
    {
        public static void Patch<TDto, TEntity>(this Delta<TDto> sourceDto, TEntity destinationEntity)
            where TDto : class, IDto
            where TEntity : class, IEntity
        {
            if (sourceDto == null)
                throw new ArgumentNullException(nameof(sourceDto));

            sourceDto.GetChangedPropertyNames()
                .ToList()
                .ForEach(changedPropName =>
                {
                    PropertyInfo prop = typeof(TEntity).GetTypeInfo().GetProperty(changedPropName);
                    if (prop == null)
                        return;
                    sourceDto.TryGetPropertyValue(changedPropName, out object obj);
                    if (obj != null && !prop.PropertyType.IsInstanceOfType(obj))
                        return;
                    prop.SetValue(destinationEntity, obj);
                });
        }
    }
}
