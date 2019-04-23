using Bit.Data.Contracts;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;

namespace Bit.Data.EntityFramework.Implementations
{
    /// <summary>
    /// Entity Framework DbContext, optimized for N-Tier app development
    /// </summary>
    public class EfDbContextBase : DbContext
    {
        public EfDbContextBase(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            ApplyDefaultConfig();
        }

        public EfDbContextBase(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
            ApplyDefaultConfig();
        }

        public EfDbContextBase(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            ApplyDefaultConfig();
        }

        public EfDbContextBase(ObjectContext objectContext, bool dbContextOwnsObjectContext)
            : base(objectContext, dbContextOwnsObjectContext)
        {
            ApplyDefaultConfig();
        }

        public EfDbContextBase(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
            ApplyDefaultConfig();
        }

        public EfDbContextBase(DbCompiledModel model)
            : base(model)
        {
            ApplyDefaultConfig();
        }

        public EfDbContextBase()
            : base()
        {
            ApplyDefaultConfig();
        }

        public EfDbContextBase(string connectionString, IDbConnectionProvider dbConnectionProvider)
            : this(dbConnectionProvider.GetDbConnection(connectionString, rollbackOnScopeStatusFailure: true), contextOwnsConnection: false)
        {
            Database.UseTransaction(dbConnectionProvider.GetDbTransaction(Database.Connection));
        }

        public virtual bool ChangeTrackingEnabled()
        {
            return true;
        }

        private void ApplyDefaultConfig()
        {
            if (ChangeTrackingEnabled() == false)
                Configuration.AutoDetectChangesEnabled = false;
            Configuration.EnsureTransactionsForFunctionsAndCommands = true;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.UseDatabaseNullSemantics = false;
            Configuration.ValidateOnSaveEnabled = true;
        }
    }
}
