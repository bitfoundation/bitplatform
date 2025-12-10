# Stage 5: Localization and Multi-language Support

## Overview

The Boilerplate project includes a comprehensive localization and multi-language support system that enables your application to serve users in multiple languages. The system is built on .NET's standard `.resx` resource file infrastructure and integrates seamlessly with Blazor components, validation attributes, and server-side code.

This system provides type-safe, compile-time verified localization that works across all platforms (Web, MAUI, Windows) and supports dynamic language switching at runtime.

---

## 1. Resource Files (`.resx`) Structure

Resource files are the foundation of the localization system. They store translatable strings as key-value pairs in XML format.

### Location

Resource files are organized across two locations in the project:

#### Shared Resources (`src/Shared/Resources/`)
- **`AppStrings.resx`**: The default language resource file (English by default) for general application strings
- **`AppStrings.fa.resx`**: Persian/Farsi translations
- **`AppStrings.sv.resx`**: Swedish translations
- **`IdentityStrings.resx`**: Default identity-related strings (authentication, authorization, user management)
- **`IdentityStrings.fa.resx`**: Persian identity strings
- **`IdentityStrings.sv.resx`**: Swedish identity strings

#### Server API Resources (`src/Server/Boilerplate.Server.Api/Resources/`)
- **`EmailStrings.resx`**: Default email template strings
- **`EmailStrings.fa.resx`**: Persian email templates
- **`EmailStrings.sv.resx`**: Swedish email templates

This separation allows server-specific resources (like email templates) to be managed independently from client-facing UI strings.

### File Structure

Each `.resx` file is an XML document containing name-value pairs:

```xml
<data name="Name" xml:space="preserve">
  <value>Name</value>
</data>
<data name="Language" xml:space="preserve">
  <value>Language</value>
</data>
<data name="RequiredAttribute_ValidationError" xml:space="preserve">
  <value>The {0} field is required.</value>
</data>
```

**Key Points:**
- Each `<data>` element has a unique `name` attribute (the resource key)
- The `<value>` element contains the translated text
- `xml:space="preserve"` ensures whitespace is maintained
- Placeholders like `{0}`, `{1}` are used for parameterized strings

### Naming Convention

- **Default language**: `[FileName].resx` (e.g., `AppStrings.resx`)
- **Translated files**: `[FileName].[culture].resx` (e.g., `AppStrings.fa.resx`, `AppStrings.sv.resx`)

The culture code follows the ISO 639-1 standard:
- `fa` for Persian/Farsi
- `sv` for Swedish
- `fr` for French
- `es` for Spanish
- `de` for German
- `ar` for Arabic
- `zh` for Chinese
- etc.

### Example: Comparing Default and Translated Files

**AppStrings.resx (English - Default):**
```xml
<data name="Settings" xml:space="preserve">
  <value>Settings</value>
</data>
<data name="Language" xml:space="preserve">
  <value>Language</value>
</data>
```

**AppStrings.fa.resx (Persian):**
```xml
<data name="Settings" xml:space="preserve">
  <value>تنظیمات</value>
</data>
<data name="Language" xml:space="preserve">
  <value>زبان</value>
</data>
```

Notice how the **resource keys remain identical** across all language files - only the values change. This is crucial for the localization system to work correctly.

---

## 2. DTOs and the `[DtoResourceType]` Attribute

DTOs (Data Transfer Objects) use the `[DtoResourceType]` attribute to connect validation messages and display names to resource files. This ensures that validation errors and form labels are automatically localized based on the user's selected language.

### The Problem It Solves

Without `[DtoResourceType]`, you would need to specify the resource type on **every single validation attribute** in your DTO:

```csharp
// ❌ Repetitive approach (without DtoResourceType)
public partial class CategoryDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError), 
              ErrorMessageResourceType = typeof(AppStrings))]  // Repetitive!
    [Display(Name = nameof(AppStrings.Name), 
             ResourceType = typeof(AppStrings))]  // Repetitive!
    public string? Name { get; set; }
}
```

The `[DtoResourceType]` attribute eliminates this repetition by specifying the resource type **once** at the class level.

### Example: CategoryDto

