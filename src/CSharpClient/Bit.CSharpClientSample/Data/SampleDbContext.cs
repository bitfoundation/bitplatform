using Bit.Data;
using Bit.Tests.Model.Dto;
using Microsoft.EntityFrameworkCore;
using Prism.AppModel;
using Prism.Services;
using System;
using System.IO;

namespace Bit.CSharpClientSample.Data
{
    public class SampleDbContext : EfCoreDbContextBase
    {
        private readonly IDeviceService _deviceService;

        public SampleDbContext(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbFileName = "Sample.db";

            if (_deviceService.RuntimePlatform != RuntimePlatform.UWP)
                dbFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dbFileName);

            optionsBuilder.UseSqlite($"Filename={dbFileName}");

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddSyncableDto<TestCustomerDto>();

            base.OnModelCreating(modelBuilder);
        }

        public virtual DbSet<TestCustomerDto> TestCustomers { get; set; }
    }
}
