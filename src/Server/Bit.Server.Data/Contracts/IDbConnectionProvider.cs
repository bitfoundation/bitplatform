using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.Contracts
{
    public interface IDbConnectionProvider : IDisposable, IAsyncDisposable
    {
        Task<DbConnection> GetDbConnectionAsync(string connectionString, bool rollbackOnScopeStatusFailure, CancellationToken cancellationToken);

        DbConnection GetDbConnection(string connectionString, bool rollbackOnScopeStatusFailure);

        DbTransaction? GetDbTransaction(DbConnection dbConnection);
    }
}