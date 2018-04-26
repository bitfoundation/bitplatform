using Bit.Core.Contracts;
using Bit.Core.Models;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Bit.Data.Implementations
{
    public class SqlServerJsonLogStore : ILogStore
    {
        public virtual AppEnvironment ActiveAppEnvironment { get; set; }

        public virtual IContentFormatter Formatter { get; set; }

        public void SaveLog(LogEntry logEntry)
        {
            using (SqlConnection connection = new SqlConnection(ActiveAppEnvironment.GetConfig<string>("AppConnectionstring")))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"INSERT INTO [dbo].[Logs] ([Contents]) VALUES (@contents)";

                    command.Parameters.AddWithValue("@contents", Formatter.Serialize(logEntry));

                    connection.Open();

                    command.ExecuteNonQuery();
                }
            }
        }

        public virtual async Task SaveLogAsync(LogEntry logEntry)
        {
            using (SqlConnection connection = new SqlConnection(ActiveAppEnvironment.GetConfig<string>("AppConnectionstring")))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"INSERT INTO [dbo].[Logs] ([Contents]) VALUES (@contents)";

                    command.Parameters.AddWithValue("@contents", Formatter.Serialize(logEntry));

                    await connection.OpenAsync();

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
