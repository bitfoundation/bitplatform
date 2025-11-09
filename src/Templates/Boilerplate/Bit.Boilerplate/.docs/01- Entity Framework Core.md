# Stage 1: Entity Framework Core

Welcome to the interactive Getting Started guide! This document will walk you through the Entity Framework Core architecture in this Boilerplate project.

---

## Overview

Entity Framework Core (EF Core) is the primary data access technology used in this project. It provides an object-relational mapping (ORM) layer that allows you to work with databases using .NET objects, eliminating much of the data access code you'd normally need to write.

In this stage, you'll learn about:
- **AppDbContext**: The central database context for server-side data access
- **Entity Models**: How domain entities are defined
- **Entity Type Configurations**: Best practices for configuring entity mappings
- **Migrations**: How to manage database schema changes (optional for server-side)
<!--#if (offlineDb == true)-->
- **Client-Side Offline Database**: How the client-side database works differently
<!--#endif-->

---

## 1. AppDbContext - The Heart of Data Access

### Location
The main database context is located at:
[`/src/Server/Boilerplate.Server.Api/Data/AppDbContext.cs`](/src/Server/Boilerplate.Server.Api/Data/AppDbContext.cs)

### What is AppDbContext?

`AppDbContext` is the central class that coordinates Entity Framework functionality for your data model. It inherits from `IdentityDbContext`, which provides built-in support for ASP.NET Core Identity (users, roles, authentication).

### Key Features

Here's the `AppDbContext` structure from the actual project:

```csharp
public partial class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Role, Guid, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>(options), 
      IDataProtectionKeyContext
{
    // DbSets represent tables in the database
    public DbSet<UserSession> UserSessions { get; set; } = default!;
    public DbSet<TodoItem> TodoItems { get; set; } = default!;
    public DbSet<Category> Categories { get; set; } = default!;
    public DbSet<Product> Products { get; set; } = default!;
    public DbSet<PushNotificationSubscription> PushNotificationSubscriptions { get; set; } = default!;
    public DbSet<WebAuthnCredential> WebAuthnCredential { get; set; } = default!;
    public DbSet<SystemPrompt> SystemPrompts { get; set; } = default!;
    public DbSet<Attachment> Attachments { get; set; } = default!;
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all entity configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        // Configure Identity table names
        ConfigureIdentityTableNames(modelBuilder);
        
        // Configure concurrency stamps for optimistic concurrency control
        ConfigureConcurrencyStamp(modelBuilder);
    }
}
```

### What Does Each DbSet Represent?

Each `DbSet<T>` property represents a **table** in your database:
- `Categories` → `Categories` table
- `Products` → `Products` table
- `TodoItems` → `TodoItems` table
- And so on...

You query and save data through these properties:
```csharp
// Example: Get all categories
var categories = await dbContext.Categories.ToListAsync();

// Example: Add a new category
var newCategory = new Category { Name = "Honda", Color = "#FF5733" };
dbContext.Categories.Add(newCategory);
await dbContext.SaveChangesAsync();
```

---

## 2. Entity Models - Defining Your Domain

### Location
Entity models are organized by domain in:
[`/src/Server/Boilerplate.Server.Api/Models/`](/src/Server/Boilerplate.Server.Api/Models/)

The folder structure is:
```
Models/
├── Categories/
│   └── Category.cs
├── Products/
│   └── Product.cs
├── Todo/
│   └── TodoItem.cs
├── Identity/
│   └── User.cs, Role.cs, etc.
└── ... other domains
```

### Example: Category Entity

Let's examine the `Category` entity from the project:

**File:** [`/src/Server/Boilerplate.Server.Api/Models/Categories/Category.cs`](/src/Server/Boilerplate.Server.Api/Models/Categories/Category.cs)

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

### Understanding the Entity Structure

#### 1. **Primary Key**
```csharp
public Guid Id { get; set; }
```
- Every entity **must** have an `Id` property
- Using `Guid` provides globally unique identifiers
- EF Core automatically recognizes this as the primary key

#### 2. **Data Annotations**
```csharp
[Required, MaxLength(64)]
public string? Name { get; set; }
```
- `[Required]`: This field cannot be null in the database
- `[MaxLength(64)]`: Limits the string length to 64 characters
- These annotations affect both database schema and validation

#### 3. **ConcurrencyStamp**
```csharp
public byte[] ConcurrencyStamp { get; set; } = [];
```
- **Critical for optimistic concurrency control**
- Configured as a **row version** in SQL Server
- Automatically prevents lost updates when multiple users edit the same record
- You **must** include this in all entities that will be updated

#### 4. **Navigation Properties**
```csharp
public IList<Product> Products { get; set; } = [];
```
- Represents the **relationship** between `Category` and `Product`
- One category can have many products (one-to-many relationship)
- EF Core uses this to generate foreign key relationships in the database

---

## 3. Entity Type Configurations - The Professional Approach

### Why Use Entity Type Configurations?

