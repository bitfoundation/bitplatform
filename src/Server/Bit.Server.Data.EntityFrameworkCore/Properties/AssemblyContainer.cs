using Microsoft.EntityFrameworkCore.Migrations;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetServerDataEntityFrameworkCoreAssembly(this AssemblyContainer container)
        {
            return typeof(MigrationBuilderExtensions).GetTypeInfo().Assembly;
        }
    }
}
