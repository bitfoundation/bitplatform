using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Bit.Data.EntityFrameworkCore.Contracts
{
    public interface IDbContextObjectsProvider
    {
        DbContextObjects GetDbContextOptions(string connectionString);
    }

    public class DbContextObjects
    {
        public virtual DbContextOptions Options { get; set; }

        public virtual DbTransaction Transaction { get; set; }

        public virtual DbConnection Connection { get; set; }
    }
}
