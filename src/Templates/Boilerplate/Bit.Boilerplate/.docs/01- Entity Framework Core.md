# Stage 1: Entity Framework Core

Welcome to Stage 1 of the getting started guide! In this stage, you'll learn about Entity Framework Core architecture in this project, including DbContext, entity models, entity type configurations, and migrations.

---

## 📂 AppDbContext - The Heart of Data Access

The `AppDbContext` is the central data access component in this project. It's your gateway to the database and manages all entity sets.

### Location
[`/src/Server/Boilerplate.Server.Api/Data/AppDbContext.cs`](../src/Server/Boilerplate.Server.Api/Data/AppDbContext.cs)

### What is AppDbContext?

`AppDbContext` inherits from `IdentityDbContext<User, Role, Guid, ...>`, which means it:
- Provides built-in ASP.NET Core Identity tables (Users, Roles, UserRoles, etc.)
- Manages all your custom entities
- Handles database connections and transactions
- Implements `IDataProtectionKeyContext` for ASP.NET Core Data Protection

### Key Features

#### DbSets - Your Entity Tables
Each `DbSet<T>` property represents a table in the database:

```csharp
public DbSet<UserSession> UserSessions { get; set; } = default!;
public DbSet<TodoItem> TodoItems { get; set; } = default!;
public DbSet<Category> Categories { get; set; } = default!;
public DbSet<Product> Products { get; set; } = default!;
public DbSet<PushNotificationSubscription> PushNotificationSubscriptions { get; set; } = default!;
public DbSet<WebAuthnCredential> WebAuthnCredential { get; set; } = default!;
public DbSet<SystemPrompt> SystemPrompts { get; set; } = default!;
public DbSet<Attachment> Attachments { get; set; } = default!;
public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
```

