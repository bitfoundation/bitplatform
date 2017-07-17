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
        private readonly IDbConnectionProvider _dbConnectionProvider;

        private readonly IDictionary<string, DbContextObjects> _dbContextObjects =
            new Dictionary<string, DbContextObjects>();

#if DEBUG
        protected SqlDbContextObjectsProvider()
        {
        }
#endif

        public SqlDbContextObjectsProvider(IDbConnectionProvider dbConnectionProvider)
        {
            if (dbConnectionProvider == null)
                throw new ArgumentNullException(nameof(dbConnectionProvider));

            _dbConnectionProvider = dbConnectionProvider;
        }

        public virtual DbContextObjects GetDbContextOptions(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (!_dbContextObjects.ContainsKey(connectionString))
            {
                DbContextOptionsBuilder dbContextOptionsBuilder = new DbContextOptionsBuilder();

                DbConnection dbConnection = _dbConnectionProvider.GetDbConnection(connectionString, rollbackOnScopeStatusFailure: true);

                dbContextOptionsBuilder.UseSqlServer(dbConnection);

                _dbContextObjects.Add(connectionString, new DbContextObjects
                {
                    Transaction = _dbConnectionProvider.GetDbTransaction(connectionString),
                    Connection = dbConnection,
                    Options = dbContextOptionsBuilder.Options
                });
            }

            return _dbContextObjects[connectionString];
        }
    }
}