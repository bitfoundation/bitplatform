using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;

namespace Bit.Data.Contracts
{
    public interface IDbConnectionProvider : IPipelineAwareDisposable
    {
        Task<DbConnection> GetDbConnectionAsync(string connectionString, bool rollbackOnScopeStatusFailure, CancellationToken cancellationToken);

        DbConnection GetDbConnection(string connectionString, bool rollbackOnScopeStatusFailure);

        DbTransaction? GetDbTransaction(DbConnection dbConnection);
    }
}