Here's the actual `CategoryDto` from the project (`src/Shared/Dtos/Categories/CategoryDto.cs`):

```csharp
namespace Boilerplate.Shared.Dtos.Categories;

[DtoResourceType(typeof(AppStrings))]  // ✅ Specify resource type once!
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

### Key Points:

1. **`[DtoResourceType(typeof(AppStrings))]`**: Specifies which resource class contains the localized strings for this DTO
   - Applied once at the class level
   - Automatically used by all validation attributes in the class
   
2. **`ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError)`**: 
   - References the resource key using `nameof()` for compile-time safety
   - The actual error message comes from the `.resx` file based on the user's language
   
3. **`Display(Name = nameof(AppStrings.Name))`**: 
   - Specifies the display name for form labels
   - Also pulled from resource files and automatically localized

4. **Calculated Properties**: 
   - Properties like `ProductsCount` that are calculated or read-only don't need validation attributes
   - They're still part of the DTO for data transfer purposes

### Why Use `nameof()`?

Using `nameof()` provides **compile-time safety**:

**✅ With `nameof()`:**
```csharp
ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError)
```
- If you rename or delete the resource key, you'll get a **compilation error**
- Refactoring tools can safely rename resource keys
- IntelliSense provides autocomplete

**❌ Without `nameof()` (string literal):**
```csharp
ErrorMessage = "RequiredAttribute_ValidationError"  // ❌ Dangerous!
```
- If you rename or delete the resource key, you'll get a **runtime error**
- Typos won't be caught until runtime
- No IntelliSense support

### Resource Key Examples

Here are the corresponding entries from `AppStrings.resx`:

```xml
<!-- Validation Error Messages -->
<data name="RequiredAttribute_ValidationError" xml:space="preserve">
  <value>The {0} field is required.</value>
</data>

<data name="MaxLengthAttribute_InvalidMaxLength" xml:space="preserve">
  <value>MaxLengthAttribute must have a Length value that is greater than zero...</value>
</data>

<!-- Field Names -->
<data name="Name" xml:space="preserve">
  <value>Name</value>
</data>

<data name="Color" xml:space="preserve">
  <value>Color</value>
</data>
```

And their Persian translations in `AppStrings.fa.resx`:

```xml
<!-- Validation Error Messages -->
<data name="RequiredAttribute_ValidationError" xml:space="preserve">
  <value>مقدار {0} الزامی است.</value>
</data>

<!-- Field Names -->
<data name="Name" xml:space="preserve">
  <value>نام</value>
</data>

<data name="Color" xml:space="preserve">
  <value>رنگ</value>
</data>
```

Notice how the **placeholder `{0}`** is preserved in both languages - it will be replaced with the field name at runtime.

---

## 3. AppDataAnnotationsValidator

To make `[DtoResourceType]` work in Blazor EditForms, you **must** use the custom `AppDataAnnotationsValidator` component instead of the standard `DataAnnotationsValidator`.

### Why is it Needed?

The standard `DataAnnotationsValidator` from Blazor **does not understand** the `[DtoResourceType]` attribute. It's designed for the traditional approach where you specify `ErrorMessageResourceType` on every validation attribute.

The `AppDataAnnotationsValidator` (located in `src/Client/Boilerplate.Client.Core/Components/AppDataAnnotationsValidator.cs`) extends the validation system to:

1. **Recognize `[DtoResourceType]`**: Reads the attribute from the DTO class
2. **Resolve localized messages**: Automatically pulls error messages from the correct resource file
3. **Display server-side validation errors**: Via the `DisplayErrors()` method for `ResourceValidationException`

**Why Server-Side Validation Error Display is Important:**

While Blazor's `EditForm` shows client-side validation errors based on DataAnnotations attributes (like `[Required]`, `[MaxLength]`), there are scenarios where you need to display **server-side validation errors** that can only be determined by the server. For example:

- **Duplicate names**: Checking if a category name already exists in the database (e.g., "Category with name 'Electronics' already exists")

In these scenarios, the server throws a `ResourceValidationException` with field-specific error messages. The `AppDataAnnotationsValidator.DisplayErrors()` method maps these server-side errors to the corresponding form fields, displaying them inline next to the relevant input controls - just like client-side validation errors.

### Usage Example

Here's an actual example from the project (`src/Client/Boilerplate.Client.Core/Components/Pages/Categories/AddOrEditCategoryModal.razor`):

```xml
<EditForm @ref="editForm" Model="category" OnValidSubmit="WrapHandled(Save)" novalidate>
    <AppDataAnnotationsValidator @ref="validatorRef" />

    <BitStack Gap="0.25rem">
        <BitTextField @bind-Value="category.Name"
                      Label="@Localizer[nameof(AppStrings.Name)]" />
        <ValidationMessage For="() => category.Name" />
        
        <BitLabel For="catColorInput">@Localizer[nameof(AppStrings.Color)]</BitLabel>
        <ValidationMessage For="() => category.Color" />
        
        <BitButton IsLoading=isSaving ButtonType="BitButtonType.Submit">
            @Localizer[nameof(AppStrings.Save)]
        </BitButton>
    </BitStack>
