using Bit.Model.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data
{
    public interface IsSyncDbContext
    {
        bool IsSyncDbContext { get; set; }
    }

    public class EfCoreDbContextBase : DbContext, IsSyncDbContext
    {
        bool IsSyncDbContext.IsSyncDbContext { get; set; }

        public EfCoreDbContextBase(DbContextOptions options)
            : base(options)
        {
            ApplyDefaultConfig();
        }

        public EfCoreDbContextBase()
            : base()
        {
            ApplyDefaultConfig();
        }

        public virtual bool ChangeTrackingEnabled()
        {
            return true;
        }

        protected virtual void ApplyDefaultConfig()
        {
            if (ChangeTrackingEnabled() == false)
                ChangeTracker.AutoDetectChangesEnabled = false;
        }

        public virtual async Task UpsertDtoAsync<TDto>(TDto dto, CancellationToken cancellationToken = default) // https://github.com/aspnet/EntityFrameworkCore/issues/9249
            where TDto : class, IDto
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (((IsSyncDbContext)this).IsSyncDbContext == false && dto is ISyncableDto syncableDto)
                Entry(syncableDto).Property("IsSynced").CurrentValue = false;

            TypeInfo dtoType = dto.GetType().GetTypeInfo();

            IEntityType efCoreDtoType = Model.FindEntityType(dtoType);

            string tableName = $"[{efCoreDtoType.GetTableName()}]"; // No schema support

            var props = efCoreDtoType.GetProperties()
                .Select(p => new { p.Name, Value = (object?)Entry(dto).Property(p.Name).CurrentValue })
                .ToArray();

            foreach (INavigation nav in efCoreDtoType.GetNavigations())
            {
                if (nav.ClrType.GetCustomAttribute<ComplexTypeAttribute>() == null)
                    continue;

                object? navInstance = nav.PropertyInfo.GetValue(dto);

                if (navInstance == null)
                    continue;

                props = props.Union(nav.ClrType.GetProperties()
                    .Select(p => new { Name = $"{nav.Name}_{p.Name}", Value = p.GetValue(navInstance) }))
                    .ToArray();
            }

            string sql = $"INSERT OR REPLACE INTO {tableName} ({string.Join(",", props.Select(p => $"[{p.Name}]"))}) VALUES({string.Join(",", props.Select((p, i) => $"{{{i}}}"))})";

            await Database.ExecuteSqlCommandAsync(sql, props.Select(p => p.Value), cancellationToken).ConfigureAwait(false);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder == null)
                throw new ArgumentNullException(nameof(optionsBuilder));

            if (ChangeTrackingEnabled() == false)
            {
                optionsBuilder = optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }

            base.OnConfiguring(optionsBuilder);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnSaveChanges();

            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            OnSaveChanges();

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected virtual void OnSaveChanges()
        {
            ChangeTracker.DetectChanges();

            if (((IsSyncDbContext)this).IsSyncDbContext == false)
            {
                foreach (EntityEntry syncableDtoEntry in ChangeTracker.Entries().ToList())
                {
                    if (syncableDtoEntry.Entity is ISyncableDto syncableDto)
                    {
                        if (syncableDtoEntry.State == EntityState.Deleted && syncableDto.Version != 0)
                        {
                            syncableDto.IsArchived = true;
                            syncableDtoEntry.State = EntityState.Modified;
                        }

                        if (syncableDtoEntry.State == EntityState.Modified || syncableDtoEntry.State == EntityState.Added)
                        {
                            Entry(syncableDto).Property("IsSynced").CurrentValue = false;
                        }
                    }
                }
            }
        }
    }
}
