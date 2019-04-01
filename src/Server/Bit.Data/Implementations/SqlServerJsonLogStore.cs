using Bit.Core.Contracts;
using Bit.Core.Models;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Bit.Data.Implementations
{
    /// <summary>
    /// select * from 
    /// Logs
    /// where JSON_VALUE(Contents, N'$.UserId') = 'YOUR_USER_ID'
    /// </summary>
    public class SqlServerJsonLogStore : ILogStore
    {
        public virtual AppEnvironment ActiveAppEnvironment { get; set; }

        public virtual IContentFormatter Formatter { get; set; }

        public void SaveLog(LogEntry logEntry)
        {
            using (SqlConnection connection = new SqlConnection(ActiveAppEnvironment.GetConfig("LogDbConnectionstring", defaultValueOnNotFoundProvider: () => ActiveAppEnvironment.GetConfig<string>("AppConnectionstring"))))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"INSERT INTO [dbo].[Logs] ([Contents]) VALUES (@contents)";

                    command.Parameters.AddWithValue("@contents", Formatter.Serialize(logEntry.ToDictionary()));

                    connection.Open();

                    command.ExecuteNonQuery();
                }
            }
        }

        public virtual async Task SaveLogAsync(LogEntry logEntry)
        {
            using (SqlConnection connection = new SqlConnection(ActiveAppEnvironment.GetConfig("LogDbConnectionstring", defaultValueOnNotFoundProvider: () => ActiveAppEnvironment.GetConfig<string>("AppConnectionstring"))))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"INSERT INTO [dbo].[Logs] ([Contents]) VALUES (@contents)";

                    command.Parameters.AddWithValue("@contents", Formatter.Serialize(logEntry.ToDictionary()));

                    await connection.OpenAsync().ConfigureAwait(false);

                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
