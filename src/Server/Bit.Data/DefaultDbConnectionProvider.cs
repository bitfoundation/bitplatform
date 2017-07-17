using Bit.Core.Contracts;
using Bit.Data.Contracts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data
{
    public class DefaultDbConnectionProvider<TDbConnection> : IDbConnectionProvider
        where TDbConnection : DbConnection, new()
    {
        private readonly IDictionary<string, DbConnectionAndTransactionPair> _connections =
            new Dictionary<string, DbConnectionAndTransactionPair>();

        private readonly IScopeStatusManager _scopeStatusManager;

#if DEBUG
        protected DefaultDbConnectionProvider()
        {
        }
#endif

        public DefaultDbConnectionProvider(IScopeStatusManager scopeStatusManager)
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
                TDbConnection newConnection = new TDbConnection();
                newConnection.ConnectionString = connectionString;
                newConnection.Open();
                DbTransaction transaction = newConnection.BeginTransaction(IsolationLevel.ReadCommitted);
                _connections.Add(connectionString, new DbConnectionAndTransactionPair(newConnection, transaction, rollbackOnScopeStatusFailure));
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
                TDbConnection newDbConnection = new TDbConnection();
                newDbConnection.ConnectionString = connectionString;
                await newDbConnection.OpenAsync(cancellationToken);
                DbTransaction transaction = newDbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
                _connections.Add(connectionString, new DbConnectionAndTransactionPair(newDbConnection, transaction, rollbackOnScopeStatusFailure));
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
            public DbConnectionAndTransactionPair(TDbConnection connection, DbTransaction transaction, bool rollbackOnScopeStatusFailure)
            {
                Connection = connection;
                Transaction = transaction;
                RollbackOnScopeStatusFailure = rollbackOnScopeStatusFailure;
            }

            public TDbConnection Connection { get; }

            public DbTransaction Transaction { get; }

            public bool RollbackOnScopeStatusFailure { get; }
        }
    }
}