using Boilerplate.Server.Api.Models.Categories;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasData(
            new() { Id = Guid.Parse("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), Name = "Ford", Color = "#FFCD56" },
            new() { Id = Guid.Parse("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), Name = "Nissan", Color = "#FF6384" },
            new() { Id = Guid.Parse("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), Name = "Benz", Color = "#4BC0C0" },
            new() { Id = Guid.Parse("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), Name = "BMW", Color = "#FF9124" },
            new() { Id = Guid.Parse("747f6d66-7524-40ca-8494-f65e85b5ee5d"), Name = "Tesla", Color = "#2B88D8" });
    }
}

