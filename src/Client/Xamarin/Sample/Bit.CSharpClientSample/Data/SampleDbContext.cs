using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Tests.Model.Dto;
using Microsoft.EntityFrameworkCore;
using Prism.Services;
using System.IO;

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

#if ANDROID || IOS
            dbFileName = Path.Combine(Microsoft.Maui.Essentials.FileSystem.AppDataDirectory, dbFileName);
#else
            dbFileName = Path.Combine(Xamarin.Essentials.FileSystem.AppDataDirectory, dbFileName);
#endif

            optionsBuilder.UseSqlite($"Filename={dbFileName}");

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddSyncableDto<TestCustomerDto>();

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<TestCustomerDto> TestCustomers { get; set; }

        public IDeviceService DeviceService { get; }
    }
}
