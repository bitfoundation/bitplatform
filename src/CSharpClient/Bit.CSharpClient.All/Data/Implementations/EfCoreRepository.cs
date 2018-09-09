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
    public class EfCoreRepository<TDto> : IRepository<TDto>
           where TDto : class, IDto
    {
        private readonly EfCoreDbContextBase _dbContext;
        private readonly DbSet<TDto> _set;

        public EfCoreRepository(EfCoreDbContextBase dbContext)
        {
            _dbContext = dbContext;
            _set = _dbContext.Set<TDto>();
        }

        public virtual async Task<TDto> AddAsync(TDto dtoToAdd, CancellationToken cancellationToken = default)
        {
            if (dtoToAdd == null)
                throw new ArgumentNullException(nameof(dtoToAdd));

            try
            {
                await _dbContext.AddAsync(dtoToAdd, cancellationToken).ConfigureAwait(false);

                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return dtoToAdd;
            }
            finally
            {
                if (_dbContext.ChangeTrackingEnabled() == false)
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
                await _dbContext.AddRangeAsync(dtosToAddList, cancellationToken).ConfigureAwait(false);

                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return dtosToAddList;
            }
            finally
            {
                if (_dbContext.ChangeTrackingEnabled() == false)
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
                _dbContext.Entry(dtoToUpdate).State = EntityState.Modified;

                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return dtoToUpdate;
            }
            finally
            {
                if (_dbContext.ChangeTrackingEnabled() == false)
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
                _dbContext.Entry(dtoToDelete).State = EntityState.Deleted;

                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return dtoToDelete;
            }
            finally
            {
                if (_dbContext.ChangeTrackingEnabled() == false)
                    Detach(dtoToDelete);
            }
        }

        public virtual void Detach(TDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            Attach(dto);

            _dbContext.Entry(dto).State = EntityState.Detached;
        }

        public virtual void Attach(TDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (_dbContext.Entry(dto).State == EntityState.Detached)
                _set.Attach(dto);
        }

        public virtual TDto Add(TDto dtoToAdd)
        {
            if (dtoToAdd == null)
                throw new ArgumentNullException(nameof(dtoToAdd));

            try
            {
                _dbContext.Add(dtoToAdd);

                SaveChanges();

                return dtoToAdd;
            }
            finally
            {
                if (_dbContext.ChangeTrackingEnabled() == false)
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
                _dbContext.AddRange(dtosToAddList);

                SaveChanges();

                return dtosToAddList;
            }
            finally
            {
                if (_dbContext.ChangeTrackingEnabled() == false)
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
                _dbContext.Entry(dtoToUpdate).State = EntityState.Modified;

                SaveChanges();

                return dtoToUpdate;
            }
            finally
            {
                if (_dbContext.ChangeTrackingEnabled() == false)
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
                _dbContext.Entry(dtoToDelete).State = EntityState.Deleted;

                SaveChanges();
                return dtoToDelete;
            }
            finally
            {
                if (_dbContext.ChangeTrackingEnabled() == false)
                    Detach(dtoToDelete);
            }
        }

        public virtual IQueryable<TDto> GetAll()
        {
            if (_dbContext.ChangeTrackingEnabled() == false)
                return _set.AsNoTracking();
            else
                return _set;
        }

        public virtual Task<IQueryable<TDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            if (_dbContext.ChangeTrackingEnabled() == false)
                return Task.FromResult(_set.AsNoTracking());
            else
                return Task.FromResult((IQueryable<TDto>)_set);
        }

        public virtual async Task LoadCollectionAsync<TProperty>(TDto dto, Expression<Func<TDto, IEnumerable<TProperty>>> childs, CancellationToken cancellationToken)
            where TProperty : class
        {
            try
            {
                Attach(dto);

                CollectionEntry<TDto, TProperty> collection = _dbContext.Entry(dto).Collection(childs);

                if (collection.IsLoaded == false)
                    await collection.LoadAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (_dbContext.ChangeTrackingEnabled() == false)
                    Detach(dto);
            }
        }

        public virtual void LoadCollection<TProperty>(TDto dto, Expression<Func<TDto, IEnumerable<TProperty>>> childs)
            where TProperty : class
        {
            try
            {
                Attach(dto);

                CollectionEntry<TDto, TProperty> collection = _dbContext.Entry(dto).Collection(childs);

                if (collection.IsLoaded == false)
                    collection.Load();
            }
            finally
            {
                if (_dbContext.ChangeTrackingEnabled() == false)
                    Detach(dto);
            }
        }

        public virtual async Task LoadReferenceAsync<TProperty>(TDto dto, Expression<Func<TDto, TProperty>> member, CancellationToken cancellationToken)
            where TProperty : class
        {
            try
            {
                Attach(dto);

                ReferenceEntry<TDto, TProperty> reference = _dbContext.Entry(dto).Reference(member);

                if (reference.IsLoaded == false)
                    await reference.LoadAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (_dbContext.ChangeTrackingEnabled() == false)
                    Detach(dto);
            }
        }

        public virtual void LoadReference<TProperty>(TDto dto, Expression<Func<TDto, TProperty>> member)
            where TProperty : class
        {
            try
            {
                Attach(dto);

                ReferenceEntry<TDto, TProperty> reference = _dbContext.Entry(dto).Reference(member);

                if (reference.IsLoaded == false)
                    reference.Load();
            }
            finally
            {
                if (_dbContext.ChangeTrackingEnabled() == false)
                    Detach(dto);
            }
        }

        public virtual Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _dbContext.ChangeTracker.DetectChanges();
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        public virtual void SaveChanges()
        {
            _dbContext.ChangeTracker.DetectChanges();
            _dbContext.SaveChanges();
        }
    }
}
