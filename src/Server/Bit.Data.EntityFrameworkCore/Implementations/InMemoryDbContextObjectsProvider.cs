using Bit.Data.EntityFrameworkCore.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class InMemoryDbContextObjectsProvider : IDbContextObjectsProvider
    {
        private readonly IDictionary<string, DbContextObjects> _dbContextObjects = new Dictionary<string, DbContextObjects>();

        private Lazy<MethodInfo> _UseInMemoryDatabaseMethod = new Lazy<MethodInfo>(() =>
        {
            TypeInfo inMemoryDbContextOptionsExtensionsType = Type.GetType($"Microsoft.EntityFrameworkCore.InMemoryDbContextOptionsExtensions, Microsoft.EntityFrameworkCore.InMemory, Version={typeof(DbContextOptionsBuilder).GetTypeInfo().Assembly.GetName().Version}, Culture=neutral, PublicKeyToken=adb9793829ddae60")?.GetTypeInfo();

            if (inMemoryDbContextOptionsExtensionsType == null)
                throw new InvalidOperationException("InMemoryDbContextOptionsExtensions type could not be found");

            return inMemoryDbContextOptionsExtensionsType.GetMethod("UseInMemoryDatabase", new[] { typeof(DbContextOptionsBuilder), typeof(string), typeof(Action<>).MakeGenericType(Type.GetType($"Microsoft.EntityFrameworkCore.Infrastructure.InMemoryDbContextOptionsBuilder, Microsoft.EntityFrameworkCore.InMemory, Version={typeof(DbContextOptionsBuilder).GetTypeInfo().Assembly.GetName().Version}, Culture=neutral, PublicKeyToken=adb9793829ddae60")) });

        }, isThreadSafe: true);

        public virtual DbContextObjects GetDbContextOptions(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (!_dbContextObjects.ContainsKey(connectionString))
            {
                DbContextOptionsBuilder dbContextOptionsBuilder = new DbContextOptionsBuilder();

                _UseInMemoryDatabaseMethod.Value.Invoke(null, new object[] { dbContextOptionsBuilder, connectionString, null });

                _dbContextObjects.Add(connectionString, new DbContextObjects
                {
                    Options = dbContextOptionsBuilder.Options
                });
            }

            return _dbContextObjects[connectionString];
        }
    }
}
