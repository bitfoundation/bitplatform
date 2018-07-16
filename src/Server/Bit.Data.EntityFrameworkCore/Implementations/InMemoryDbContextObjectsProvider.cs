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

        private MethodInfo GetUseInMemoryDatabaseMethod()
        {
            TypeInfo inMemoryDbContextOptionsExtensionsType = Assembly.Load("Microsoft.EntityFrameworkCore.InMemory")?.GetType("Microsoft.EntityFrameworkCore.InMemoryDbContextOptionsExtensions")?.GetTypeInfo();

            if (inMemoryDbContextOptionsExtensionsType == null)
                throw new InvalidOperationException("InMemoryDbContextOptionsExtensions type could not be found");

            return inMemoryDbContextOptionsExtensionsType.GetMethod("UseInMemoryDatabase", new[] { typeof(DbContextOptionsBuilder), typeof(string), typeof(Action<>).MakeGenericType(Assembly.Load("Microsoft.EntityFrameworkCore.InMemory").GetType("Microsoft.EntityFrameworkCore.Infrastructure.InMemoryDbContextOptionsBuilder")) });
        }

        private readonly Lazy<MethodInfo> _UseInMemoryDatabaseMethod;

        public InMemoryDbContextObjectsProvider()
        {
            _UseInMemoryDatabaseMethod = new Lazy<MethodInfo>(GetUseInMemoryDatabaseMethod, isThreadSafe: true);
        }

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
