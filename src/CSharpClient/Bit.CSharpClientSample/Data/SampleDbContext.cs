using Bit.Data;
using Bit.Tests.Model.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace Bit.CSharpClientSample.Data
{
    public class SampleDbContext : EfCoreDbContextBase
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Sample.db");

            optionsBuilder.UseSqlite($"Filename={dbFileName}");

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddSyncableDto<TestCustomerDto>();
        }

        public virtual DbSet<TestCustomerDto> TestCustomers { get; set; }
    }
}
