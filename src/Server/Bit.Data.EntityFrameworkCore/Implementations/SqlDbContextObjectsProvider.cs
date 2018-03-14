using Microsoft.EntityFrameworkCore;
using System;
using System.Data.Common;
using System.Reflection;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class SqlDbContextObjectsProvider : DbContextObjectsProvider
    {
        private Lazy<MethodInfo> _UseSqlServerMethod = new Lazy<MethodInfo>(() =>
        {
            TypeInfo sqlServerDbContextOptionsExtensionsType = Type.GetType($"Microsoft.EntityFrameworkCore.SqlServerDbContextOptionsExtensions, Microsoft.EntityFrameworkCore.SqlServer, Version={typeof(DbContextOptionsBuilder).GetTypeInfo().Assembly.GetName().Version}, Culture=neutral, PublicKeyToken=adb9793829ddae60")?.GetTypeInfo();

            if (sqlServerDbContextOptionsExtensionsType == null)
                throw new InvalidOperationException("SqlServerDbContextOptionsExtensions type could not be found");

            return sqlServerDbContextOptionsExtensionsType.GetMethod("UseSqlServer", new[] { typeof(DbContextOptionsBuilder), typeof(DbConnection), typeof(Action<>).MakeGenericType(Type.GetType($"Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder, Microsoft.EntityFrameworkCore.SqlServer, Version={typeof(DbContextOptionsBuilder).GetTypeInfo().Assembly.GetName().Version}, Culture=neutral, PublicKeyToken=adb9793829ddae60")) });

        }, isThreadSafe: true);

        public override void UseDbConnection(DbConnection dbConnection, DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            _UseSqlServerMethod.Value.Invoke(null, new object[] { dbContextOptionsBuilder, dbConnection, null });
        }
    }
}