Instead of cluttering your entity classes with `Fluent API` configuration code or excessive data annotations, the project uses **separate configuration classes**. This provides:

✅ **Separation of Concerns**: Entity classes remain clean and focused on domain logic  
✅ **Better Organization**: All database mapping logic is in one place  
✅ **Easier Testing**: Entities are POCOs (Plain Old CLR Objects) without database concerns  
✅ **Improved Readability**: Configuration is easier to find and maintain

### Location
Entity configurations are located at:
[`/src/Server/Boilerplate.Server.Api/Data/Configurations/`](/src/Server/Boilerplate.Server.Api/Data/Configurations/)

The folder structure mirrors the Models folder:
```
Configurations/
├── Category/
│   └── CategoryConfiguration.cs
├── Product/
│   └── ProductConfiguration.cs
├── Identity/
│   └── UserConfiguration.cs, RoleConfiguration.cs
└── ... other configurations
```

### Example: CategoryConfiguration

**File:** [`/src/Server/Boilerplate.Server.Api/Data/Configurations/Category/CategoryConfiguration.cs`](/src/Server/Boilerplate.Server.Api/Data/Configurations/Category/CategoryConfiguration.cs)

```csharp
using Boilerplate.Server.Api.Models.Categories;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public partial class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // Configure unique index on Name
        builder.HasIndex(p => p.Name).IsUnique();

        // Seed initial data
        var defaultConcurrencyStamp = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        builder.HasData(
            new Category { 
                Id = Guid.Parse("31d78bd0-0b4f-4e87-b02f-8f66d4ab2845"), 
                Name = "Ford", 
                Color = "#FFCD56", 
                ConcurrencyStamp = defaultConcurrencyStamp 
            },
            new Category { 
                Id = Guid.Parse("582b8c19-0709-4dae-b7a6-fa0e704dad3c"), 
                Name = "Nissan", 
                Color = "#FF6384", 
                ConcurrencyStamp = defaultConcurrencyStamp 
            },
            new Category { 
                Id = Guid.Parse("6fae78f3-b067-40fb-a2d5-9c8dd5eb2e08"), 
                Name = "Benz", 
                Color = "#4BC0C0", 
                ConcurrencyStamp = defaultConcurrencyStamp 
            },
            new Category { 
                Id = Guid.Parse("ecf0496f-f1e3-4d92-8fe4-0d7fa2b4ffa4"), 
                Name = "BMW", 
                Color = "#FF9124", 
                ConcurrencyStamp = defaultConcurrencyStamp 
            },
            new Category { 
                Id = Guid.Parse("747f6d66-7524-40ca-8494-f65e85b5ee5d"), 
                Name = "Tesla", 
                Color = "#2B88D8", 
                ConcurrencyStamp = defaultConcurrencyStamp 
            }
        );
    }
}
```

### Understanding the Configuration

#### 1. **Unique Indexes**
```csharp
builder.HasIndex(p => p.Name).IsUnique();
```
- Creates a unique index on the `Name` column
- Ensures no two categories can have the same name
- Database will enforce this constraint at the SQL level

#### 2. **Seed Data with HasData()**
```csharp
builder.HasData(
    new Category { Id = Guid.Parse("..."), Name = "Ford", ... }
);
```
- Pre-populates the database with initial data
- Useful for development, testing, and demo scenarios
- Data is inserted when the database is created

### How Configurations Are Applied

In `AppDbContext.OnModelCreating()`:
```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
```

This single line:
- **Scans** the entire assembly for classes implementing `IEntityTypeConfiguration<T>`
- **Automatically applies** all configurations
- You don't need to manually register each configuration class

---

## 4. Migrations (Optional for Server-Side)

### Important Note About Migrations

**EF Core migrations are NOT mandatory** in this project, especially for:
- Test projects
- Rapid prototyping scenarios
- Development environments where the database can be easily recreated

### Default Approach: EnsureCreatedAsync()

By default, the project uses `Database.EnsureCreatedAsync()` which **automatically creates** the database schema based on your entities without requiring migrations:

**File:** [`/src/Server/Boilerplate.Server.Api/Program.cs`](/src/Server/Boilerplate.Server.Api/Program.cs)
```csharp
if (builder.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync(); // Automatically creates schema
}
```

### When to Use Migrations?

You should **switch to migrations** when:
- Deploying to **production environments**
- You need to **preserve existing data** during schema changes
- You want **version control** for your database schema
- You're working in a **team environment** where schema changes need to be tracked

### How to Switch to Migrations

If you decide to use migrations, follow these steps:

#### Step 1: Replace EnsureCreatedAsync() with MigrateAsync()

Replace `EnsureCreatedAsync()` with `MigrateAsync()` in these 3 files:
1. [`/src/Server/Boilerplate.Server.Api/Program.cs`](/src/Server/Boilerplate.Server.Api/Program.cs)
2. [`/src/Server/Boilerplate.Server.Web/Program.cs`](/src/Server/Boilerplate.Server.Web/Program.cs)
3. [`/src/Tests/TestsInitializer.cs`](/src/Tests/TestsInitializer.cs)

