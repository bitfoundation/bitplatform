using AdminPanel.Api.Models.Categories;

namespace AdminPanel.Api.Data.Configurations.Account;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasData(
            new Category() { Id = 1, Name = "Ford", Color = "#ff0000" },
            new Category() { Id = 2, Name = "Nissan", Color = "#0300ff" },
            new Category() { Id = 3, Name = "Benz", Color = "#00f800" },
            new Category() { Id = 4, Name = "BMW", Color = "#fefe00" },
            new Category() { Id = 5, Name = "Tesla", Color = "#fe04fe" });
    }
}

