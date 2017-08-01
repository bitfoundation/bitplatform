using System.Data.Common;
using System.Data.Entity;
using Bit.Data.Contracts;

namespace Bit.Data.EntityFramework.Implementations
{
    /// <summary>
    /// Entity Framework DbContext, optimized for N-Tier app development
    /// </summary>
    public class EfDbContextBase : DbContext
    {
        protected EfDbContextBase()
        {
        }

        public EfDbContextBase(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            ApplyDefaultConfig();
        }

        public EfDbContextBase(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            ApplyDefaultConfig();
        }

        public EfDbContextBase(string connectionString, IDbConnectionProvider dbConnectionProvider)
            : base(dbConnectionProvider.GetDbConnection(connectionString, rollbackOnScopeStatusFailure: true), contextOwnsConnection: false)
        {
            ApplyDefaultConfig();
            Database.UseTransaction(dbConnectionProvider.GetDbTransaction(connectionString));
        }

        private void ApplyDefaultConfig()
        {
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.EnsureTransactionsForFunctionsAndCommands = true;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.UseDatabaseNullSemantics = false;
            Configuration.ValidateOnSaveEnabled = true;
        }
    }
}