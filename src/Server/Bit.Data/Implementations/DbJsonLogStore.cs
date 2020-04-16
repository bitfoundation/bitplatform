using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Data.Contracts;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.Implementations
{
    /// <summary>
    /// select * from 
    /// Logs
    /// where JSON_VALUE(Contents, N'$.UserId') = 'YOUR_USER_ID'
    /// </summary>
    public class DbJsonLogStore : ILogStore
    {
        public virtual AppEnvironment ActiveAppEnvironment { get; set; } = default!;

        public virtual IDbConnectionProvider DbConnectionProvider { get; set; } = default!;

        public virtual IContentFormatter ContentFormatter { get; set; } = default!;

        public void SaveLog(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException(nameof(logEntry));

            DbConnection connection = DbConnectionProvider.GetDbConnection(ActiveAppEnvironment.GetConfig(AppEnvironment.KeyValues.Data.LogDbConnectionstring, defaultValueOnNotFoundProvider: () => ActiveAppEnvironment.GetConfig<string>("AppConnectionstring")) ?? throw new InvalidOperationException("Log connection string could not be found."), rollbackOnScopeStatusFailure: false);

            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = DbConnectionProvider.GetDbTransaction(connection);

                command.CommandText = @"INSERT INTO [dbo].[Logs] ([Contents]) VALUES (@contents)";

                command.AddParameterWithValue("@contents", ContentFormatter.Serialize(logEntry.ToDictionary()));

                command.ExecuteNonQuery();
            }
        }

        public virtual async Task SaveLogAsync(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException(nameof(logEntry));

            DbConnection connection = await DbConnectionProvider.GetDbConnectionAsync(ActiveAppEnvironment.GetConfig(AppEnvironment.KeyValues.Data.LogDbConnectionstring, defaultValueOnNotFoundProvider: () => ActiveAppEnvironment.GetConfig<string>("AppConnectionstring")) ?? throw new InvalidOperationException("Log connection string could not be found."), rollbackOnScopeStatusFailure: false, cancellationToken: CancellationToken.None).ConfigureAwait(false);

#if DotNet
            using (DbCommand command = connection.CreateCommand())
#else
            await using (DbCommand command = connection.CreateCommand())
#endif
            {
                command.Transaction = DbConnectionProvider.GetDbTransaction(connection);

                command.CommandText = @"INSERT INTO [dbo].[Logs] ([Contents]) VALUES (@contents)";

                command.AddParameterWithValue("@contents", ContentFormatter.Serialize(logEntry.ToDictionary()));

                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}
