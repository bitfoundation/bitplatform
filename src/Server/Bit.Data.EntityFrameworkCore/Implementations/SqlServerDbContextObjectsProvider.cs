using Microsoft.EntityFrameworkCore;
using System;
using System.Data.Common;
using System.Reflection;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class SqlServerDbContextObjectsProvider : DbContextObjectsProvider
    {
        public SqlServerDbContextObjectsProvider()
        {
            _useSqlServerMethod = new Lazy<MethodInfo>(GetUseSqlServerMethod, isThreadSafe: true);
        }

        private MethodInfo GetUseSqlServerMethod()
        {
            TypeInfo sqlServerDbContextOptionsExtensionsType = Assembly.Load("Microsoft.EntityFrameworkCore.SqlServer")?.GetType("Microsoft.EntityFrameworkCore.SqlServerDbContextOptionsExtensions")?.GetTypeInfo();

            if (sqlServerDbContextOptionsExtensionsType == null)
                throw new InvalidOperationException("SqlServerDbContextOptionsExtensions type could not be found");

            return sqlServerDbContextOptionsExtensionsType.GetMethod("UseSqlServer", new[] { typeof(DbContextOptionsBuilder), typeof(DbConnection), typeof(Action<>).MakeGenericType(Assembly.Load("Microsoft.EntityFrameworkCore.SqlServer").GetType("Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder")) });
        }

        private readonly Lazy<MethodInfo> _useSqlServerMethod;

        public override void UseDbConnection(DbConnection dbConnection, DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            _useSqlServerMethod.Value.Invoke(null, new object[] { dbContextOptionsBuilder, dbConnection, null });
        }
    }
}