#### OnModelCreating - Configuration Hub
This method is where all entity configurations are applied:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Apply all IEntityTypeConfiguration implementations automatically
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    // Configure Identity table names
    ConfigureIdentityTableNames(modelBuilder);

    // Configure ConcurrencyStamp for optimistic concurrency control
    ConfigureConcurrencyStamp(modelBuilder);
}
```

#### Concurrency Control
The project implements **optimistic concurrency control** using `ConcurrencyStamp`:

```csharp
public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
{
    try
    {
        SetConcurrencyStamp();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    catch (DbUpdateConcurrencyException exception)
    {
        throw new ConflictException(nameof(AppStrings.UpdateConcurrencyException), exception);
    }
}
```

**What this means:**
- When two users try to update the same record simultaneously, the second update will fail with a `ConflictException`
- This prevents data loss and ensures data integrity
- Users see a user-friendly error message asking them to refresh and try again

---

## 🏗️ Entity Models - Your Data Structure

Entity models define the structure of your data. They are POCOs (Plain Old CLR Objects) that map to database tables.

### Location
[`/src/Server/Boilerplate.Server.Api/Models/`](../src/Server/Boilerplate.Server.Api/Models/)

The Models folder is organized by feature:
- **Categories/** - Category entities
- **Products/** - Product entities
- **Identity/** - User, Role, and identity-related entities
- **Todo/** - TodoItem entities
- **PushNotification/** - Push notification subscription entities
- **Attachments/** - File attachment entities
- **Chatbot/** - AI chatbot entities

### Example 1: Category Entity

**File:** [`/src/Server/Boilerplate.Server.Api/Models/Categories/Category.cs`](../src/Server/Boilerplate.Server.Api/Models/Categories/Category.cs)

```csharp
using Boilerplate.Server.Api.Models.Products;

namespace Boilerplate.Server.Api.Models.Categories;

public partial class Category
{
    public Guid Id { get; set; }

    [Required, MaxLength(64)]
    public string? Name { get; set; }

    public string? Color { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = [];

    public IList<Product> Products { get; set; } = [];
}
```

**Key characteristics:**
- ✅ `Id` property of type `Guid` (primary key)
- ✅ `ConcurrencyStamp` property for optimistic concurrency control
- ✅ Data annotations (`[Required]`, `[MaxLength]`) for validation
- ✅ Navigation property (`Products`) for relationships
- ✅ Nullable reference types enabled (`string?`)

---

## ⚙️ Entity Type Configurations - Fluent API

Entity Type Configurations use **Fluent API** to configure your entities in a clean, organized way separate from the entity classes.

### Location
[`/src/Server/Boilerplate.Server.Api/Data/Configurations/`](../src/Server/Boilerplate.Server.Api/Data/Configurations/)

The Configurations folder mirrors the Models folder structure:
- **Category/** - Category configurations
- **Product/** - Product configurations
- **Identity/** - Identity-related configurations
- **Todo/** - Todo configurations
- etc.

### Benefits of Entity Type Configurations

✅ **Separation of Concerns** - Keep entity classes clean, put complex configuration in separate files  
✅ **Fluent API Power** - Access advanced configurations not available via data annotations  
✅ **Better Organization** - Each entity has its own configuration file  
✅ **Automatic Discovery** - All configurations are automatically applied via `ApplyConfigurationsFromAssembly()`  
✅ **Seed Data** - Perfect place to add initial data using `HasData()`

### Example 1: CategoryConfiguration

**File:** [`/src/Server/Boilerplate.Server.Api/Data/Configurations/Category/CategoryConfiguration.cs`](../src/Server/Boilerplate.Server.Api/Data/Configurations/Identity/CategoryConfiguration.cs)

```csharp
using Boilerplate.Server.Api.Models.Categories;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public partial class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // Create a unique index on Name
        builder.HasIndex(p => p.Name).IsUnique();

        // Seed initial data
        var defaultConcurrencyStamp = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        builder.HasData(
            new Category { Id = Guid.Parse("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), Name = "Ford", Color = "#FFCD56", ConcurrencyStamp = defaultConcurrencyStamp },
            new Category { Id = Guid.Parse("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), Name = "Nissan", Color = "#FF6384", ConcurrencyStamp = defaultConcurrencyStamp },
            new Category { Id = Guid.Parse("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), Name = "Benz", Color = "#4BC0C0", ConcurrencyStamp = defaultConcurrencyStamp },
            new Category { Id = Guid.Parse("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), Name = "BMW", Color = "#FF9124", ConcurrencyStamp = defaultConcurrencyStamp },
            new Category { Id = Guid.Parse("747f6d66-7524-40ca-8494-f65e85b5ee5d"), Name = "Tesla", Color = "#2B88D8", ConcurrencyStamp = defaultConcurrencyStamp });
    }
}
```

**What this configuration does:**
- Creates a **unique index** on the `Name` column (prevents duplicate category names)
- Seeds **5 initial categories** into the database (Ford, Nissan, Benz, BMW, Tesla)

### Example 2: UserConfiguration

**File:** [`/src/Server/Boilerplate.Server.Api/Data/Configurations/Identity/UserConfiguration.cs`](../src/Server/Boilerplate.Server.Api/Data/Configurations/Identity/UserConfiguration.cs)

```csharp
public partial class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Configure relationships
        builder.HasMany(user => user.Roles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId);

        builder.HasMany(user => user.Claims)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId);

        builder.HasMany(user => user.Tokens)
            .WithOne(ut => ut.User)
            .HasForeignKey(ut => ut.UserId);

        builder.HasMany(user => user.Logins)
            .WithOne(ul => ul.User)
            .HasForeignKey(ul => ul.UserId);

        // Seed test user
        const string userName = "test";
        const string email = "test@bitplatform.dev";

        builder.HasData([new User
        {
            Id = Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"),
            EmailConfirmed = true,
            LockoutEnabled = true,
            Gender = Gender.Other,
            BirthDate = new DateTimeOffset(new DateOnly(2023, 1, 1), default, default),
            FullName = "Boilerplate test account",
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailTokenRequestedOn = new DateTimeOffset(new DateOnly(2023, 1, 1), default, default),
            PhoneNumber = "+31684207362",
            PhoneNumberConfirmed = true,
            SecurityStamp = "959ff4a9-4b07-4cc1-8141-c5fc033daf83",
            ConcurrencyStamp = "315e1a26-5b3a-4544-8e91-2760cd28e231",
            PasswordHash = "AQAAAAIAAYagAAAAEP0v3wxkdWtMkHA3Pp5/JfS+42/Qto9G05p2mta6dncSK37hPxEHa3PGE4aqN30Aag==", // 123456
        }]);

        // Create unique indexes with filters (for nullable columns)
        builder
            .HasIndex(b => b.Email)
            .HasFilter($"[{nameof(User.Email)}] IS NOT NULL")
            .IsUnique();

        builder
            .HasIndex(b => b.PhoneNumber)
            .HasFilter($"[{nameof(User.PhoneNumber)}] IS NOT NULL")
            .IsUnique();
    }
}
```

**What this configuration does:**
- Configures **one-to-many relationships** between User and related entities
- Seeds a **test user** (username: `test`, password: `123456`)
- Creates **unique filtered indexes** on Email and PhoneNumber (only where they're not null)

---

## 🔄 Migrations - Managing Database Schema Changes (Optional)

Migrations are EF Core's way of evolving your database schema over time as your entity models change.

### Location
[`/src/Server/Boilerplate.Server.Api/Data/Migrations/`](../src/Server/Boilerplate.Server.Api/Data/Migrations/)

### ⚠️ Important: Migrations are Recommended

Using EF Core migrations is **not mandatory** for:
- ✅ Test projects or rapid prototyping
- ✅ Development environments where you can easily recreate the database
- ✅ Scenarios where you use `EnsureCreatedAsync()`

**Current approach in the project:**

The project currently uses `EnsureCreatedAsync()` in [`Program.cs`](../src/Server/Boilerplate.Server.Api/Program.cs):

```csharp
await dbContext.Database.EnsureCreatedAsync(); // It's recommended to start using ef-core migrations.
```

**What `EnsureCreatedAsync()` does:**
- Creates the database if it doesn't exist
- Creates all tables based on your entity models
- **Does NOT** create migrations or track schema changes
- **Simple and fast** for development

**When to switch to migrations:**
- ✅ Production environments
- ✅ When you need to preserve existing data during schema changes
- ✅ When you want version-controlled database schema history
- ✅ When you need to roll back schema changes

### Creating Migrations (When Needed)

If you decide to use migrations, here's how to create them:

#### Option 1: Using Package Manager Console (Visual Studio)

1. Set **`Boilerplate.Server.Api`** as the default project and startup project in Package Manager Console
3. Run:

```powershell
Add-Migration Initial -Verbose -OutputDir Data\Migrations
```

#### Option 2: Using .NET CLI

Open a terminal in the `Boilerplate.Server.Api` project directory and run:

```bash
dotnet ef migrations add Initial --verbose --output-dir Data/Migrations
```

---
<!--#if (offlineDb == true)-->
## 📱 Client-Side Offline Database

This project also includes a **client-side offline database** that allows the application to work without an internet connection!

### What is the Offline Database?

The offline database is a **SQLite database** that runs entirely on the client device (browser, mobile app, desktop app) using:
- **Bit.Besql** - SQLite for Blazor WebAssembly (runs in the browser!)
- **Microsoft.EntityFrameworkCore.Sqlite** - SQLite for .NET MAUI and Windows apps

### Location
[`/src/Client/Boilerplate.Client.Core/Data/`](../src/Client/Boilerplate.Client.Core/Data/)

**Key files:**
- **`OfflineDbContext.cs`** - The client-side DbContext
- **`README.md`** - Comprehensive guide for working with the offline database
- **`Migrations/`** - Client-side migrations (auto-applied on app startup)

### Key Characteristics

✅ **Per-Client Database** - Each client (web browser, mobile app, desktop app) has its own local database  
✅ **Automatic Synchronization** - Data can sync with the server when online  
✅ **Offline-First** - App works fully offline, syncs when connection is available  
✅ **Migration-Only Approach** - Client-side databases **ONLY** use migrations (NOT `EnsureCreatedAsync()`)

**Why migrations-only for client databases?**
- You cannot manually manage thousands of client databases
- Migrations ensure all clients have the correct schema
- `EnsureCreatedAsync()` doesn't work well for schema updates on existing databases

### Example: OfflineDbContext

**File:** [`/src/Client/Boilerplate.Client.Core/Data/OfflineDbContext.cs`](../src/Client/Boilerplate.Client.Core/Data/OfflineDbContext.cs)

```csharp
using Boilerplate.Shared.Dtos.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Boilerplate.Client.Core.Data;

public partial class OfflineDbContext(DbContextOptions<OfflineDbContext> options) : DbContext(options)
{
    public virtual DbSet<UserDto> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed test user for offline mode
        modelBuilder.Entity<UserDto>()
            .HasData([new()
            {
                Id = Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"),
                Email = "test@bitplatform.dev",
                UserName = "test",
                PhoneNumber = "+31684207362",
                BirthDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Gender = Gender.Other,
                Password = "123456",
                FullName = "Boilerplate test account"
            }]);

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();

        base.ConfigureConventions(configurationBuilder);
    }
}
```

**Key differences from server-side DbContext:**
- Uses **DTOs** (not entities) - stores data in the same format as API responses
- Includes **seed data** for offline testing
- Configured for **SQLite limitations** (DateTimeOffset conversion)

### Creating Client-Side Migrations

**Important:** You must follow these steps carefully when creating client-side migrations.

#### Option 1: Using Package Manager Console (Visual Studio)

1. Set **`Boilerplate.Server.Web`** as the startup project in Solution Explorer
2. Set **`Boilerplate.Client.Core`** as the default project in Package Manager Console
3. Run:

```powershell
Add-Migration Initial -OutputDir Data\Migrations -Context OfflineDbContext -Verbose
```

#### Option 2: Using .NET CLI

Open a terminal in the `Server.Web` project directory and run:

```bash
dotnet ef migrations add Initial --context OfflineDbContext --output-dir Data/Migrations --project ../Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj --verbose
```

**⚠️ Important Notes:**
- **DO NOT** run `Update-Database` for client-side migrations
- Migrations are automatically applied when the app starts on each client device
- The database is created in the browser's cache storage (for WebAssembly) or in the app's local data folder (for MAUI/Windows)

### More Information

For comprehensive details about the offline database, including:
- Performance optimization with compiled models
- Advanced configuration
- Troubleshooting

Read: [`/src/Client/Boilerplate.Client.Core/Data/README.md`](../src/Client/Boilerplate.Client.Core/Data/README.md)

---
<!--#endif-->

## 🎯 Summary

In Stage 1, you learned:

✅ **AppDbContext** - The central data access component located in `/src/Server/Boilerplate.Server.Api/Data/AppDbContext.cs`  
✅ **Entity Models** - POCOs that define your data structure, located in `/src/Server/Boilerplate.Server.Api/Models/`  
✅ **Entity Type Configurations** - Fluent API configurations for clean separation of concerns, located in `/src/Server/Boilerplate.Server.Api/Data/Configurations/`  
✅ **Concurrency Control** - Automatic optimistic concurrency with `ConcurrencyStamp`  
✅ **Migrations (Optional)** - Can use `EnsureCreatedAsync()` for rapid development, switch to migrations for production  
<!--#if (offlineDb == true)-->
✅ **Client-Side Offline Database** - SQLite database on each client device with automatic migrations and offline-first capabilities
<!--#endif-->

---

## 📚 Key Concepts to Remember

1. **Entity Models** → Define data structure (properties, relationships)
2. **Entity Type Configurations** → Configure entities with Fluent API (indexes, seed data, relationships)
3. **AppDbContext** → Manages database connections and applies configurations
4. **Migrations** → Optional for development, recommended for production
<!--#if (offlineDb == true)-->
5. **Offline Database** → Client-side SQLite for offline-first applications
<!--#endif-->

---
