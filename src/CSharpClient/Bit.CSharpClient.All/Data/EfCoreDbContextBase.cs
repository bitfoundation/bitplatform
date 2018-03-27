using Bit.Model.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Linq;
using System.Reflection;
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

        }

        public EfCoreDbContextBase()
            : base()
        {

        }

        public async Task UpsertDtoAsync<TDto>(TDto dto) // https://github.com/aspnet/EntityFrameworkCore/issues/9249
            where TDto : class, IDto
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (((IsSyncDbContext)this).IsSyncDbContext == false && dto is ISyncableDto syncableDto)
                Entry(syncableDto).Property("IsSynced").CurrentValue = false;

            TypeInfo dtoType = dto.GetType().GetTypeInfo();

            IEntityType efCoreDtoType = Model.FindEntityType(dtoType);

            IRelationalEntityTypeAnnotations dtoTypeRelationalInfo = efCoreDtoType.Relational();

            string tableName = $"[{dtoTypeRelationalInfo.TableName}]"; // No schema support

            var props = efCoreDtoType.GetProperties()
                .Select(p => new { p.Name, Value = Entry(dto).Property(p.Name).CurrentValue })
                .ToArray();

            string sql = $"INSERT OR REPLACE INTO {tableName} ({string.Join(",", props.Select(p => $"[{p.Name}]"))}) VALUES({string.Join(",", props.Select((p, i) => $"{{{i}}}"))})";

            await Database.ExecuteSqlCommandAsync(sql, props.Select(p => p.Value)).ConfigureAwait(false);
        }
    }
}
