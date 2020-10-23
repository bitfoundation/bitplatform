using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Prism.AppModel;

namespace Prism.Services.Dialogs.Popups
{
    /*internal static class AutoInitializeHelpers
    {
        public static bool HasKey(this IEnumerable<KeyValuePair<string, object>> parameters, string name, out string key)
        {
            key = parameters.Select(x => x.Key).FirstOrDefault(k => k.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return !string.IsNullOrEmpty(key);
        }

        public static (string Name, bool IsRequired) GetAutoInitializeProperty(this PropertyInfo pi)
        {
            var attr = pi.GetCustomAttribute<AutoInitializeAttribute>();
            if (attr is null)
            {
                return (pi.Name, false);
            }

            return (string.IsNullOrEmpty(attr.Name) ? pi.Name : attr.Name, attr.IsRequired);
        }
    }*/
}
