//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Products;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public partial class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasIndex(p => p.Name).IsUnique();
        builder.HasIndex(p => p.ShortId).IsUnique();

        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (database == "PostgreSQL" || database == "SqlServer")
        builder.Property(p => p.ShortId).UseSequence("ProductShortId");
        //#endif
        //#if (database == "PostgreSQL" || database == "SqlServer")
        if (AppDbContext.IsEmbeddingEnabled)
        {
            builder.Property(p => p.Embedding).HasColumnType("vector(384)"); // Dimensions depends on the model used, here assuming 384 because of LocalTextEmbeddingGenerationService.
        }
        else
        {
            builder.Ignore(p => p.Embedding);
        }
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif

        //#if (IsInsideProjectTemplate == true)
        builder.Ignore(p => p.Embedding);
        //#endif

        var defaultConcurrencyStamp = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        DateTimeOffset baseDate = DateTimeOffset.Parse("2022-07-12", styles: DateTimeStyles.AssumeUniversal);

        // --- Benz Entries (19 cars) ---
        // https://www.mercedes-benz.ca/en/all-vehicles

        var benzId = Guid.Parse("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08");
        builder.HasData(new Product
        {
            Id = Guid.Parse($"9a59dda2-7b12-4cc1-9658-d2586eef91d7"),
            Name = "EQB SUV",
            Price = 54_200M,
            DescriptionHTML = "<h3>Range to roam. Room for up to 7.</h3><p>It's a versatile SUV with room for up to seven. And an advanced EV you can enjoy every day. Intelligent technology and thoughtful luxury are delivered with swift response and silenced refinement.</p>",
            DescriptionText = "Range to roam. Room for up to 7.\nIt's a versatile SUV with room for up to seven. And an advanced EV you can enjoy every day. Intelligent technology and thoughtful luxury are delivered with swift response and silenced refinement.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10000,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"5746ae3d-5116-4774-9d55-0ff496e5186f"),
            Name = "EQE Sedan",
            Price = 76_050M,
            DescriptionHTML = "<h3>Electric, essential, quintessential</h3><p>It's futuristic, forward and fresh, but you know its core values. Ever-refined luxury. Ever-advancing innovation. And a never-ending devotion to your well-being. Perhaps no electric sedan feels so new, yet so natural.</p>",
            DescriptionText = "Electric, essential, quintessential\nIt's futuristic, forward and fresh, but you know its core values. Ever-refined luxury. Ever-advancing innovation. And a never-ending devotion to your well-being. Perhaps no electric sedan feels so new, yet so natural.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10001,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"512eb70b-1d39-4845-88c0-fe19cd2d1979"),
            Name = "EQE SUV",
            Price = 79_050M,
            DescriptionHTML = "<h3>Advanced, adaptive, adventurous.</h3><p>More than new, it can update its advancements down the road. More than thoughtful, it can anticipate needs and desires. Beyond futuristic, it can make better use of your time, and make your time using it better.</p>",
            DescriptionText = "Advanced, adaptive, adventurous.\nMore than new, it can update its advancements down the road. More than thoughtful, it can anticipate needs and desires. Beyond futuristic, it can make better use of your time, and make your time using it better.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10003,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"f4a3b2c1-d0e9-f8a7-b6c5-d4e3f2a1b0c9"),
            Name = "EQS SUV",
            Price = 136_000M,
            DescriptionHTML = "<h3>The pinnacle of electric luxury SUVs.</h3><p>Experience flagship comfort and groundbreaking technology in an all-electric SUV form, redefining sustainable luxury with space for up to seven.</p>",
            DescriptionText = "The pinnacle of electric luxury SUVs.\nExperience flagship comfort and groundbreaking technology in an all-electric SUV form, redefining sustainable luxury with space for up to seven.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10004,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"e3b2a1d0-c9f8-e7b6-a5d4-c3b2a1d0e9f8"),
            Name = "EQS Sedan",
            Price = 136_000M,
            DescriptionHTML = "<h3>The future of luxury, electrified.</h3><p>The flagship electric sedan fuses progressive design with pioneering technology and breathtaking performance, setting a new standard for electric mobility.</p>",
            DescriptionText = "The future of luxury, electrified.\nThe flagship electric sedan fuses progressive design with pioneering technology and breathtaking performance, setting a new standard for electric mobility.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10005,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"d2a1b0c9-f8e7-b6a5-d4c3-b2a1d0e9f8e7"),
            Name = "GLA SUV",
            Price = 49_900M,
            DescriptionHTML = "<h3>Compact dimensions, grand aspirations.</h3><p>Agile and adventurous, the GLA combines SUV versatility with compact efficiency, perfect for navigating city streets or exploring scenic routes.</p>",
            DescriptionText = "Compact dimensions, grand aspirations.\nAgile and adventurous, the GLA combines SUV versatility with compact efficiency, perfect for navigating city streets or exploring scenic routes.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10006,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"c1b0a9f8-e7b6-a5d4-c3b2-a1d0e9f8e7b6"),
            Name = "GLB SUV",
            Price = 52_500M,
            DescriptionHTML = "<h3>Versatility meets space.</h3><p>Surprisingly spacious for its size, the GLB offers an optional third row, making it the flexible and family-friendly compact SUV for all your adventures.</p>",
            DescriptionText = "Versatility meets space.\nSurprisingly spacious for its size, the GLB offers an optional third row, making it the flexible and family-friendly compact SUV for all your adventures.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10007,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"b0a9f8e7-b6a5-d4c3-b2a1-d0e9f8e7b6a5"),
            Name = "GLC SUV",
            Price = 58_900M,
            DescriptionHTML = "<h3>Intelligent drive, impressive design.</h3><p>The GLC SUV sets benchmarks for comfort, technology, and performance in the mid-size luxury segment, adapting effortlessly to your driving needs.</p>",
            DescriptionText = "Intelligent drive, impressive design.\nThe GLC SUV sets benchmarks for comfort, technology, and performance in the mid-size luxury segment, adapting effortlessly to your driving needs.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10008,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"a9f8e7b6-a5d4-c3b2-a1d0-e9f8e7b6a5d4"),
            Name = "GLC Coupe",
            Price = 63_500M,
            DescriptionHTML = "<h3>Sporty style, SUV substance.</h3><p>Combining the dynamic presence of a coupe with the versatility of an SUV, the GLC Coupe makes a powerful statement on any road.</p>",
            DescriptionText = "Sporty style, SUV substance.\nCombining the dynamic presence of a coupe with the versatility of an SUV, the GLC Coupe makes a powerful statement on any road.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10009,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"98e7b6a5-d4c3-b2a1-d0e9-f8e7b6a5d4c3"),
            Name = "GLE SUV",
            Price = 82_800M,
            DescriptionHTML = "<h3>Master of every ground.</h3><p>Intelligent, spacious, and capable, the GLE SUV offers cutting-edge technology and luxurious comfort for families and adventurers alike.</p>",
            DescriptionText = "Master of every ground.\nIntelligent, spacious, and capable, the GLE SUV offers cutting-edge technology and luxurious comfort for families and adventurers alike.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10010,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"87b6a5d4-c3b2-a1d0-e9f8-e7b6a5d4c3b2"),
            Name = "GLE Coupe",
            Price = 94_900M,
            DescriptionHTML = "<h3>Athletic presence, commanding performance.</h3><p>The GLE Coupe blends the muscular stance of an SUV with the elegant lines of a coupe, delivering exhilarating performance and undeniable style.</p>",
            DescriptionText = "Athletic presence, commanding performance.\nThe GLE Coupe blends the muscular stance of an SUV with the elegant lines of a coupe, delivering exhilarating performance and undeniable style.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10011,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"76a5d4c3-b2a1-d0e9-f8e7-b6a5d4c3b2a1"),
            Name = "GLS SUV",
            Price = 115_100M,
            DescriptionHTML = "<h3>The S-Class of SUVs.</h3><p>Offering first-class travel for up to seven passengers, the GLS combines commanding presence with unparalleled luxury, space, and technology.</p>",
            DescriptionText = "The S-Class of SUVs.\nOffering first-class travel for up to seven passengers, the GLS combines commanding presence with unparalleled luxury, space, and technology.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10012,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"65d4c3b2-a1d0-e9f8-e7b6-a5d4c3b2a1d0"),
            Name = "G-CLASS SUV",
            Price = 180_000M,
            DescriptionHTML = "<h3>An icon reengineered.</h3><p>Instantly recognizable, eternally capable. The G-Class remains the definitive luxury off-roader, blending timeless design with modern technology and rugged performance.</p>",
            DescriptionText = "An icon reengineered.\nInstantly recognizable, eternally capable. The G-Class remains the definitive luxury off-roader, blending timeless design with modern technology and rugged performance.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10013,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"54c3b2a1-d0e9-f8e7-b6a5-d4c3b2a1d0e9"),
            Name = "E-Class Sedan",
            Price = 73_900M,
            DescriptionHTML = "<h3>The heart of the brand, intelligently refined.</h3><p>A masterpiece of intelligence, the E-Class Sedan seamlessly blends dynamic design, luxurious comfort, and groundbreaking driver assistance systems.</p>",
            DescriptionText = "The heart of the brand, intelligently refined.\nA masterpiece of intelligence, the E-Class Sedan seamlessly blends dynamic design, luxurious comfort, and groundbreaking driver assistance systems.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10014,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"43b2a1d0-e9f8-e7b6-a5d4-c3b2a1d0e9f8"),
            Name = "S-Class Sedan",
            Price = 140_000M,
            DescriptionHTML = "<h3>The pinnacle of automotive desire.</h3><p>Engineered without compromise, the S-Class Sedan pioneers innovations in safety, comfort, and driving experience, defining luxury travel.</p>",
            DescriptionText = "The pinnacle of automotive desire.\nEngineered without compromise, the S-Class Sedan pioneers innovations in safety, comfort, and driving experience, defining luxury travel.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10015,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"32a1d0e9-f8e7-b6a5-d4c3-b2a1d0e9f8e7"),
            Name = "CLA COUPE",
            Price = 50_500M,
            DescriptionHTML = "<h3>Irresistible from every angle.</h3><p>With its iconic sloping roofline and sporty stance, the CLA Coupe captivates with expressive design and agile performance.</p>",
            DescriptionText = "Irresistible from every angle.\nWith its iconic sloping roofline and sporty stance, the CLA Coupe captivates with expressive design and agile performance.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10016,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"21d0e9f8-e7b6-a5d4-c3b2-a1d0e9f8e7b6"),
            Name = "CLE Coupe",
            Price = 65_000M,
            DescriptionHTML = "<h3>Sporty elegance, redefined.</h3><p>The all-new CLE Coupe merges expressive design with dynamic handling and advanced technology, creating a modern icon of desire.</p>",
            DescriptionText = "Sporty elegance, redefined.\nThe all-new CLE Coupe merges expressive design with dynamic handling and advanced technology, creating a modern icon of desire.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10017,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"10e9f8e7-b6a5-d4c3-b2a1-d0e9f8e7b6a5"),
            Name = "Mercedes-AMG GT Coupe",
            Price = 150_000M,
            DescriptionHTML = "<h3>Driving performance, elevated.</h3><p>Born on the racetrack, the AMG GT Coupe embodies the essence of a pure sports car with breathtaking power and precision handling.</p>",
            DescriptionText = "Driving performance, elevated.\nBorn on the racetrack, the AMG GT Coupe embodies the essence of a pure sports car with breathtaking power and precision handling.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10018,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse($"f9f8e7b6-a5d4-c3b2-a1d0-e9f8e7b6a5d4"),
            Name = "CLE Cabriolet",
            Price = 75_000M,
            DescriptionHTML = "<h3>Open-air freedom, elegant design.</h3><p>Experience the joy of driving with the top down in the CLE Cabriolet, offering sophisticated style, year-round comfort, and exhilarating performance.</p>",
            DescriptionText = "Open-air freedom, elegant design.\nExperience the joy of driving with the top down in the CLE Cabriolet, offering sophisticated style, year-round comfort, and exhilarating performance.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = benzId,
            ShortId = 10019,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        // --- Ford Entries (8 cars) ---

        var fordId = Guid.Parse("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845");
        builder.HasData(new Product
        {
            Id = Guid.Parse("a1b2c3d4-e5f6-7890-1234-567890abcdef"),
            Name = "Ford F-150",
            Price = 45_000M,
            DescriptionHTML = "<h3>Built Ford Tough.</h3><p>America's best-selling truck, known for its capability, innovation, and toughness for work or play.</p>",
            DescriptionText = "Built Ford Tough.\nAmerica's best-selling truck, known for its capability, innovation, and toughness for work or play.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = fordId,
            ShortId = 10020,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("b2c3d4e5-f6a7-89b0-12c3-45d678e9f0a1"),
            Name = "Ford Mustang",
            Price = 40_000M,
            DescriptionHTML = "<h3>Iconic American Muscle.</h3><p>Thrilling performance and unmistakable style define the legendary Ford Mustang coupe.</p>",
            DescriptionText = "Iconic American Muscle.\nThrilling performance and unmistakable style define the legendary Ford Mustang coupe.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = fordId,
            ShortId = 10021,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("c3d4e5f6-a7b8-9c01-23d4-56e789f0a1b2"),
            Name = "Ford Explorer",
            Price = 48_000M,
            DescriptionHTML = "<h3>Adventure Ready SUV.</h3><p>A spacious and capable SUV designed for family adventures, offering three rows of seating and modern tech.</p>",
            DescriptionText = "Adventure Ready SUV.\nA spacious and capable SUV designed for family adventures, offering three rows of seating and modern tech.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = fordId,
            ShortId = 10022,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("d4e5f6a7-b8c9-d0e1-f234-567890a1b2c3"),
            Name = "Ford Escape",
            Price = 35_000M,
            DescriptionHTML = "<h3>Compact Versatility.</h3><p>A stylish and efficient compact SUV offering flexibility for city driving and weekend getaways.</p>",
            DescriptionText = "Compact Versatility.\nA stylish and efficient compact SUV offering flexibility for city driving and weekend getaways.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = fordId,
            ShortId = 10023,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("e5f6a7b8-c9d0-e1f2-3456-7890a1b2c3d4"),
            Name = "Ford Bronco",
            Price = 42_000M,
            DescriptionHTML = "<h3>Built Wild.</h3><p>Return of an icon. The Ford Bronco is engineered for rugged off-road capability and adventure.</p>",
            DescriptionText = "Built Wild.\nReturn of an icon. The Ford Bronco is engineered for rugged off-road capability and adventure.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = fordId,
            ShortId = 10024,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("f6a7b8c9-d0e1-f2a3-4567-8901b2c3d4e5"),
            Name = "Ford Mustang Mach-E",
            Price = 55_000M,
            DescriptionHTML = "<h3>Electric Thrills.</h3><p>An all-electric SUV bearing the Mustang name, delivering exhilarating performance and advanced technology.</p>",
            DescriptionText = "Electric Thrills.\nAn all-electric SUV bearing the Mustang name, delivering exhilarating performance and advanced technology.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = fordId,
            ShortId = 10025,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("a7b8c9d0-e1f2-a3b4-5678-9012c3d4e5f6"),
            Name = "Ford Maverick",
            Price = 28_000M,
            DescriptionHTML = "<h3>Compact Truck, Big Ideas.</h3><p>An affordable and versatile compact pickup, available with a standard hybrid powertrain.</p>",
            DescriptionText = "Compact Truck, Big Ideas.\nAn affordable and versatile compact pickup, available with a standard hybrid powertrain.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = fordId,
            ShortId = 10026,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("b8c9d0e1-f2a3-b4c5-6789-0123d4e5f6a7"),
            Name = "Ford Edge",
            Price = 40_000M,
            DescriptionHTML = "<h3>Stylish Mid-Size Crossover.</h3><p>Combines sophisticated design with smart technology and engaging performance in a two-row crossover.</p>",
            DescriptionText = "Stylish Mid-Size Crossover.\nCombines sophisticated design with smart technology and engaging performance in a two-row crossover.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = fordId,
            ShortId = 10027,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });


        // --- Nissan Entries (8 cars) ---

        var nissanId = Guid.Parse("582b8c19-0709-4dae-b7a6-fa0e704dad3c");
        builder.HasData(new Product
        {
            Id = Guid.Parse("c9d0e1f2-a3b4-c5d6-7890-1234e5f6a7b8"),
            Name = "Nissan Rogue",
            Price = 32_000M,
            DescriptionHTML = "<h3>Family-Friendly Crossover.</h3><p>Nissan's popular compact SUV, offering advanced safety features and a comfortable, versatile interior.</p>",
            DescriptionText = "Family-Friendly Crossover.\nNissan's popular compact SUV, offering advanced safety features and a comfortable, versatile interior.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = nissanId,
            ShortId = 10028,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("d0e1f2a3-b4c5-d6e7-8901-2345f6a7b8c9"),
            Name = "Nissan Altima",
            Price = 30_000M,
            DescriptionHTML = "<h3>Intelligent Midsize Sedan.</h3><p>A stylish sedan featuring available All-Wheel Drive and driver-assist technologies.</p>",
            DescriptionText = "Intelligent Midsize Sedan.\nA stylish sedan featuring available All-Wheel Drive and driver-assist technologies.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = nissanId,
            ShortId = 10029,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("e1f2a3b4-c5d6-e7f8-9012-3456a7b8c9d0"),
            Name = "Nissan Sentra",
            Price = 25_000M,
            DescriptionHTML = "<h3>Sharp Compact Sedan.</h3><p>Offers unexpected style, standard safety features, and premium feel in the compact sedan class.</p>",
            DescriptionText = "Sharp Compact Sedan.\nOffers unexpected style, standard safety features, and premium feel in the compact sedan class.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = nissanId,
            ShortId = 10030,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("f2a3b4c5-d6e7-f8a9-0123-4567b8c9d0e1"),
            Name = "Nissan Pathfinder",
            Price = 40_000M,
            DescriptionHTML = "<h3>Rugged 3-Row SUV.</h3><p>Return to rugged. The Pathfinder offers seating for up to eight and capable performance for family adventures.</p>",
            DescriptionText = "Rugged 3-Row SUV.\nReturn to rugged. The Pathfinder offers seating for up to eight and capable performance for family adventures.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = nissanId,
            ShortId = 10031,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("a3b4c5d6-e7f8-a9b0-1234-5678c9d0e1f2"),
            Name = "Nissan Frontier",
            Price = 35_000M,
            DescriptionHTML = "<h3>Right-Sized Pickup.</h3><p>A capable and durable mid-size truck designed for both work duties and weekend adventures.</p>",
            DescriptionText = "Right-Sized Pickup.\nA capable and durable mid-size truck designed for both work duties and weekend adventures.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = nissanId,
            ShortId = 10032,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("b4c5d6e7-f8a9-b0c1-2345-6789d0e1f2a3"),
            Name = "Nissan Kicks",
            Price = 24_000M,
            DescriptionHTML = "<h3>Expressive Subcompact Crossover.</h3><p>Stand out with customizable style and enjoy city-friendly agility and smart technology.</p>",
            DescriptionText = "Expressive Subcompact Crossover.\nStand out with customizable style and enjoy city-friendly agility and smart technology.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = nissanId,
            ShortId = 10033,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("c5d6e7f8-a9b0-c1d2-3456-7890e1f2a3b4"),
            Name = "Nissan Ariya",
            Price = 50_000M,
            DescriptionHTML = "<h3>Nissan's Electric Crossover.</h3><p>Experience the future of driving with the all-electric Ariya, blending sleek design with advanced EV technology.</p>",
            DescriptionText = "Nissan's Electric Crossover.\nExperience the future of driving with the all-electric Ariya, blending sleek design with advanced EV technology.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = nissanId,
            ShortId = 10034,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("d6e7f8a9-b0c1-d2e3-4567-8901f2a3b4c5"),
            Name = "Nissan Z",
            Price = 45_000M,
            DescriptionHTML = "<h3>Legendary Performance.</h3><p>The iconic sports car returns, pairing timeless design with thrilling twin-turbo V6 power.</p>",
            DescriptionText = "Legendary Performance.\nThe iconic sports car returns, pairing timeless design with thrilling twin-turbo V6 power.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = nissanId,
            ShortId = 10035,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });


        // --- BMW Entries (8 cars) ---
        var bmwId = Guid.Parse("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4");
        builder.HasData(new Product
        {
            Id = Guid.Parse("e7f8a9b0-c1d2-e3f4-5678-9012a3b4c5d6"),
            Name = "BMW 3 Series Sedan",
            Price = 50_000M,
            DescriptionHTML = "<h3>The Ultimate Driving Machine.</h3><p>The quintessential sports sedan, balancing dynamic performance with everyday usability and luxury.</p>",
            DescriptionText = "The Ultimate Driving Machine.\nThe quintessential sports sedan, balancing dynamic performance with everyday usability and luxury.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = bmwId,
            ShortId = 10036,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("f8a9b0c1-d2e3-f4a5-6789-0123b4c5d6e7"),
            Name = "BMW X3 SAV",
            Price = 55_000M,
            DescriptionHTML = "<h3>Versatile Sport Activity Vehicle.</h3><p>Combines dynamic BMW driving characteristics with the versatility and utility of an SAV.</p>",
            DescriptionText = "Versatile Sport Activity Vehicle.\nCombines dynamic BMW driving characteristics with the versatility and utility of an SAV.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = bmwId,
            ShortId = 10037,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("a9b0c1d2-e3f4-a5b6-7890-1234c5d6e7f8"),
            Name = "BMW X5 SAV",
            Price = 70_000M,
            DescriptionHTML = "<h3>The Boss.</h3><p>The original Sports Activity Vehicle, setting benchmarks for luxury, performance, and capability in its class.</p>",
            DescriptionText = "The Boss.\nThe original Sports Activity Vehicle, setting benchmarks for luxury, performance, and capability in its class.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = bmwId,
            ShortId = 10038,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("b0c1d2e3-f4a5-b6c7-8901-2345d6e7f8a9"),
            Name = "BMW 5 Series Sedan",
            Price = 65_000M,
            DescriptionHTML = "<h3>Executive Athleticism.</h3><p>A sophisticated blend of dynamic performance, cutting-edge technology, and luxurious comfort for the executive class.</p>",
            DescriptionText = "Executive Athleticism.\nA sophisticated blend of dynamic performance, cutting-edge technology, and luxurious comfort for the executive class.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = bmwId,
            ShortId = 10039,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("c1d2e3f4-a5b6-c7d8-9012-3456e7f8a9b0"),
            Name = "BMW i4 Gran Coupe",
            Price = 60_000M,
            DescriptionHTML = "<h3>Electric Performance Coupe.</h3><p>BMW's first all-electric Gran Coupe, delivering impressive range and signature BMW driving dynamics.</p>",
            DescriptionText = "Electric Performance Coupe.\nBMW's first all-electric Gran Coupe, delivering impressive range and signature BMW driving dynamics.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = bmwId,
            ShortId = 10040,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("d2e3f4a5-b6c7-d8e9-0123-4567f8a9b0c1"),
            Name = "BMW iX SAV",
            Price = 90_000M,
            DescriptionHTML = "<h3>Electric Technology Flagship.</h3><p>A bold vision of the future SAV, featuring sustainable luxury, groundbreaking tech, and exhilarating electric power.</p>",
            DescriptionText = "Electric Technology Flagship.\nA bold vision of the future SAV, featuring sustainable luxury, groundbreaking tech, and exhilarating electric power.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = bmwId,
            ShortId = 10041,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("e3f4a5b6-c7d8-e9f0-1234-5678a9b0c1d2"),
            Name = "BMW M3 Sedan",
            Price = 80_000M,
            DescriptionHTML = "<h3>High-Performance Icon.</h3><p>The legendary M3 delivers uncompromising track-ready performance combined with everyday usability.</p>",
            DescriptionText = "High-Performance Icon.\nThe legendary M3 delivers uncompromising track-ready performance combined with everyday usability.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = bmwId,
            ShortId = 10042,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("f4a5b6c7-d8e9-f0a1-2345-6789b0c1d2e3"),
            Name = "BMW Z4 Roadster",
            Price = 60_000M,
            DescriptionHTML = "<h3>Open-Top Freedom.</h3><p>A classic roadster experience with modern BMW performance, agility, and style.</p>",
            DescriptionText = "Open-Top Freedom.\nA classic roadster experience with modern BMW performance, agility, and style.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = bmwId,
            ShortId = 10043,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });


        // --- Tesla Entries (7 cars) ---
        var teslaId = Guid.Parse("747f6d66-7524-40ca-8494-f65e85b5ee5d");
        builder.HasData(new Product
        {
            Id = Guid.Parse("a5b6c7d8-e9f0-a1b2-3456-7890c1d2e3f4"),
            Name = "Tesla Model 3",
            Price = 45_000M,
            DescriptionHTML = "<h3>Mass-Market Electric Sedan.</h3><p>Tesla's most affordable car, offering impressive range, performance, and minimalist design.</p>",
            DescriptionText = "Mass-Market Electric Sedan.\nTesla's most affordable car, offering impressive range, performance, and minimalist design.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = teslaId,
            ShortId = 10044,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("b6c7d8e9-f0a1-b2c3-4567-8901d2e3f4a5"),
            Name = "Tesla Model Y",
            Price = 55_000M,
            DescriptionHTML = "<h3>Compact Electric SUV.</h3><p>A versatile electric SUV offering ample space, performance, and access to Tesla's Supercharger network.</p>",
            DescriptionText = "Compact Electric SUV.\nA versatile electric SUV offering ample space, performance, and access to Tesla's Supercharger network.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = teslaId,
            ShortId = 10045,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("c7d8e9f0-a1b2-c3d4-5678-9012e3f4a5b6"),
            Name = "Tesla Model S",
            Price = 90_000M,
            DescriptionHTML = "<h3>Luxury Electric Benchmark.</h3><p>The sedan that redefined electric performance, offering incredible acceleration, range, and technology.</p>",
            DescriptionText = "Luxury Electric Benchmark.\nThe sedan that redefined electric performance, offering incredible acceleration, range, and technology.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = teslaId,
            ShortId = 10046,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("d8e9f0a1-b2c3-d4e5-6789-0123f4a5b6c7"),
            Name = "Tesla Model X",
            Price = 100_000M,
            DescriptionHTML = "<h3>Electric SUV with Falcon Wings.</h3><p>A unique family SUV featuring distinctive Falcon Wing doors, impressive range, and performance.</p>",
            DescriptionText = "Electric SUV with Falcon Wings.\nA unique family SUV featuring distinctive Falcon Wing doors, impressive range, and performance.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = teslaId,
            ShortId = 10047,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("e9f0a1b2-c3d4-e5f6-7890-1234a5b6c7d8"),
            Name = "Tesla Model 3 Performance",
            Price = 55_000M,
            DescriptionHTML = "<h3>Enhanced Electric Sport Sedan.</h3><p>Takes the Model 3 foundation and adds quicker acceleration, track mode, and sportier tuning.</p>",
            DescriptionText = "Enhanced Electric Sport Sedan.\nTakes the Model 3 foundation and adds quicker acceleration, track mode, and sportier tuning.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = teslaId,
            ShortId = 10048,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("f0a1b2c3-d4e5-f6a7-8901-2345b6c7d8e9"),
            Name = "Tesla Model S Plaid",
            Price = 110_000M,
            DescriptionHTML = "<h3>Beyond Ludicrous Speed.</h3><p>Offers staggering acceleration figures, making it one of the quickest production cars ever built.</p>",
            DescriptionText = "Beyond Ludicrous Speed.\nOffers staggering acceleration figures, making it one of the quickest production cars ever built.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = teslaId,
            ShortId = 10049,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });

        builder.HasData(new Product
        {
            Id = Guid.Parse("a1b2c3d4-e5f6-a7b8-9012-3456c7d8e9f0"),
            Name = "Tesla Cybertruck",
            Price = 70_000M,
            DescriptionHTML = "<h3>Radical Electric Pickup.</h3><p>A futuristic pickup truck with an exoskeleton design, offering utility, performance, and durability.</p>",
            DescriptionText = "Radical Electric Pickup.\nA futuristic pickup truck with an exoskeleton design, offering utility, performance, and durability.\n",
            CreatedOn = baseDate.AddDays(-10),
            CategoryId = teslaId,
            ShortId = 10050,
            ConcurrencyStamp = defaultConcurrencyStamp,
            HasPrimaryImage = false
        });
    }
}

