using Microsoft.EntityFrameworkCore;
using System;
using System.Data.Common;
using System.Reflection;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class SqliteDbContextObjectsProvider : DbContextObjectsProvider
    {
        public SqliteDbContextObjectsProvider()
        {
            _UseSqliteMethod = new Lazy<MethodInfo>(GetUseSqliteMethod, isThreadSafe: true);
        }

        public MethodInfo GetUseSqliteMethod()
        {
            TypeInfo sqliteDbContextOptionsExtensionsType = Assembly.Load("Microsoft.EntityFrameworkCore.Sqlite")?.GetType("Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions")?.GetTypeInfo();

            if (sqliteDbContextOptionsExtensionsType == null)
                throw new InvalidOperationException("SqliteDbContextOptionsBuilderExtensions type could not be found");

            return sqliteDbContextOptionsExtensionsType.GetMethod("UseSqlite", new[] { typeof(DbContextOptionsBuilder), typeof(DbConnection), typeof(Action<>).MakeGenericType(Assembly.Load("Microsoft.EntityFrameworkCore.Sqlite").GetType("Microsoft.EntityFrameworkCore.Infrastructure.SqliteDbContextOptionsBuilder")) });
        }

        private readonly Lazy<MethodInfo> _UseSqliteMethod;

        public override void UseDbConnection(DbConnection dbConnection, DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            _UseSqliteMethod.Value.Invoke(null, new object[] { dbContextOptionsBuilder, dbConnection, null });
        }
    }
}
