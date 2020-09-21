using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Tests.Model.Dto;
using Microsoft.EntityFrameworkCore;
using Prism.Services;
using System.IO;
using Xamarin.Essentials;

namespace Bit.CSharpClientSample.Data
{
    public class SampleDbContext : EfCoreDbContextBase
    {
        public SampleDbContext(DbContextOptions options, IDeviceService deviceService)
            : base(options)
        {
            DeviceService = deviceService;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbFileName = "Sample.db";

            dbFileName = Path.Combine(FileSystem.AppDataDirectory, dbFileName);

            optionsBuilder.UseSqlite($"Filename={dbFileName}");

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddSyncableDto<TestCustomerDto>();

            base.OnModelCreating(modelBuilder);
        }

        public virtual DbSet<TestCustomerDto> TestCustomers { get; set; }

        public virtual IDeviceService DeviceService { get; }
    }
}
