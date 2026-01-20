# Stage 2: DTOs, Mappers, and Mapperly

Welcome to **Stage 2** of the Boilerplate project tutorial! In this stage, you'll learn about:

- **DTOs (Data Transfer Objects)**: How data is transferred between client and server
- **AppJsonContext**: Efficient JSON serialization configuration
- **Mappers with Mapperly**: High-performance object mapping for reading and writing data
- **Project vs Map**: Understanding when to use each method
- **Patch Methods**: Efficient client-side state management

---

## 1. What are DTOs?

**DTOs (Data Transfer Objects)** are simple classes that represent the data structure sent between the client and server. They serve multiple purposes:

- **Decoupling**: Separate your database entities from your API contracts
- **Security**: Control exactly what data is exposed to clients
- **Validation**: Add validation attributes to ensure data integrity
- **Documentation**: Self-documenting API contracts

### Example: CategoryDto

Let's look at a real DTO from the project: [`CategoryDto`](/src/Shared/Features/Categories/CategoryDto.cs)

```csharp
namespace Boilerplate.Shared.Dtos.Categories;

[DtoResourceType(typeof(AppStrings))]
public partial class CategoryDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Name))]
    public string? Name { get; set; }

    [Display(Name = nameof(AppStrings.Color))]
    public string? Color { get; set; }

    public int ProductsCount { get; set; }

    public byte[] Version { get; set; } = [];
}
```

### Key Features of DTOs in This Project

1. **`[DtoResourceType(typeof(AppStrings))]`**: Connects the DTO to localization resources for multi-language validation messages and display names. This would be discussed in details in upcoming stages.

2. **Validation Attributes**: 
   - `[Required]`: Ensures the field is not empty
   - `[MaxLength]`: Limits the length of string properties
   - `[EmailAddress]`, `[Phone]`: Validates format of specific data types

3. **Calculated Properties**: `ProductsCount` is a computed property that shows the count of products in a category (not stored in the database)

4. **Version**: Used for optimistic concurrency control to prevent conflicting updates

### Example: UserDto

Another example is [`UserDto`](/src/Shared/Features/Identity/Dtos/UserDto.cs):

```csharp
[DtoResourceType(typeof(AppStrings))]
public partial class UserDto : IValidatableObject
{
    public Guid Id { get; set; }

    [Display(Name = nameof(AppStrings.UserName))]
    public string? UserName { get; set; }

    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Email) && string.IsNullOrEmpty(PhoneNumber))
            yield return new ValidationResult(
                errorMessage: nameof(AppStrings.EitherProvideEmailOrPhoneNumber),
                memberNames: [nameof(Email), nameof(PhoneNumber)]
            );
    }
}
```

**Note**: `UserDto` implements `IValidatableObject` for complex validation rules that can't be expressed with simple attributes.

---

## 2. AppJsonContext - Efficient JSON Serialization

**Location**: [`src/Shared/Infrastructure/Dtos/AppJsonContext.cs`](/src/Shared/Infrastructure/Dtos/AppJsonContext.cs)

The project uses **System.Text.Json Source Generator** for high-performance JSON serialization. Every DTO used in API/SignalR communication must be registered in `AppJsonContext`.

```csharp
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CategoryDto))]
[JsonSerializable(typeof(List<CategoryDto>))]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(List<ProductDto>))]
// ... more types
public partial class AppJsonContext : JsonSerializerContext
{
}
```

### Why AppJsonContext?

1. **Performance**: Source generation eliminates reflection at runtime
2. **AOT Compatibility**: Required for ahead-of-time compilation scenarios
3. **Type Safety**: Compile-time errors if serialization isn't supported

### When to Update AppJsonContext

**You must add a `[JsonSerializable]` attribute whenever you:**
- Create a new DTO
- Return a `List<T>` or `PagedResponse<T>` of a DTO from an API
- Use a DTO in SignalR communication

---

## 3. Mappers with Mapperly

**Mapperly** is a high-performance object mapping library that generates mapping code at compile time (no reflection!).

**More info**: [Server/Features/Mappers.md](/src/Server/Boilerplate.Server.Api/Features/Mappers.md)

### The Three Core Methods

1. **`Project()`**: Converts `IQueryable<Entity>` → `IQueryable<DTO>` (for reading data)
2. **`Map()`**: Converts `Entity` ↔ `DTO` (for individual objects)
3. **`Patch()`**: Updates an existing object with values from another object (for updates)

### Example: CategoriesMapper

**Location**: [`src/Server/Boilerplate.Server.Api/Features/Categories/CategoriesMapper.cs`](/src/Server/Boilerplate.Server.Api/Features/Categories/CategoriesMapper.cs)