</EditForm>
```

### Code-Behind Integration

In the code-behind file (`AddOrEditCategoryModal.razor.cs`), you can reference the validator to display server-side validation errors:

```csharp
private AppDataAnnotationsValidator validatorRef = default!;

private async Task Save()
{
    if (isSaving) return;
    isSaving = true;

    try
    {
        if (category.Id == default)
        {
            await categoryController.Create(category, CurrentCancellationToken);
        }
        else
        {
            await categoryController.Update(category, CurrentCancellationToken);
        }
    }
    catch (ResourceValidationException e)
    {
        // Display server-side validation errors in the form
        validatorRef.DisplayErrors(e);
    }
    finally
    {
        isSaving = false;
    }
}
```

---

## 4. Using `IStringLocalizer<T>` in Code

The `IStringLocalizer<T>` interface is the primary way to access localized strings programmatically in C# code. It allows you to retrieve translated strings based on resource keys and supports parameterized messages.

### In Components and Pages

All components that inherit from `AppComponentBase` or pages that inherit from `AppPageBase` automatically have access to the `Localizer` property.

**Base class location**: `src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs`

```csharp
public partial class AppComponentBase : OwningComponentBase, IAsyncDisposable
{
    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
    // ... other injected services
}
```

> **Note:** `AppComponentBase` inherits from `OwningComponentBase` (not `ComponentBase`), which provides a scoped service container for each component instance. This enables proper service lifetime management and automatic disposal of scoped services when the component is disposed.

This means **every component in the project** automatically has the `Localizer` property available without needing to inject it manually.

#### Usage in Razor Files

**Example from `AddOrEditCategoryModal.razor`:**

```xml
<BitText Typography="BitTypography.H5">
    @if (category.Id == default)
    {
        @Localizer[nameof(AppStrings.AddCategory)]
    }
    else
    {
        @Localizer[nameof(AppStrings.EditCategory)]
    }
</BitText>

<BitTextField @bind-Value="category.Name"
              Label="@Localizer[nameof(AppStrings.Name)]" />

<BitButton ButtonType="BitButtonType.Submit">
    @Localizer[nameof(AppStrings.Save)]
</BitButton>
```

### In Controllers (Server-Side)

All API controllers that inherit from `AppControllerBase` have automatic access to the `Localizer` property.

**Base class location**: `src/Server/Boilerplate.Server.Api/Controllers/AppControllerBase.cs`

```csharp
public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected AppDbContext DbContext = default!;
    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
    [AutoInject] protected ServerApiSettings AppSettings = default!;
}
```

#### Usage in Controllers

**Example from `CategoryController.cs`:**

```csharp
[HttpGet("{id}")]
public async Task<CategoryDto> Get(Guid id, CancellationToken cancellationToken)
{
    var dto = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    if (dto is null)
        throw new ResourceNotFoundException(Localizer[nameof(AppStrings.CategoryCouldNotBeFound)]);

    return dto;
}

