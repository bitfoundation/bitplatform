using System.Linq;

namespace System.Reflection
{
    public static class AssemblyExtensions
    {
        public static TypeInfo[] GetLoadableExportedTypes(this Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            try
            {
                return assembly.GetExportedTypes().Select(t => t.GetTypeInfo()).ToArray();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Select(t => t.GetTypeInfo()).ToArray();
            }
        }
    }
}
