using Boilerplate.Server.Api.Models.Categories;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasData(
            new() { Id = 1, Name = "Ford", Color = "#FFCD56" },
            new() { Id = 2, Name = "Nissan", Color = "#FF6384" },
            new() { Id = 3, Name = "Benz", Color = "#4BC0C0" },
            new() { Id = 4, Name = "BMW", Color = "#FF9124" },
            new() { Id = 5, Name = "Tesla", Color = "#2B88D8" });
    }
}