[HttpDelete("{id}/{version}")]
public async Task Delete(Guid id, string version, CancellationToken cancellationToken)
{
    if (await DbContext.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
    {
        throw new BadRequestException(Localizer[nameof(AppStrings.CategoryNotEmpty)]);
    }
    
    // Delete logic...
}

private async Task Validate(Category category, CancellationToken cancellationToken)
{
    var entry = DbContext.Entry(category);
    
    if ((entry.State is EntityState.Added || entry.Property(c => c.Name).IsModified)
        && await DbContext.Categories.AnyAsync(p => p.Name == category.Name, cancellationToken))
    {
        // Validation error with parameter
        throw new ResourceValidationException(
            (nameof(CategoryDto.Name), 
             [Localizer[nameof(AppStrings.DuplicateCategoryName), category.Name!]])
        );
    }
}
```

**Key Points:**
- Controllers automatically get the user's culture from the HTTP request
- Error messages thrown from controllers are automatically localized for the client
- The client receives error messages in their selected language

### In Services

For services that don't inherit from a base class with `Localizer`, you can inject `IStringLocalizer<T>` directly using the `[AutoInject]` attribute.

**Example from exception handlers and services:**

```csharp
public partial class ExceptionDelegatingHandler
{
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // ... request handling
        }
        catch (HttpRequestException exp) when (exp.StatusCode is HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)]);
        }
        catch (HttpRequestException exp) when (exp.StatusCode is HttpStatusCode.Forbidden)
        {
            throw new ForbiddenException(localizer[nameof(AppStrings.ForbiddenException)]);
        }
    }
}
```

### Localization with Parameters

Resource strings can include placeholders like `{0}`, `{1}`, etc., which are replaced at runtime.

**Resource file example (`AppStrings.resx`):**
```xml
<data name="DuplicateCategoryName" xml:space="preserve">
  <value>Category with name '{0}' already exists.</value>
</data>

<data name="PrivilegedDeviceLimitMessage" xml:space="preserve">
  <value>From {0} devices allowed for full features, you've used {1}.
After reaching {0}, extra sign-ins will have reduced functions.</value>
</data>

<data name="UserLockedOut" xml:space="preserve">
  <value>Your account has been locked. Try again in {0}.</value>
</data>
```

**Usage with parameters:**

```csharp
// Single parameter
var message = Localizer[nameof(AppStrings.DuplicateCategoryName), categoryName];
// Result: "Category with name 'Electronics' already exists."

// Multiple parameters
var message = Localizer[nameof(AppStrings.PrivilegedDeviceLimitMessage), maxDevices, usedDevices];
// Result: "From 5 devices allowed for full features, you've used 3..."

