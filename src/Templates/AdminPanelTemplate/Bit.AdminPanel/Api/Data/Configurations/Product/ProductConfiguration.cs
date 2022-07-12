using AdminPanel.Api.Models.Products;

namespace AdminPanel.Api.Data.Configurations.Account;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasData(
                new Product() { Id = 1, Name = "Mustang", Price = 27155, Description = "The Ford Mustang is ranked #1 in Sports Cars", CreatedOn = DateTime.Parse("2021-05-17"), CategoryId = 1 },
                new Product() { Id = 2, Name = "GT", Price = 500000, Description = "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", CreatedOn = DateTime.Parse("2021-05-07"), CategoryId = 1 },
                new Product() { Id = 3, Name = "Ranger", Price = 25000, Description = "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", CreatedOn = DateTime.Parse("2021-07-16"), CategoryId = 1 },
                new Product() { Id = 4, Name = "Raptor", Price = 53205, Description = "Raptor is a SCORE off-road trophy truck living in a asphalt world", CreatedOn = DateTime.Parse("2022-02-23"), CategoryId = 1 },
                new Product() { Id = 5, Name = "Maverick", Price = 22470, Description = "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", CreatedOn = DateTime.Parse("2021-05-27"), CategoryId = 1 },

                new Product() { Id = 6, Name = "Roadster", Price = 42800, Description = "A powerful convertible sports car", CreatedOn = DateTime.Parse("2022-01-27"), CategoryId = 2 },
                new Product() { Id = 7, Name = "Altima", Price = 24550, Description = "A perfectly adequate family sedan with sharp looks", CreatedOn = DateTime.Parse("2021-02-26"), CategoryId = 2 },
                new Product() { Id = 8, Name = "GT-R", Price = 113540, Description = "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", CreatedOn = DateTime.Parse("2022-03-24"), CategoryId = 2 },
                new Product() { Id = 9, Name = "Juke", Price = 28100, Description = "A new smart SUV", CreatedOn = DateTime.Parse("2022-02-10"), CategoryId = 2 },

                new Product() { Id = 10, Name = "H247", Price = 54950, Description = "", CreatedOn = DateTime.Parse("2021-07-22"), CategoryId = 3 },
                new Product() { Id = 11, Name = "V297", Price = 103360, Description = "", CreatedOn = DateTime.Parse("2022-04-18"), CategoryId = 3 },
                new Product() { Id = 12, Name = "R50", Price = 2000000, Description = "", CreatedOn = DateTime.Parse("2021-06-09"), CategoryId = 3 },

                new Product() { Id = 13, Name = "M550i", Price = 77790, Description = "", CreatedOn = DateTime.Parse("2022-03-14"), CategoryId = 4 },
                new Product() { Id = 14, Name = "540i", Price = 60945, Description = "", CreatedOn = DateTime.Parse("2022-02-01"), CategoryId = 4 },
                new Product() { Id = 15, Name = "530e", Price = 56545, Description = "", CreatedOn = DateTime.Parse("2021-05-11"), CategoryId = 4 },
                new Product() { Id = 16, Name = "530i", Price = 55195, Description = "", CreatedOn = DateTime.Parse("2022-02-02"), CategoryId = 4 },
                new Product() { Id = 17, Name = "M850i", Price = 100045, Description = "", CreatedOn = DateTime.Parse("2021-09-29"), CategoryId = 4 },
                new Product() { Id = 18, Name = "X7", Price = 77980, Description = "", CreatedOn = DateTime.Parse("2022-03-09"), CategoryId = 4 },
                new Product() { Id = 19, Name = "IX", Price = 87000, Description = "", CreatedOn = DateTime.Parse("2021-03-18"), CategoryId = 4 },

                new Product() { Id = 20, Name = "Model 3", Price = 61990, Description = "rapid acceleration and dynamic handling", CreatedOn = DateTime.Parse("2022-01-15"), CategoryId = 5 },
                new Product() { Id = 21, Name = "Model S", Price = 135000, Description = "finishes near the top of our luxury electric car rankings.", CreatedOn = DateTime.Parse("2021-05-23"), CategoryId = 5 },
                new Product() { Id = 22, Name = "Model X", Price = 138890, Description = "Heart-pumping acceleration, long drive range", CreatedOn = DateTime.Parse("2022-04-15"), CategoryId = 5 },
                new Product() { Id = 23, Name = "Model Y", Price = 67790, Description = "extensive driving range, lots of standard safety features", CreatedOn = DateTime.Parse("2021-03-13"), CategoryId = 5 }
            );
    }
}

