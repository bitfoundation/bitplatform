using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public abstract class DbContextObjectsProvider : IDbContextObjectsProvider
    {
        public virtual IDbConnectionProvider DbConnectionProvider { get; set; }

        private readonly IDictionary<string, DbContextObjects> _dbContextObjects =
            new Dictionary<string, DbContextObjects>();

        public abstract void UseDbConnection(DbConnection dbConnection, DbContextOptionsBuilder dbContextOptionsBuilder);

        public virtual DbContextObjects GetDbContextOptions(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (!_dbContextObjects.ContainsKey(connectionString))
            {
                DbConnection dbConnection = DbConnectionProvider.GetDbConnection(connectionString, rollbackOnScopeStatusFailure: true);

                DbContextOptionsBuilder dbContextOptionsBuilder = new DbContextOptionsBuilder();

                UseDbConnection(dbConnection, dbContextOptionsBuilder);

                _dbContextObjects.Add(connectionString, new DbContextObjects
                {
                    Transaction = DbConnectionProvider.GetDbTransaction(connectionString),
                    Connection = dbConnection,
                    Options = dbContextOptionsBuilder.Options
                });
            }

            return _dbContextObjects[connectionString];
        }
    }
}