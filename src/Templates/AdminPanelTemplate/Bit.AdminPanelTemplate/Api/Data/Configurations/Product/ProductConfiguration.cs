using AdminPanelTemplate.Api.Models.Account;
using AdminPanelTemplate.Api.Models.Products;

namespace AdminPanelTemplate.Api.Data.Configurations.Account;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasData(
                new Product() { Id = 1, Name = "Mustang", Price = 27155, Desc = "The Ford Mustang is ranked #1 in Sports Cars", CreateDate = DateTime.Parse("2021-05-17"), CategoryId = 1 },
                new Product() { Id = 2, Name = "GT", Price = 500000, Desc = "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", CreateDate = DateTime.Parse("2021-05-07"), CategoryId = 1 },
                new Product() { Id = 3, Name = "Ranger", Price = 25000, Desc = "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", CreateDate = DateTime.Parse("2021-07-16"), CategoryId = 1 },
                new Product() { Id = 4, Name = "Raptor", Price = 53205, Desc = "Raptor is a SCORE off-road trophy truck living in a asphalt world", CreateDate = DateTime.Parse("2022-02-23"), CategoryId = 1 },
                new Product() { Id = 5, Name = "Maverick", Price = 22470, Desc = "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", CreateDate = DateTime.Parse("2021-05-27"), CategoryId = 1 },
               
                new Product() { Id = 6, Name = "Roadster", Price = 42800, Desc = "A powerful convertible sports car", CreateDate = DateTime.Parse("2022-01-27"), CategoryId = 2 },
                new Product() { Id = 7, Name = "Altima", Price = 24550, Desc = "A perfectly adequate family sedan with sharp looks", CreateDate = DateTime.Parse("2021-02-26"), CategoryId = 2 },
                new Product() { Id = 8, Name = "GT-R", Price = 113540, Desc = "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", CreateDate = DateTime.Parse("2022-03-24"), CategoryId = 2 },
                new Product() { Id = 9, Name = "Juke", Price = 28100, Desc = "A new smart SUV", CreateDate = DateTime.Parse("2022-02-10"), CategoryId = 2 },

                new Product() { Id = 10, Name = "H247", Price = 54950, Desc = "", CreateDate = DateTime.Parse("2021-07-22"), CategoryId = 3 },
                new Product() { Id = 11, Name = "V297", Price = 103360, Desc = "", CreateDate = DateTime.Parse("2022-04-18"), CategoryId = 3 },
                new Product() { Id = 12, Name = "R50", Price = 2000000, Desc = "", CreateDate = DateTime.Parse("2021-06-09"), CategoryId = 3 },

                new Product() { Id = 13, Name = "M550i", Price = 77790, Desc = "", CreateDate = DateTime.Parse("2022-03-14"), CategoryId = 4 },
                new Product() { Id = 14, Name = "540i", Price = 60945, Desc = "", CreateDate = DateTime.Parse("2022-02-01"), CategoryId = 4 },
                new Product() { Id = 15, Name = "530e", Price = 56545, Desc = "", CreateDate = DateTime.Parse("2021-05-11"), CategoryId = 4 },
                new Product() { Id = 16, Name = "530i", Price = 55195, Desc = "", CreateDate = DateTime.Parse("2022-02-02"), CategoryId = 4 },
                new Product() { Id = 17, Name = "M850i", Price = 100045, Desc = "", CreateDate = DateTime.Parse("2021-09-29"), CategoryId = 4 },
                new Product() { Id = 18, Name = "X7", Price = 77980, Desc = "", CreateDate = DateTime.Parse("2022-03-09"), CategoryId = 4 },
                new Product() { Id = 19, Name = "IX", Price = 87000, Desc = "", CreateDate = DateTime.Parse("2021-03-18"), CategoryId = 4 },


                new Product() { Id = 20, Name = "Model 3", Price = 61990, Desc = "rapid acceleration and dynamic handling", CreateDate = DateTime.Parse("2022-01-15"), CategoryId = 5 },
                new Product() { Id = 21, Name = "Model S", Price = 135000, Desc = "finishes near the top of our luxury electric car rankings.", CreateDate = DateTime.Parse("2021-05-23"), CategoryId = 5 },
                new Product() { Id = 22, Name = "Model X", Price = 138890, Desc = "Heart-pumping acceleration, long drive range", CreateDate = DateTime.Parse("2022-04-15"), CategoryId = 5 },
                new Product() { Id = 23, Name = "Model Y", Price = 67790, Desc = "extensive driving range, lots of standard safety features", CreateDate = DateTime.Parse("2021-03-13"), CategoryId = 5 }
            );
    }
}

