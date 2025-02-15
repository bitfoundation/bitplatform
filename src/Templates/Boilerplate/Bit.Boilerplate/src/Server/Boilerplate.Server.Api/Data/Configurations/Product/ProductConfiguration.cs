using Boilerplate.Server.Api.Models.Products;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public partial class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasIndex(p => p.Name).IsUnique();

        var defaultConcurrencyStamp = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        DateTimeOffset baseDate = DateTimeOffset.Parse("2022-07-12", styles: DateTimeStyles.AssumeUniversal);

        for (int i = 1; i <= 6; i++)
        {
            builder.HasData(
                new() { Id = Guid.Parse($"9a59dda2-7b12-4cc1-9658-d2586eef91d{i}"), Name = $"Mustang - {i}", Price = 27155, Description = "The Ford Mustang is ranked #1 in Sports Cars", CreatedOn = baseDate.AddDays(-10), CategoryId = Guid.Parse("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"a42914e2-92da-4f0b-aab0-b9572c9671b{i}"), Name = $"GT - {i}", Price = 500000, Description = "The Ford GT is a mid-engine two-seater sports car manufactured and marketed by American automobile manufacturer", CreatedOn = baseDate.AddDays(-15), CategoryId = Guid.Parse("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"f75325c8-a213-470b-ab93-4677ca4caee{i}"), Name = $"Ranger - {i}", Price = 25000, Description = "Ford Ranger is a nameplate that has been used on multiple model lines of pickup trucks sold by Ford worldwide.", CreatedOn = baseDate.AddDays(-25), CategoryId = Guid.Parse("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"43a82ec1-aab6-445f-83af-a85028417cf{i}"), Name = $"Raptor - {i}", Price = 53205, Description = "Raptor is a SCORE off-road trophy truck living in a asphalt world", CreatedOn = baseDate.AddDays(-30), CategoryId = Guid.Parse("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"f01b32bb-eccd-43be-aaf3-3c788a7d755{i}"), Name = $"Maverick - {i}", Price = 22470, Description = "The Ford Maverick is a compact pickup truck produced by Ford Motor Company.", CreatedOn = baseDate.AddDays(-35), CategoryId = Guid.Parse("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), ConcurrencyStamp = defaultConcurrencyStamp },
                                                                                           
                new() { Id = Guid.Parse($"d53bb159-f4f9-493a-b4dc-215fd765ca2{i}"), Name = $"Roadster - {i}", Price = 42800, Description = "A powerful convertible sports car", CreatedOn = baseDate.AddDays(-10), CategoryId = Guid.Parse("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"74bb268f-18cf-45ec-9f2f-30b34b18fb3{i}"), Name = $"Altima - {i}", Price = 24550, Description = "A perfectly adequate family sedan with sharp looks", CreatedOn = baseDate.AddDays(-15), CategoryId = Guid.Parse("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"eb787e1a-7ba8-4708-924b-9f7964fa0f6{i}"), Name = $"GT-R - {i}", Price = 113540, Description = "Legendary supercar with AWD, 4 seats, a powerful V6 engine and the latest tech", CreatedOn = baseDate.AddDays(-25), CategoryId = Guid.Parse("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"362a6638-0031-485d-825f-e8aeae63a33{i}"), Name = $"Juke - {i}", Price = 28100, Description = "A new smart SUV", CreatedOn = baseDate.AddDays(-35), CategoryId = Guid.Parse("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), ConcurrencyStamp = defaultConcurrencyStamp },
                                                                                           
                new() { Id = Guid.Parse($"8629931e-e26e-4885-b561-e447197d4b6{i}"), Name = $"H247 - {i}", Price = 54950, Description = "Subcompact luxury crossover SUV", CreatedOn = baseDate.AddDays(-10), CategoryId = Guid.Parse("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"a1c1987d-ee6c-41ad-9647-18de4504303{i}"), Name = $"V297 - {i}", Price = 103360, Description = "Battery-electric full-size luxury liftback", CreatedOn = baseDate.AddDays(-15), CategoryId = Guid.Parse("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"59eea437-bdf2-4c11-b262-06643b25328{i}"), Name = $"R50 - {i}", Price = 2000000, Description = "Ultra-rare and powerful sports car", CreatedOn = baseDate.AddDays(-35), CategoryId = Guid.Parse("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), ConcurrencyStamp = defaultConcurrencyStamp },
                                                                                           
                new() { Id = Guid.Parse($"01d223a3-182d-406a-9722-19dab083f96{i}"), Name = $"M550i - {i}", Price = 77790, Description = "A powerful, sporty variant of the BMW 5 Series", CreatedOn = baseDate.AddDays(-10), CategoryId = Guid.Parse("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"64a2616f-3af6-4248-86cf-4a605095a64{i}"), Name = $"540i - {i}", Price = 60945, Description = "Luxurious and powerful sedan that combines elegant design with impressive performance", CreatedOn = baseDate.AddDays(-15), CategoryId = Guid.Parse("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"ac50dc29-4b7e-4d4d-b23a-4227d91f2bb{i}"), Name = $"530e - {i}", Price = 56545, Description = "Combines class, spaciousness, and a well-built cabin", CreatedOn = baseDate.AddDays(-20), CategoryId = Guid.Parse("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"fb41cc51-9abd-4b45-b0d9-ea8f565ec50{i}"), Name = $"530i - {i}", Price = 55195, Description = "Zippy and fuel-efficient powertrain, and sure-footed handling", CreatedOn = baseDate.AddDays(-25), CategoryId = Guid.Parse("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"e159b1ad-12aa-4e02-a39b-d5e4a32eaf9{i}"), Name = $"M850i - {i}", Price = 100045, Description = "A Beastly coupe, powered by a fine-tuned 523-horsepower V8 engine", CreatedOn = baseDate.AddDays(-30), CategoryId = Guid.Parse("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"4d9cb0f4-1f32-45d5-8c84-d7f15bc569d{i}"), Name = $"X7 - {i}", Price = 77980, Description = "A full-size luxury crossover SUV that combines innovative design, an expansive presence, and a range of powerful engines", CreatedOn = baseDate.AddDays(-35), CategoryId = Guid.Parse("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"1b22319e-0a58-471e-82b6-75cd8b9d98e{i}"), Name = $"IX - {i}", Price = 87000, Description = "Luxury crossover SUV that combines cutting-edge technology", CreatedOn = baseDate.AddDays(-40), CategoryId = Guid.Parse("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), ConcurrencyStamp = defaultConcurrencyStamp },
                                                                                           
                new() { Id = Guid.Parse($"96c73b9c-03df-4f70-ac8d-75c32b89881{i}"), Name = $"Model 3 - {i}", Price = 61990, Description = "Rapid acceleration and dynamic handling", CreatedOn = baseDate.AddDays(-10), CategoryId = Guid.Parse("747f6d66-7524-40ca-8494-f65e85b5ee5d"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"840ba759-bde9-4821-b49b-c981c082bb9{i}"), Name = $"Model S - {i}", Price = 135000, Description = "Finishes near the top of our luxury electric car rankings.", CreatedOn = baseDate.AddDays(-15), CategoryId = Guid.Parse("747f6d66-7524-40ca-8494-f65e85b5ee5d"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"840e113b-5074-4b1c-86bd-e9affb65941{i}"), Name = $"Model X - {i}", Price = 138890, Description = "Heart-pumping acceleration, long drive range", CreatedOn = baseDate.AddDays(-20), CategoryId = Guid.Parse("747f6d66-7524-40ca-8494-f65e85b5ee5d"), ConcurrencyStamp = defaultConcurrencyStamp },
                new() { Id = Guid.Parse($"b2db9074-a0a9-4054-87e2-206b7a55c79{i}"), Name = $"Model Y - {i}", Price = 67790, Description = "Extensive driving range, lots of standard safety features", CreatedOn = baseDate.AddDays(-35), CategoryId = Guid.Parse("747f6d66-7524-40ca-8494-f65e85b5ee5d"), ConcurrencyStamp = defaultConcurrencyStamp }
            );
        }
    }
}

