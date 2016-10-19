using Foundation.Model.Contracts;
using System.Linq;
using System.Reflection;

namespace System.Web.OData
{
    public static class DeltaExtensions
    {
        public static void Patch<TSourceDto, TSourceModel>(this Delta<TSourceDto> source, TSourceModel dest)
            where TSourceDto : class, IDto
            where TSourceModel : class, IEntity
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            source.GetChangedPropertyNames()
                .ToList()
                .ForEach(changedPropName =>
                {
                    PropertyInfo prop = typeof(TSourceModel).GetTypeInfo().GetProperty(changedPropName);
                    if (prop == null)
                        return;
                    object obj = null;
                    source.TryGetPropertyValue(changedPropName, out obj);
                    if (obj != null && !prop.PropertyType.IsAssignableFrom(obj.GetType()))
                        return;
                    prop.SetValue(dest, obj);
                });
        }
    }
}
