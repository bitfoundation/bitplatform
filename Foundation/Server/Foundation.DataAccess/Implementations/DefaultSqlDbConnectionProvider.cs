using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Foundation.Core.Contracts;
using Foundation.DataAccess.Contracts;

namespace Foundation.DataAccess.Implementations
{
    public class DefaultSqlDbConnectionProvider : IDbConnectionProvider
    {
        private readonly IDictionary<string, DbConnectionAndTransactionPair> _connections =
            new Dictionary<string, DbConnectionAndTransactionPair>();

        private readonly IScopeStatusManager _scopeStatusManager;

        protected DefaultSqlDbConnectionProvider()
        {
        }

        public DefaultSqlDbConnectionProvider(IScopeStatusManager scopeStatusManager)
        {
            if (scopeStatusManager == null)
                throw new ArgumentNullException(nameof(scopeStatusManager));

            _scopeStatusManager = scopeStatusManager;
        }

        public virtual DbTransaction GetDbTransaction(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (!_connections.ContainsKey(connectionString))
            {
                throw new InvalidOperationException("No connection was created");
            }

            return _connections[connectionString].Transaction;
        }

        public virtual DbConnection GetDbConnection(string connectionString, bool rollbackOnScopeStatusFailure)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (!_connections.ContainsKey(connectionString))
            {
                SqlConnection newSqlConnection = new SqlConnection(connectionString);
                newSqlConnection.Open();
                SqlTransaction transaction = newSqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
                _connections.Add(connectionString, new DbConnectionAndTransactionPair(newSqlConnection, transaction, rollbackOnScopeStatusFailure));
            }

            return _connections[connectionString].Connection;
        }

        public virtual async Task<DbConnection> GetDbConnectionAsync(string connectionString, bool rollbackOnScopeStatusFailure,
            CancellationToken cancellationToken)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (!_connections.ContainsKey(connectionString))
            {
                SqlConnection newSqlConnection = new SqlConnection(connectionString);
                await newSqlConnection.OpenAsync(cancellationToken);
                SqlTransaction transaction = newSqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
                _connections.Add(connectionString, new DbConnectionAndTransactionPair(newSqlConnection, transaction, rollbackOnScopeStatusFailure));
            }

            return _connections[connectionString].Connection;
        }

        public virtual void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            bool wasSucceeded = _scopeStatusManager.WasSucceeded();

            foreach (DbConnectionAndTransactionPair connectionAndTransaction in _connections.Values)
            {
                try
                {
                    if (connectionAndTransaction.RollbackOnScopeStatusFailure == false)
                        connectionAndTransaction.Transaction.Commit();
                    else
                    {
                        if (wasSucceeded)
                            connectionAndTransaction.Transaction.Commit();
                        else
                            connectionAndTransaction.Transaction.Rollback();
                    }
                }
                finally
                {
                    connectionAndTransaction.Connection.Dispose();
                }
            }
        }

        private class DbConnectionAndTransactionPair
        {
            public DbConnectionAndTransactionPair(SqlConnection connection, SqlTransaction transaction, bool rollbackOnScopeStatusFailure)
            {
                Connection = connection;
                Transaction = transaction;
                RollbackOnScopeStatusFailure = rollbackOnScopeStatusFailure;
            }

            public SqlConnection Connection { get; }

            public DbTransaction Transaction { get; }

            public bool RollbackOnScopeStatusFailure { get; }
        }
    }
}