// Complex parameter (humanized time)
throw new BadRequestException(
    Localizer[nameof(AppStrings.UserLockedOut), 
              tryAgainIn.Humanize(culture: CultureInfo.CurrentUICulture)]
);
// Result: "Your account has been locked. Try again in 5 minutes."
```

### Best Practices for IStringLocalizer

1. **Always use `nameof()`** for resource keys:
   ```csharp
   ✅ Localizer[nameof(AppStrings.Save)]
   ❌ Localizer["Save"]  // No compile-time safety!
   ```

2. **Don't concatenate localized strings** - use parameterized messages instead:
   ```csharp
   ❌ var msg = Localizer[nameof(AppStrings.Hello)] + " " + userName;
   ✅ var msg = Localizer[nameof(AppStrings.HelloUser), userName];
   ```
   
3. **Preserve placeholder order** - some languages may need different word order:
   ```xml
   <!-- English -->
   <value>Delete {0} from {1}?</value>
   
   <!-- Some languages might need -->
   <value>{1} থেকে {0} মুছবেন?</value>  <!-- Bengali: different word order -->
   ```

4. **Use descriptive resource keys** that include context:
   ```csharp
   ✅ Localizer[nameof(AppStrings.CategoryDeleteConfirmation)]
   ❌ Localizer[nameof(AppStrings.Confirm)]  // Too generic
   ```

---

## 5. The `bit-resx` Tool - Automated Translation

The `bit-resx` tool (also known as `Bit.ResxTranslator`) is a .NET global tool that automates the translation of `.resx` files using Large Language Models (LLMs) like OpenAI or Azure OpenAI. This dramatically reduces the manual effort required to maintain translations across multiple languages.

### What Does It Do?

The tool performs the following tasks automatically:

1. **Compares resource files**: Analyzes the default language `.resx` file against translated versions
2. **Identifies missing translations**: Finds resource keys that exist in the default file but are missing in target language files
3. **AI-powered translation**: Uses LLMs (OpenAI/Azure OpenAI) to automatically translate missing entries
4. **Preserves existing translations**: Never overwrites manual translations - only adds missing ones
5. **Maintains placeholder formats**: Correctly preserves placeholders like `{0}`, `{1}` in translated text
6. **Updates files**: Automatically inserts new translations into the appropriate `.resx` files while maintaining XML structure
7. **Creates new language files**: Can generate new target language `.resx` files if they don't exist

### Installation

Install `bit-resx` as a .NET global tool:

```bash
dotnet tool install --global Bit.ResxTranslator
```

#### `Bit.ResxTranslator.json` Configuration Options Explained

**`DefaultLanguage`**: The culture code for your primary language (usually `"en"` for English)

**`SupportedLanguages`**: Array of ISO 639-1 language codes to translate into:
- `"fa"` - Persian/Farsi
- `"sv"` - Swedish
- `"fr"` - French
- `"de"` - German
- `"es"` - Spanish
- `"ar"` - Arabic
- `"zh"` - Chinese
- `"hi"` - Hindi
- etc.

**`ResxPaths`**: Glob patterns to locate your `.resx` files:
```json
"ResxPaths": [
    "/src/**/*.resx",                    // All .resx files in src
]
```

**`ChatOptions`**: LLM configuration:
- `"Temperature": "0"` - Deterministic translations (recommended for consistency)

**`OpenAI` / `AzureOpenAI`**: LLM provider configuration:
- `Model` - The model to use (e.g., `gpt-4.1-mini`)
- `Endpoint` - API endpoint URL
- `ApiKey` - Your API key (see security note below)

#### Security Note: API Keys

**❌ Never commit API keys to source control!**

Instead, use environment variables:

```json
{
    "OpenAI": {
        "ApiKey": null  // Leave as null in config file
    }
}
```

Then set the environment variable:

**Windows (PowerShell):**
```powershell
$env:OpenAI__ApiKey = "your-api-key-here"
```

**Windows (CMD):**
```cmd
set OpenAI__ApiKey=your-api-key-here
```

**Linux/macOS:**
```bash
export OpenAI__ApiKey="your-api-key-here"
```

**For Azure OpenAI:**
```powershell
$env:AzureOpenAI__ApiKey = "your-azure-key"
```

The tool automatically reads environment variables using the pattern: `{Section}__{Property}`

### Usage

Run the translation command from your project root (where `Bit.ResxTranslator.json` is located):

```bash
bit-resx-translate
```

## Philosophy of bit-resx Translator in CD Pipelines

While the `bit-resx` translator is a powerful tool for automatic translation, its core philosophy is to streamline localization in CI/CD pipelines. You do **not** need to manually translate or commit every language file for all supported languages. Instead, you can:

- Only add or manually translate the keys and languages that are important to your project.
- Leave less important languages (or less critical keys) untranslated or even omit them from the source code.
- During the CD (Continuous Deployment) process, `bit-resx` will automatically fill in any missing translations for all supported languages before publishing.

For example, in this project, the Swedish language file is much smaller than the English file and only contains keys that have been manually reviewed or improved. If some automatic translations are not satisfactory, you can simply add or override those specific keys manually. On the next CD run, only the missing keys will be auto-translated, and your manual translations will be preserved.

This approach keeps your source code clean and focused, while ensuring that all languages are fully translated at deployment time.

That's why `bit-resx` tool is added to the project CD pipelines. Here's how it's used in this project's GitHub Actions:

**Example from `.github/workflows/cd.yml`:**

```yaml
- name: Install Bit.ResxTranslator
  run: dotnet tool install --global Bit.ResxTranslator --prerelease

- name: Translate Resources
  env:
    OpenAI__ApiKey: ${{ secrets.OPENAI_API_KEY }}
  run: bit-resx-translate
```

### When to Run the Tool

**During Development (Optional):**
- After adding new resource keys to the default `.resx` file
- Before committing changes that include new translatable strings
- When adding support for a new language

**In CD pipeline:**
- As part of publish/deploy during CD

---