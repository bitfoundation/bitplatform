using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace TodoTemplate.Api.Utilities
{
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Dynamically load all IEntityTypeConfiguration with Reflection
        /// </summary>
        /// <param name="modelBuilder"></param>
        /// <param name="assemblies">Assemblies contains Entities</param>
        public static void RegisterEntityTypeConfiguration(this ModelBuilder modelBuilder, params Assembly[] assemblies)
        {
            MethodInfo applyGenericMethod = typeof(ModelBuilder).GetMethods().First(m => m.Name == nameof(ModelBuilder.ApplyConfiguration));

            IEnumerable<Type> types = assemblies.SelectMany(a => a.GetExportedTypes())
                .Where(c => c.IsClass && !c.IsAbstract && c.IsPublic);

            foreach (Type type in types)
            {
                foreach (Type iface in type.GetInterfaces())
                {
                    if (iface.IsConstructedGenericType && iface.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
                    {
                        MethodInfo applyConcreteMethod = applyGenericMethod.MakeGenericMethod(iface.GenericTypeArguments[0]);
                        applyConcreteMethod.Invoke(modelBuilder, new object[] { Activator.CreateInstance(type) });
                    }
                }
            }
        }
    }
}
