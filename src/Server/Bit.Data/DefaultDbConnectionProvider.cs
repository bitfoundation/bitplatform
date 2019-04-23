using Bit.Core.Contracts;
using Bit.Data.Contracts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data
{
    public class DefaultDbConnectionProvider<TDbConnection> : IDbConnectionProvider
        where TDbConnection : DbConnection, new()
    {
        private readonly ICollection<DbConnectionAndTransactionPair> _dbConnectionAndTransactions =
            new List<DbConnectionAndTransactionPair>();

        public virtual IScopeStatusManager ScopeStatusManager { get; set; }

        public virtual IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

        public virtual DbTransaction GetDbTransaction(DbConnection dbConnection)
        {
            if (dbConnection == null)
                throw new ArgumentNullException(nameof(dbConnection));

            return _dbConnectionAndTransactions.ExtendedSingle($"Getting db transaction for db connection", dbAndTran => dbAndTran.Connection == dbConnection).Transaction;
        }

        public virtual DbConnection GetDbConnection(string connectionString, bool rollbackOnScopeStatusFailure)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (!_dbConnectionAndTransactions.Any(dbAndTran => dbAndTran.ConnectionString != connectionString))
            {
                TDbConnection newConnection = new TDbConnection { ConnectionString = connectionString };
                DbTransaction transaction = null;
                try
                {
                    newConnection.Open();
                    transaction = newConnection.BeginTransaction(IsolationLevel);
                }
                catch { }
                _dbConnectionAndTransactions.Add(new DbConnectionAndTransactionPair(connectionString, newConnection, transaction, rollbackOnScopeStatusFailure));
            }

            return _dbConnectionAndTransactions.ExtendedSingle($"Getting db connection for {connectionString}", dbAndTran => dbAndTran.ConnectionString == connectionString).Connection;
        }

        public virtual async Task<DbConnection> GetDbConnectionAsync(string connectionString, bool rollbackOnScopeStatusFailure,
            CancellationToken cancellationToken)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (!_dbConnectionAndTransactions.Any(dbAndTran => dbAndTran.ConnectionString != connectionString))
            {
                TDbConnection newConnection = new TDbConnection { ConnectionString = connectionString };
                DbTransaction transaction = null;
                try
                {
                    await newConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    transaction = newConnection.BeginTransaction(IsolationLevel);
                }
                catch { }
                _dbConnectionAndTransactions.Add(new DbConnectionAndTransactionPair(connectionString, newConnection, transaction, rollbackOnScopeStatusFailure));
            }

            return _dbConnectionAndTransactions.ExtendedSingle($"Getting db connection for {connectionString}", dbAndTran => dbAndTran.ConnectionString == connectionString).Connection;
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            bool wasSucceeded = ScopeStatusManager.WasSucceeded();

            foreach (DbConnectionAndTransactionPair dbAndTran in _dbConnectionAndTransactions)
            {
                try
                {
                    if (dbAndTran.RollbackOnScopeStatusFailure == false)
                        dbAndTran.Transaction?.Commit();
                    else
                    {
                        if (wasSucceeded)
                            dbAndTran.Transaction?.Commit();
                        else
                            dbAndTran.Transaction?.Rollback();
                    }
                }
                finally
                {
                    dbAndTran.Transaction?.Dispose();
                    dbAndTran.Connection.Dispose();
                }
            }
        }

        public class DbConnectionAndTransactionPair
        {
            public DbConnectionAndTransactionPair(string connectionString, TDbConnection connection, DbTransaction transaction, bool rollbackOnScopeStatusFailure)
            {
                Connection = connection;
                Transaction = transaction;
                RollbackOnScopeStatusFailure = rollbackOnScopeStatusFailure;
                ConnectionString = connectionString;
            }

            public TDbConnection Connection { get; }

            public DbTransaction Transaction { get; }

            public virtual string ConnectionString { get; }

            public bool RollbackOnScopeStatusFailure { get; }
        }
    }
}