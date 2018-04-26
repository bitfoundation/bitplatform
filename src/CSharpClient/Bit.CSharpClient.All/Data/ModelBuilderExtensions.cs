using Bit.Model.Contracts;

namespace Microsoft.EntityFrameworkCore
{
    public static class ModelBuilderExtensions
    {
        public static void AddSyncableDto<TSyncableDto>(this ModelBuilder modelBuilder)
            where TSyncableDto : class, ISyncableDto
        {
            modelBuilder.Entity<TSyncableDto>()
                .Property<bool>("IsSynced");

            modelBuilder.Entity<TSyncableDto>()
                .HasQueryFilter(b => b.IsArchived == false);
        }
    }
}