**Before:**
```csharp
await dbContext.Database.EnsureCreatedAsync();
```

**After:**
```csharp
await dbContext.Database.MigrateAsync();
```

#### Step 2: Create Your First Migration

Open a terminal in the `Boilerplate.Server.Api` project directory and run:

```bash
dotnet ef migrations add InitialMigration --output-dir Data/Migrations --verbose
```

This creates migration files in the `/Data/Migrations/` folder.

#### Step 3: Apply the Migration

The migration will be **automatically applied** when the application starts (thanks to `MigrateAsync()`).

### Adding Future Migrations

When you modify entities or configurations, create a new migration:

```bash
dotnet ef migrations add AddNewPropertyToCategory --output-dir Data/Migrations --verbose
```

**Important:** Always use descriptive migration names that explain what changed (e.g., `AddEmailToUser`, `CreateProductsTable`).

---
<!--#if (offlineDb == true)-->
## 5. Client-Side Offline Database

### What Makes It Different?

This project includes a **client-side offline database** that allows the application to work **without an internet connection**. This is fundamentally different from the server-side database.

### Key Characteristics

#### Per-Client Database
- Each client (web browser, mobile app, desktop app) has its **own local database**
- Stored locally on the user's device
- Independent of the server database

#### Manual Management Not Feasible
- There will be **as many databases as there are clients** (potentially millions)
- You cannot manually manage each client's database
- **Migrations are the only viable approach**

#### Migration-Only Approach
For client-side databases:
- ❌ **DO NOT use** `EnsureCreatedAsync()`
- ✅ **ALWAYS use** `MigrateAsync()` and EF Core migrations
- Migrations ensure controlled, versioned schema updates across all client devices

### Location and Technology

**DbContext Location:** [`/src/Client/Boilerplate.Client.Core/Data/OfflineDbContext.cs`](/src/Client/Boilerplate.Client.Core/Data/OfflineDbContext.cs)

**Technology:** [bit Besql](https://bitplatform.dev/besql) - EF Core SQLite for Blazor

```csharp
public partial class OfflineDbContext(DbContextOptions<OfflineDbContext> options) : DbContext(options)
{
    public virtual DbSet<UserDto> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed initial offline user data
        modelBuilder.Entity<UserDto>()
            .HasData([new()
            {
                Id = Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"),
                Email = "test@bitplatform.dev",
                UserName = "test",
                FullName = "Boilerplate test account"
            }]);
        
        base.OnModelCreating(modelBuilder);
    }
}
```

### How Migrations Are Applied

Migrations are **automatically applied** when the application starts on the client:

**File:** [`/src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs)
```csharp
dbContextInitializer: async (sp, dbContext) => 
    await Task.Run(async () => await dbContext.Database.MigrateAsync())
```

This ensures:
- First-time users get the initial schema
- Existing users get schema updates automatically
- No data loss during updates

### Creating Client-Side Migrations

To add a migration for `OfflineDbContext`, follow these steps:

#### Option 1: Using Package Manager Console (Visual Studio)

1. Set `Boilerplate.Server.Web` as the **Startup Project**
2. Set `Boilerplate.Client.Core` as the **Default Project** in Package Manager Console
3. Run:
```powershell
Add-Migration YourMigrationName -OutputDir Data\Migrations -Context OfflineDbContext -Verbose
```

#### Option 2: Using dotnet CLI

Open a terminal in the `Boilerplate.Server.Web` project directory and run:
```bash
dotnet ef migrations add YourMigrationName --context OfflineDbContext --output-dir Data/Migrations --project ../Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj --verbose
```

**Important Notes:**
- Ensure the solution builds successfully before running migration commands
- Do **NOT** run `Update-Database` for client-side migrations
- The migration is automatically applied via `MigrateAsync()` when the app starts on each device

### Additional Resources

For comprehensive information about the client-side offline database, including:
- Advanced configuration
- Performance optimization with compiled models
- Debugging SQLite databases in the browser
- Downloading the database file for inspection

**See:** [`/src/Client/Boilerplate.Client.Core/Data/README.md`](/src/Client/Boilerplate.Client.Core/Data/README.md)

---
<!--#endif-->

## Summary

In this stage, you learned about:

✅ **AppDbContext**: The central database context located at `/src/Server/Boilerplate.Server.Api/Data/AppDbContext.cs`  
✅ **Entity Models**: Defined in `/src/Server/Boilerplate.Server.Api/Models/` with required properties like `Id` and `ConcurrencyStamp`  
✅ **Entity Type Configurations**: Best practice for separating mapping logic, located in `/src/Server/Boilerplate.Server.Api/Data/Configurations/`  
✅ **Migrations (Optional)**: For server-side, `EnsureCreatedAsync()` is used by default; migrations are recommended for production  
<!--#if (offlineDb == true)-->
✅ **Client-Side Offline Database**: Uses `OfflineDbContext` with **mandatory migrations** and automatic application on startup
<!--#endif-->

---