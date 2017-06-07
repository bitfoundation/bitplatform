using System;
using System.Collections.Generic;
using Bit.Data.EntityFrameworkCore.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class InMemoryDbContextObjectsProvider : IDbContextObjectsProvider
    {
        private readonly IDictionary<string, DbContextObjects> _dbContextObjects = new Dictionary<string, DbContextObjects>();

        public virtual DbContextObjects GetDbContextOptions(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (!_dbContextObjects.ContainsKey(connectionString))
            {
                DbContextOptionsBuilder dbContextOptionsBuilder = new DbContextOptionsBuilder();

                dbContextOptionsBuilder.UseInMemoryDatabase(databaseName: connectionString);

                _dbContextObjects.Add(connectionString, new DbContextObjects
                {
                    Options = dbContextOptionsBuilder.Options
                });
            }

            return _dbContextObjects[connectionString];
        }
    }
}
