using Bit.Model.Contracts;
using System;

namespace Microsoft.EntityFrameworkCore
{
    public static class ModelBuilderExtensions
    {
        public static void AddSyncableDto<TSyncableDto>(this ModelBuilder modelBuilder)
            where TSyncableDto : class, ISyncableDto
        {
            if (modelBuilder == null)
                throw new ArgumentNullException(nameof(modelBuilder));

            modelBuilder.Entity<TSyncableDto>()
                .Property<bool>("IsSynced");

            modelBuilder.Entity<TSyncableDto>()
                .HasQueryFilter(b => b.IsArchived == false);
        }
    }
}
