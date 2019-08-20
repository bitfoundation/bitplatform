using Bit.Core.Implementations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NHibernate;
using NHibernate.Collection;
using NHibernate.Proxy;
using System;
using System.Collections;
using System.Reflection;

namespace Bit.Data.NHibernate.Implementations
{
    public class NHibernateContractResolver : BitContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            if (typeof(INHibernateProxy).IsAssignableFrom(objectType))
                return base.CreateContract(objectType.BaseType);
            else
                return base.CreateContract(objectType);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty jsonProp = base.CreateProperty(member, memberSerialization);

            if (jsonProp.ShouldSerialize == null)
            {
                jsonProp.ShouldSerialize = entity =>
                {
                    if (member is PropertyInfo prop)
                    {
                        if (prop.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                        {
                            FieldInfo backingField = entity.GetType().GetField($"<{member.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (backingField != null)
                            {
                                object backingFieldValue = backingField.GetValue(entity);
                                if (backingFieldValue is IPersistentCollection persistentCollection)
                                    return persistentCollection.WasInitialized;
                            }
                        }
                    }

                    return NHibernateUtil.IsPropertyInitialized(entity, member.Name);
                };
            }

            return jsonProp;
        }
    }
}
