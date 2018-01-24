using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class SqlDbContextObjectsProvider : DbContextObjectsProvider
    {
        public override void UseDbConnection(DbConnection dbConnection, DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            dbContextOptionsBuilder.UseSqlServer(dbConnection);
        }
    }
}