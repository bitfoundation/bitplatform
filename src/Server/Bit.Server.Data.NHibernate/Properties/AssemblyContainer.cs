using Bit.Data.NHibernate.Implementations;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetServerDataNHibernateAssembly(this AssemblyContainer container)
        {
            return typeof(NHRepository<>).GetTypeInfo().Assembly;
        }
    }
}
