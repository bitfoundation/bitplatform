using Bit.Data.Contracts;
using Bit.Model.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.Implementations
{
    public class EfCoreRepository<TDbContext, TDto> : IRepository<TDto>
        where TDto : class, IDto
        where TDbContext : DbContext
    {
        private TDbContext _DbContext;

        public virtual TDbContext DbContext
        {
            get => _DbContext;
            set
            {
                _DbContext = value;
                Set = _DbContext.Set<TDto>();
            }
        }

        public virtual DbSet<TDto> Set { get; protected set; }

        public virtual async Task<TDto> AddAsync(TDto dtoToAdd, CancellationToken cancellationToken = default)
        {
            if (dtoToAdd == null)
                throw new ArgumentNullException(nameof(dtoToAdd));

            try
            {
                await DbContext.AddAsync(dtoToAdd, cancellationToken).ConfigureAwait(false);

                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return dtoToAdd;
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(dtoToAdd);
            }
        }

        public virtual async Task<IEnumerable<TDto>> AddRangeAsync(IEnumerable<TDto> dtosToAdd, CancellationToken cancellationToken = default)
        {
            if (dtosToAdd == null)
                throw new ArgumentNullException(nameof(dtosToAdd));

            List<TDto> dtosToAddList = dtosToAdd as List<TDto> ?? dtosToAdd.ToList();

            try
            {
                await DbContext.AddRangeAsync(dtosToAddList, cancellationToken).ConfigureAwait(false);

                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return dtosToAddList;
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    dtosToAddList.ForEach(Detach);
            }
        }

        public virtual async Task<TDto> UpdateAsync(TDto dtoToUpdate, CancellationToken cancellationToken = default)
        {
            if (dtoToUpdate == null)
                throw new ArgumentNullException(nameof(dtoToUpdate));

            try
            {
                Attach(dtoToUpdate);
                DbContext.Entry(dtoToUpdate).State = EntityState.Modified;

                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return dtoToUpdate;
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(dtoToUpdate);
            }
        }

        public virtual async Task<TDto> DeleteAsync(TDto dtoToDelete, CancellationToken cancellationToken = default)
        {
            if (dtoToDelete == null)
                throw new ArgumentNullException(nameof(dtoToDelete));

            try
            {
                Attach(dtoToDelete);
                DbContext.Entry(dtoToDelete).State = EntityState.Deleted;

                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return dtoToDelete;
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(dtoToDelete);
            }
        }

        public virtual void Detach(TDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            Attach(dto);

            DbContext.Entry(dto).State = EntityState.Detached;
        }

        public virtual void Attach(TDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (DbContext.Entry(dto).State == EntityState.Detached)
                Set.Attach(dto);
        }

        public virtual TDto Add(TDto dtoToAdd)
        {
            if (dtoToAdd == null)
                throw new ArgumentNullException(nameof(dtoToAdd));

            try
            {
                DbContext.Add(dtoToAdd);

                SaveChanges();

                return dtoToAdd;
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(dtoToAdd);
            }
        }

        public virtual IEnumerable<TDto> AddRange(IEnumerable<TDto> dtosToAdd)
        {
            if (dtosToAdd == null)
                throw new ArgumentNullException(nameof(dtosToAdd));

            List<TDto> dtosToAddList = dtosToAdd as List<TDto> ?? dtosToAdd.ToList();

            try
            {
                DbContext.AddRange(dtosToAddList);

                SaveChanges();

                return dtosToAddList;
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    dtosToAddList.ForEach(Detach);
            }
        }

        public virtual TDto Update(TDto dtoToUpdate)
        {
            if (dtoToUpdate == null)
                throw new ArgumentNullException(nameof(dtoToUpdate));

            try
            {
                Attach(dtoToUpdate);
                DbContext.Entry(dtoToUpdate).State = EntityState.Modified;

                SaveChanges();

                return dtoToUpdate;
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(dtoToUpdate);
            }
        }

        public virtual TDto Delete(TDto dtoToDelete)
        {
            if (dtoToDelete == null)
                throw new ArgumentNullException(nameof(dtoToDelete));

            try
            {
                Attach(dtoToDelete);
                DbContext.Entry(dtoToDelete).State = EntityState.Deleted;

                SaveChanges();
                return dtoToDelete;
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(dtoToDelete);
            }
        }

        public virtual IQueryable<TDto> GetAll()
        {
            if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                return Set.AsNoTracking();
            else
                return Set;
        }

        public virtual Task<IQueryable<TDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                return Task.FromResult(Set.AsNoTracking());
            else
                return Task.FromResult((IQueryable<TDto>)Set);
        }

        public virtual async Task LoadCollectionAsync<TProperty>(TDto dto, Expression<Func<TDto, IEnumerable<TProperty>>> childs, CancellationToken cancellationToken)
            where TProperty : class
        {
            try
            {
                Attach(dto);

                CollectionEntry<TDto, TProperty> collection = DbContext.Entry(dto).Collection(childs);

                if (collection.IsLoaded == false)
                    await collection.LoadAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(dto);
            }
        }

        public virtual void LoadCollection<TProperty>(TDto dto, Expression<Func<TDto, IEnumerable<TProperty>>> childs)
            where TProperty : class
        {
            try
            {
                Attach(dto);

                CollectionEntry<TDto, TProperty> collection = DbContext.Entry(dto).Collection(childs);

                if (collection.IsLoaded == false)
                    collection.Load();
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(dto);
            }
        }

        public virtual async Task LoadReferenceAsync<TProperty>(TDto dto, Expression<Func<TDto, TProperty>> member, CancellationToken cancellationToken)
            where TProperty : class
        {
            try
            {
                Attach(dto);

                ReferenceEntry<TDto, TProperty> reference = DbContext.Entry(dto).Reference(member);

                if (reference.IsLoaded == false)
                    await reference.LoadAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(dto);
            }
        }

        public virtual void LoadReference<TProperty>(TDto dto, Expression<Func<TDto, TProperty>> member)
            where TProperty : class
        {
            try
            {
                Attach(dto);

                ReferenceEntry<TDto, TProperty> reference = DbContext.Entry(dto).Reference(member);

                if (reference.IsLoaded == false)
                    reference.Load();
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(dto);
            }
        }

        /// <summary>
        /// Unit of work is being handled by implicit unit of work implementation. SaveChanges is a non public method which is not present in Repository contract.
        /// </summary>
        protected virtual Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            DbContext.ChangeTracker.DetectChanges();
            return DbContext.SaveChangesAsync(cancellationToken);
        }

        protected virtual void SaveChanges()
        {
            DbContext.ChangeTracker.DetectChanges();
            DbContext.SaveChanges();
        }

        public virtual async Task ReloadAsync(TDto dto, CancellationToken cancellationToken)
        {
            try
            {
                Attach(dto);

                await DbContext.Entry(dto).ReloadAsync(cancellationToken);
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(dto);
            }
        }

        public virtual void Reload(TDto dto)
        {
            try
            {
                Attach(dto);

                DbContext.Entry(dto).Reload();
            }
            finally
            {
                if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(dto);
            }
        }

        public virtual IQueryable<TChild> GetCollectionQuery<TChild>(TDto dto, Expression<Func<TDto, IEnumerable<TChild>>> childs) where TChild : class
        {
            if (DbContext is EfCoreDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                throw new InvalidOperationException("This operation is valid for db context with change tracking enabled");

            Attach(dto);

            return DbContext.Entry(dto).Collection(childs).Query();
        }
    }

    public class EfCoreRepository<TDto> : EfCoreRepository<EfCoreDbContextBase, TDto>, IRepository<TDto>
        where TDto : class, IDto
    {

    }
}
