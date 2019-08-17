using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Data.Contracts;
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
        public virtual AppEnvironment ActiveAppEnvironment { get; set; }

        public virtual IDbConnectionProvider DbConnectionProvider { get; set; }

        public virtual IContentFormatter Formatter { get; set; }

        public void SaveLog(LogEntry logEntry)
        {
            DbConnection connection = DbConnectionProvider.GetDbConnection(ActiveAppEnvironment.GetConfig(AppEnvironment.KeyValues.Data.LogDbConnectionstring, defaultValueOnNotFoundProvider: () => ActiveAppEnvironment.GetConfig<string>("AppConnectionstring")), rollbackOnScopeStatusFailure: false);

            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = DbConnectionProvider.GetDbTransaction(connection);

                command.CommandText = @"INSERT INTO [dbo].[Logs] ([Contents]) VALUES (@contents)";

                command.AddParameterWithValue("@contents", Formatter.Serialize(logEntry.ToDictionary()));

                command.ExecuteNonQuery();
            }
        }

        public virtual async Task SaveLogAsync(LogEntry logEntry)
        {
            DbConnection connection = await DbConnectionProvider.GetDbConnectionAsync(ActiveAppEnvironment.GetConfig(AppEnvironment.KeyValues.Data.LogDbConnectionstring, defaultValueOnNotFoundProvider: () => ActiveAppEnvironment.GetConfig<string>("AppConnectionstring")), rollbackOnScopeStatusFailure: false, cancellationToken: CancellationToken.None).ConfigureAwait(false);

            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = DbConnectionProvider.GetDbTransaction(connection);

                command.CommandText = @"INSERT INTO [dbo].[Logs] ([Contents]) VALUES (@contents)";

                command.AddParameterWithValue("@contents", Formatter.Serialize(logEntry.ToDictionary()));

                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}
