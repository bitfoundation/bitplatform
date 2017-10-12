using System;
using System.Collections.Generic;
using System.Data.Common;
using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class SqlDbContextObjectsProvider : IDbContextObjectsProvider
    {
        public virtual IDbConnectionProvider DbConnectionProvider { get; set; }

        private readonly IDictionary<string, DbContextObjects> _dbContextObjects =
            new Dictionary<string, DbContextObjects>();

        public virtual DbContextObjects GetDbContextOptions(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (!_dbContextObjects.ContainsKey(connectionString))
            {
                DbContextOptionsBuilder dbContextOptionsBuilder = new DbContextOptionsBuilder();

                DbConnection dbConnection = DbConnectionProvider.GetDbConnection(connectionString, rollbackOnScopeStatusFailure: true);

                dbContextOptionsBuilder.UseSqlServer(dbConnection);

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