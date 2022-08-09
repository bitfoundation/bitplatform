using AdminPanel.Api.Models.Categories;

namespace AdminPanel.Api.Data.Configurations.Account;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasData(
            new Category() { Id = 1, Name = "Ford", Color = "#FFCD56" },
            new Category() { Id = 2, Name = "Nissan", Color = "#FF6384" },
            new Category() { Id = 3, Name = "Benz", Color = "#4BC0C0" },
            new Category() { Id = 4, Name = "BMW", Color = "#FF9124" },
            new Category() { Id = 5, Name = "Tesla", Color = "#2B88D8" });
    }
}

