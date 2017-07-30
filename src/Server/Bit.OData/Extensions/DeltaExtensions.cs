using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bit.Model.Contracts;

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
                    if (obj != null && !prop.PropertyType.IsInstanceOfType(obj))
                        return;
                    prop.SetValue(dest, obj);
                });
        }

        public static void IgnoreChanges<TDto>(this Delta<TDto> source, params string[] members)
            where TDto : class, IDto
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (string member in members)
            {
                (((HashSet<string>)source.GetChangedPropertyNames())).Remove(member);
            }
        }
    }
}
