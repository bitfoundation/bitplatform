# Stage 2: DTOs, Mappers, and Mapperly

Welcome to Stage 2! In this stage, we'll explore how the project uses **Data Transfer Objects (DTOs)**, **Mapperly** for high-performance object mapping, and the role of **AppJsonContext** for optimized JSON serialization.

---

## Table of Contents

1. [What are DTOs?](#what-are-dtos)
2. [Example DTO: CategoryDto](#example-dto-categorydto)
3. [AppJsonContext: Optimized JSON Serialization](#appjsoncontext-optimized-json-serialization)
4. [What is Mapperly?](#what-is-mapperly)
5. [Mapper Example: CategoriesMapper](#mapper-example-categoriesmapper)
6. [Project() vs Map(): Reading Data Efficiently](#project-vs-map-reading-data-efficiently)
7. [Manual Projection Alternative](#manual-projection-alternative)
8. [Mapperly Usage in Client.Core: Patching DTOs](#mapperly-usage-in-clientcore-patching-dtos)
9. [Summary](#summary)

---

## What are DTOs?

**Data Transfer Objects (DTOs)** are simple objects used to transfer data between different layers of the application, particularly between the server and client. They serve several important purposes:

- **Data Shaping**: DTOs control exactly what data is sent over the network
- **Security**: Hide sensitive entity properties that shouldn't be exposed to clients
- **Validation**: Apply validation rules using data annotations
- **API Contracts**: Define clear contracts between frontend and backend
- **Performance**: Reduce payload size by only including necessary properties

In this project, all DTOs are located in the **`Shared/Dtos`** folder and are accessible to both server and client projects.

---

## Example DTO: CategoryDto

Let's examine a real DTO from the project: [`CategoryDto`](/src/Shared/Dtos/Categories/CategoryDto.cs)

```csharp
namespace Boilerplate.Shared.Dtos.Categories;

[DtoResourceType(typeof(AppStrings))]
public partial class CategoryDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Name))]
    [MaxLength(64, ErrorMessage = nameof(AppStrings.MaxLengthAttribute_InvalidMaxLength))]
    public string? Name { get; set; }

    [Display(Name = nameof(AppStrings.Color))]
    public string? Color { get; set; } = "#FFFFFF";

    public int ProductsCount { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = [];
}
```

### Key Features of this DTO:

1. **`[DtoResourceType(typeof(AppStrings))]`**: Connects validation messages and display names to localized resource files (we'll cover this in Stage 5)

2. **Validation Attributes**: 
   - `[Required]`: Ensures the Name field is not empty
   - `[MaxLength(64)]`: Limits the Name to 64 characters
   - `[Display]`: Provides human-readable labels for UI controls

3. **Calculated Properties**:
   - `ProductsCount`: This property doesn't exist in the database entity but is calculated during projection

4. **Concurrency Control**:
   - `ConcurrencyStamp`: Used for optimistic concurrency control to prevent data conflicts

---

## AppJsonContext: Optimized JSON Serialization

**AppJsonContext** is a source-generated JSON serializer context that provides optimal performance for JSON serialization/deserialization in this project.

### What is AppJsonContext?

Located at [`Shared/Dtos/AppJsonContext.cs`](/src/Shared/Dtos/AppJsonContext.cs), it uses .NET's **System.Text.Json Source Generator** to generate serialization code at compile time instead of using reflection at runtime.

```csharp
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(Dictionary<string, string?>))]
[JsonSerializable(typeof(CategoryDto))]
[JsonSerializable(typeof(List<CategoryDto>))]
[JsonSerializable(typeof(PagedResult<CategoryDto>))]
// ... more types
public partial class AppJsonContext : JsonSerializerContext
{
}
```

### Benefits:

- ⚡ **Performance**: Faster serialization (no reflection overhead)
- 📦 **Trimming-Friendly**: Works perfectly with .NET's assembly trimming for smaller app sizes
- 🚀 **AOT Compatible**: Essential for Native AOT compilation scenarios
- 🔒 **Type Safety**: Compile-time checking of serializable types

### Important Rule:

**Every DTO that will be sent over HTTP must be registered in AppJsonContext** using `[JsonSerializable(typeof(YourDto))]`.

If you forget to register a DTO, you'll get a helpful exception at runtime pointing you to add it to AppJsonContext.

---

## What is Mapperly?

**Mapperly** is a .NET source generator that creates high-performance object-to-object mapping code at compile time. Unlike reflection-based mappers (like AutoMapper), Mapperly generates explicit C# code during build, resulting in:

- ⚡ Zero runtime overhead
- 🔍 Compile-time validation of mappings
- 📖 Readable, debuggable generated code
- 🚀 Native AOT and trimming friendly

### How Mapperly Works:

1. You define a `static partial class` with the `[Mapper]` attribute
2. You declare `partial` methods that define the mapping signatures
3. At build time, Mapperly generates the implementation for these methods
4. The generated code performs efficient property-to-property copying

For more details, visit the [Mapperly documentation](https://mapperly.riok.app/docs/intro/).

---

## Mapper Example: CategoriesMapper

Let's look at the actual mapper for categories: [`CategoriesMapper.cs`](/src/Server/Boilerplate.Server.Api/Mappers/CategoriesMapper.cs)

```csharp
using Riok.Mapperly.Abstractions;
using Boilerplate.Shared.Dtos.Categories;
using Boilerplate.Server.Api.Models.Categories;

namespace Boilerplate.Server.Api.Mappers;

/// <summary>
/// More info at Server/Mappers/README.md
/// </summary>
[Mapper]
public static partial class CategoriesMapper
{
    public static partial IQueryable<CategoryDto> Project(this IQueryable<Category> query);

    [MapProperty(nameof(@Category.Products.Count), nameof(@CategoryDto.ProductsCount))]
    public static partial CategoryDto Map(this Category source);
    
    public static partial Category Map(this CategoryDto source);
    public static partial void Patch(this CategoryDto source, Category destination);
}
```

### Breaking Down the Mapper:

1. **`[Mapper]` Attribute**: Tells Mapperly to generate code for this class

2. **`static partial class`**: The class must be static and partial so Mapperly can generate the implementation

3. **Extension Methods**: Methods like `Project(this IQueryable<Category>)` become extension methods that can be called fluently

4. **Three Types of Mapping Methods**:

   - **`Project()`**: For efficient database queries (explained next)
   - **`Map()`**: For converting individual objects
   - **`Patch()`**: For updating existing objects with new values

5. **`[MapProperty]` Attribute**: Explicitly maps `Category.Products.Count` to `CategoryDto.ProductsCount`
   - Note: In this case, it's actually unnecessary because Mapperly would figure it out automatically by convention
   - It's included here to demonstrate how to handle complex property mappings

---

## Project() vs Map(): Reading Data Efficiently

This is one of the most important concepts when working with Entity Framework Core and DTOs.

### The Problem:

When you need to return a list of DTOs to the client, you have two options:

**❌ Inefficient Approach (using Map):**
```csharp
var categories = await dbContext.Categories
    .ToListAsync(); // Loads ALL entity properties into memory first
    
var dtos = categories.Select(c => c.Map()).ToList(); // Then maps to DTOs
```

**✅ Efficient Approach (using Project):**
```csharp
var dtos = await dbContext.Categories
    .Project() // Translates to SQL SELECT with only needed columns
    .ToListAsync(); // Executes optimized query
```

### Key Differences:

| Aspect | `Map()` | `Project()` |
|--------|---------|-------------|
| **Execution Location** | In-memory (C# code) | Database-side (SQL query) |
| **Data Retrieved** | ALL entity columns | ONLY DTO columns |
| **Performance** | Slower for large datasets | Optimized for large datasets |
| **Use Case** | Single object mapping | Query projections |
| **Return Type** | `EntityType → DtoType` | `IQueryable<Entity> → IQueryable<Dto>` |

### How Project() Works:

When you call `.Project()` on an `IQueryable<Category>`, Mapperly generates an **Expression Tree** that Entity Framework Core can translate directly into SQL:

```csharp
// Your code:
var query = dbContext.Categories.Project();

// Generated SQL (simplified):
SELECT 
    c.Id, 
    c.Name, 
    c.Color,
    c.ConcurrencyStamp,
    (SELECT COUNT(*) FROM Products p WHERE p.CategoryId = c.Id) as ProductsCount
FROM Categories c
```

Notice how:
- Only the columns needed by `CategoryDto` are selected
- The calculated property `ProductsCount` is computed in SQL
- No unnecessary data is transferred from the database

### Real-World Impact:

Imagine a `Category` entity with 20 properties, but `CategoryDto` only needs 5. Using `Project()`:

- Reduces network traffic between database and server by ~75%
- Reduces memory usage on the server
- Scales efficiently even with millions of records
- Supports paging, filtering, and sorting at the database level (via OData)

---

## Manual Projection Alternative

**Important Note**: Using Mapperly's `Project()` is **not mandatory**. You can perform projection manually using LINQ's `Select()` method:

```csharp
// Manual projection using LINQ Select
var categories = await dbContext.Categories
    .Select(c => new CategoryDto
    {
        Id = c.Id,
        Name = c.Name,
        Color = c.Color,
        ProductsCount = c.Products.Count,
        ConcurrencyStamp = c.ConcurrencyStamp
    })
    .ToListAsync();
```

### Mapperly Project() vs Manual Select():

**Both approaches produce identical SQL queries** and have the same performance. The choice depends on your preferences:

**Manual `Select()` Advantages:**
- ✅ Explicit and clear - you see exactly what's being mapped
- ✅ No source generator dependency
- ✅ More flexibility for complex transformations

**Mapperly `Project()` Advantages:**
- ✅ Reduces repetitive boilerplate code
- ✅ Automatically updated when entity properties change
- ✅ Compile-time validation of property compatibility
- ✅ Consistent mapping logic between `Project()` and `Map()`
- ✅ Less prone to human error (forgetting to map a property)

### Recommendation:

For projects with many DTOs and frequent schema changes, Mapperly's `Project()` reduces maintenance effort. For simple scenarios or teams unfamiliar with Mapperly, manual `Select()` is perfectly fine.

---

## Mapperly Usage in Client.Core: Patching DTOs

In the client application, Mapperly is used in a different but equally important way: **patching DTO objects** to update existing in-memory objects without replacing their references.

### The Problem:

When you update an entity on the server and get the updated DTO back, you need to update the UI. You have two options:

**❌ Replacing the object (breaks two-way binding):**
```csharp
// This creates a new object reference
userDto = await userController.Update(userDto);
// Any UI controls bound to the old userDto reference won't update!
```

**✅ Patching the existing object (preserves two-way binding):**
```csharp
var updatedUser = await userController.Update(userDto);
updatedUser.Patch(userDto); // Copies properties to existing object
// UI controls remain bound and automatically update!
```

### The Patch Mapper:

Located at [`Shared/Mapper.cs`](/src/Shared/Mapper.cs):

```csharp
/// <summary>
/// Patching methods help you patch the DTO you have received from the server 
/// (for example, after calling an Update api) onto the DTO you have bound to the UI. 
/// This way, the UI gets updated with the latest stored changes,
/// and there's no need to re-fetch that specific data from the server.
/// </summary>
[Mapper(UseDeepCloning = true)]
public static partial class Mapper
{
    public static partial void Patch(this CategoryDto source, CategoryDto destination);
    public static partial void Patch(this ProductDto source, ProductDto destination);
    public static partial void Patch(this UserDto source, UserDto destination);
    // ... more DTOs
}
```

### Real-World Example: ProfileSection

From [`ProfileSection.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Pages/Settings/ProfileSection.razor.cs):

```csharp
private async Task SaveProfile()
{
    if (isSaving || CurrentUser is null) return;

    isSaving = true;

    try
    {
        // First, update the CurrentUser with form values
        editUserDto.Patch(CurrentUser);

        // Call the API and get the server's response
        var updatedUser = await userController.Update(editUserDto, CurrentCancellationToken);
        
        // Patch the server response back onto CurrentUser
        // This ensures CurrentUser has any server-calculated values (like ConcurrencyStamp)
        updatedUser.Patch(CurrentUser);

        PublishUserDataUpdated();

        SnackBarService.Success(Localizer[nameof(AppStrings.ProfileUpdatedSuccessfullyMessage)]);
    }
    catch (KnownException e)
    {
        SnackBarService.Error(e.Message);
    }
    finally
    {
        isSaving = false;
    }
}
```

### Why This Matters:

1. **Blazor Two-Way Binding**: When you use `@bind` in Blazor, it binds to the object reference. Patching updates the object in-place, so the UI automatically reflects changes.

2. **Server-Calculated Values**: The server might update properties like `ConcurrencyStamp` or calculated fields. Patching ensures the client has the latest values.

3. **Avoids Re-fetching**: You don't need to call the API again just to get the updated object.

4. **Deep Cloning**: The `[Mapper(UseDeepCloning = true)]` attribute ensures nested objects are also properly copied, not just referenced.

---

## Summary

In this stage, you learned:

✅ **DTOs** are data contracts that transfer data between client and server  
✅ **AppJsonContext** provides optimized, source-generated JSON serialization  
✅ **Mapperly** is a compile-time source generator for efficient object mapping  
✅ **`Project()`** translates to SQL for efficient database queries (recommended for reading data)  
✅ **`Map()`** converts individual objects in memory (used for create/update operations)  
✅ **`Patch()`** updates existing objects without breaking references (essential for Blazor binding)  
✅ **Manual `Select()`** is a valid alternative to `Project()` - both produce identical SQL  

### Key Takeaways:

- Always use `Project()` (or manual `Select()`) when querying `IQueryable` for best performance
- Use `Map()` when converting single DTOs to entities for create/update operations
- Use `Patch()` on the client to update bound objects without breaking two-way binding
- Register all DTOs in `AppJsonContext` for optimal JSON serialization

### Additional Resources:

- **Mapperly Documentation**: https://mapperly.riok.app/docs/intro/
- **Mapper README**: [/src/Server/Boilerplate.Server.Api/Mappers/Readme.md](/src/Server/Boilerplate.Server.Api/Mappers/Readme.md)
- **System.Text.Json Source Generation**: https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/

---