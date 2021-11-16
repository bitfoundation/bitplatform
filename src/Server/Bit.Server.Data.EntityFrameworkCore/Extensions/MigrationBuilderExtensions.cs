using Bit.Data.Implementations;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Migrations
{
    public static class MigrationBuilderExtensions
    {
        /// <summary>
        /// <seealso cref="DbJsonLogStore"/>
        /// </summary>
        public static void CreateSqlServerJsonLogStoreTable(this MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder == null)
                throw new ArgumentNullException(nameof(migrationBuilder));

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "NewSequentialId()"),
                    Contents = table.Column<string>(nullable: false),
                    Date = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "GetUTCDate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });
        }

        public static void CreateHangfireSqlObjects(this MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder == null)
                throw new ArgumentNullException(nameof(migrationBuilder));

            using (Stream hangfireJobsDatabaseStream = Assembly.Load("Bit.Server.Hangfire").GetManifestResourceStream("Bit.Hangfire.Hangfire-Database-Script.sql")!)
            {
                using (StreamReader reader = new StreamReader(hangfireJobsDatabaseStream))
                {
                    string sql = reader.ReadToEnd();
                    migrationBuilder.Sql(sql);
                }
            }
        }

        public static void AllowReadCommittedSnapshot(this MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder == null)
                throw new ArgumentNullException(nameof(migrationBuilder));

            migrationBuilder.Sql("DECLARE @dbname sysname = db_name();" +
                                 "EXEC('ALTER DATABASE [' + @dbname + '] SET READ_COMMITTED_SNAPSHOT ON');", true);
        }
    }
}