```csharp
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

### The Category Entity

For reference, here's the entity model: [`Category.cs`](/src/Server/Boilerplate.Server.Api/Features/Categories/Category.cs)

```csharp
public partial class Category
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Color { get; set; }

    public byte[] Version { get; set; } = [];

    public IList<Product> Products { get; set; } = [];
}
```

### Understanding [MapProperty]

In the mapper above, notice:

```csharp
[MapProperty(nameof(@Category.Products.Count), nameof(@CategoryDto.ProductsCount))]
```

**What it does**: Maps `Category.Products.Count` → `CategoryDto.ProductsCount`

**Important Note**: In this specific case, `[MapProperty]` is actually **not necessary** because Mapperly would automatically map `Products.Count` to `ProductsCount` by convention. It's included here to demonstrate how to explicitly map properties when needed.

---

## 4. Project() vs Map() - When to Use Each

### Project() - For Efficient Querying

**Use `Project()` when:**
- Reading data from the database
- You have an `IQueryable<Entity>` and want `IQueryable<DTO>`
- You want efficient SQL generation

**Example from [`CategoryController.cs`](/src/Server/Boilerplate.Server.Api/Features/Categories/CategoryController.cs):**

```csharp
[HttpGet, EnableQuery]
public IQueryable<CategoryDto> Get()
{
    return DbContext.Categories
        .Project(); // Extension method from CategoriesMapper
}
```

**What happens behind the scenes:**

1. EF Core translates the query to SQL
2. Only the columns needed for `CategoryDto` are selected
3. The database returns DTOs directly (no entities created in memory)
4. Extremely efficient for large datasets

**Generated SQL Example:**
```sql
SELECT 
    c.Id, 
    c.Name, 
    c.Color, 
    (SELECT COUNT(*) FROM Products WHERE CategoryId = c.Id) AS ProductsCount,
    c.Version
FROM Categories c
```

### Map() - For Individual Objects

**Use `Map()` when:**
- Converting a single entity to DTO
- Creating or updating records
- You already have the entity object in memory

**Example from the same controller:**

```csharp
[HttpPost]
public async Task<CategoryDto> Create(CategoryDto dto, CancellationToken cancellationToken)
{
    var entityToAdd = dto.Map(); // Convert DTO to Entity

    await DbContext.Categories.AddAsync(entityToAdd, cancellationToken);
    await DbContext.SaveChangesAsync(cancellationToken);

    return entityToAdd.Map(); // Convert Entity back to DTO
}
```

### Manual Projection Alternative

**Important**: Using Mapperly's `Project()` is **not mandatory**. You can perform projection manually using LINQ's `Select()`:

```csharp
// Using Mapperly's Project()
return DbContext.Categories.Project();

// Manual alternative (produces the same SQL)
return DbContext.Categories.Select(c => new CategoryDto
{
    Id = c.Id,
    Name = c.Name,
    Color = c.Color,
    ProductsCount = c.Products.Count,
    Version = c.Version
});
```

**Both approaches produce identical SQL queries**. However, Mapperly's `Project()` offers these benefits:

1. **Less Repetitive Code**: Write mapping logic once in the mapper
2. **Automatic Updates**: When you add/remove entity properties, the mapper updates automatically
3. **Consistency**: Ensures the same mapping logic across your entire application
4. **Refactoring Safety**: Renaming properties updates all mappings

---

## 5. Patch() Methods - Efficient Updates

### Server-Side Patch Usage

In the Update controller method, `Patch()` is used to update an entity with values from a DTO:

```csharp
[HttpPut]
public async Task<CategoryDto> Update(CategoryDto dto, CancellationToken cancellationToken)
{
    var entityToUpdate = await DbContext.Categories.FindAsync([dto.Id], cancellationToken)
        ?? throw new ResourceNotFoundException();

    dto.Patch(entityToUpdate); // Update entity with DTO values

    await DbContext.SaveChangesAsync(cancellationToken);

    return entityToUpdate.Map();
}
```

**Why use Patch() instead of Map()?**

- **Preserves unchanged properties**: Only updates the properties sent by the client
- **Respects Entity State**: Maintains EF Core change tracking
- **Security**: Prevents overposting attacks

### Client-Side Patch Usage

**Location**: [`src/Shared/Features/[Feature]/[Feature]Mapper.cs`](/src/Shared/Features/)

On the client side, the project defines additional `Patch()` methods in feature-specific mapper files for updating UI-bound DTOs:

```csharp
[Mapper(UseDeepCloning = true)]
public static partial class Mapper
{
    public static partial void Patch(this TodoItemDto source, TodoItemDto destination);
    public static partial void Patch(this ProductDto source, ProductDto destination);
    public static partial void Patch(this CategoryDto source, CategoryDto destination);
    // ... more patch methods
}
```

### Real-World Example: AddOrEditCategoryModal

**Location**: [`AddOrEditCategoryModal.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Pages/Categories/AddOrEditCategoryModal.razor.cs)

```csharp
public async Task ShowModal(CategoryDto categoryToShow)
{
    isOpen = true;
    categoryToShow.Patch(category); // Update local category with data
    StateHasChanged();
}
```

**Why this pattern?**

When the modal opens with a category to edit, instead of replacing the entire `category` object (which would break UI bindings), we **patch** the values into the existing object. This ensures:

1. **UI Bindings Remain Intact**: Form inputs stay connected to the same object reference
2. **Change Tracking Works**: `EditContext.IsModified()` correctly detects changes
3. **No Re-rendering Issues**: Blazor's change detection works smoothly

### Another Example: ProfileSection

**Location**: [`ProfileSection.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Pages/Settings/ProfileSection.razor.cs)

```csharp
private async Task SaveProfile()
{
    editUserDto.Patch(CurrentUser);
    
    (await userController.Update(editUserDto, CurrentCancellationToken)).Patch(CurrentUser);
}
```

---
