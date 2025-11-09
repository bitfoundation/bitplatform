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

    public byte[] ConcurrencyStamp { get; set; } = [];
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
3. **Support dynamic language switching**: Updates validation messages when the user changes their language
4. **Provide compile-time safety**: Works seamlessly with `nameof()` for resource keys
5. **Display server-side validation errors**: Via the `DisplayErrors()` method for `ResourceValidationException`

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
        
        <BitStack Horizontal>
            <BitButton OnClick="(() => isOpen = false)">
                @Localizer[nameof(AppStrings.Cancel)]
            </BitButton>

            <BitButton IsLoading=isSaving ButtonType="BitButtonType.Submit">
                @Localizer[nameof(AppStrings.Save)]
            </BitButton>
        </BitStack>
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

### Key Features

#### 1. Automatic Client-Side Validation
When the user fills out the form, `AppDataAnnotationsValidator` automatically validates against the rules defined in the DTO:
- Required fields
- Maximum length
- Email format
- Phone format
- Custom validation rules

All validation messages are **automatically localized** based on the user's selected language.

#### 2. Server-Side Validation Error Display
When the server throws a `ResourceValidationException` (e.g., duplicate category name), you can display it in the form using:

```csharp
validatorRef.DisplayErrors(exception);
```

This maps server-side errors to the appropriate form fields, showing validation messages exactly where they belong.

#### 3. Clear Errors
You can programmatically clear all validation errors:

```csharp
validatorRef.ClearErrors();
```

### Comparison

**❌ Without AppDataAnnotationsValidator:**
```xml
<EditForm Model="category">
    <DataAnnotationsValidator />  <!-- Won't work with [DtoResourceType]! -->
    <!-- Validation messages will be in English only -->
</EditForm>
```

**✅ With AppDataAnnotationsValidator:**
```xml
<EditForm Model="category">
    <AppDataAnnotationsValidator @ref="validatorRef" />  <!-- ✅ Full localization support! -->
    <!-- Validation messages automatically adapt to user's language -->
</EditForm>
```

### Important Notes

- **Always use `AppDataAnnotationsValidator`** in Blazor EditForms throughout the project
- **Never use the standard `DataAnnotationsValidator`** - it doesn't support `[DtoResourceType]`
- Store a reference (`@ref`) to the validator if you need to display server-side errors or clear errors programmatically
- Works seamlessly with all standard validation attributes (`Required`, `MaxLength`, `EmailAddress`, `Phone`, `Range`, `Compare`, etc.)

---

## 4. Using `IStringLocalizer<T>` in Code

The `IStringLocalizer<T>` interface is the primary way to access localized strings programmatically in C# code. It allows you to retrieve translated strings based on resource keys and supports parameterized messages.

### In Components and Pages

All components that inherit from `AppComponentBase` or pages that inherit from `AppPageBase` automatically have access to the `Localizer` property.

**Base class location**: `src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs`

```csharp
public partial class AppComponentBase : ComponentBase
{
    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
    // ... other injected services
}
```

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

**More examples from `NotAuthorizedPage.razor`:**

```xml
<AppPageData Title="@Localizer[nameof(AppStrings.NotAuthorizedPageTitle)]" />

<BitText Typography="BitTypography.H4">
    @Localizer[nameof(AppStrings.ForbiddenException)]
</BitText>

<BitButton OnClick="WrapHandled(SignOut)">
    @Localizer[nameof(AppStrings.SignInAsDifferentUser)]
</BitButton>
```

**Key Pattern:**
```xml
@Localizer[nameof(AppStrings.ResourceKey)]
```

- `@` - Razor syntax to output C# expressions
- `Localizer[]` - Indexer to get localized string
- `nameof(AppStrings.ResourceKey)` - Type-safe resource key reference

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

[HttpDelete("{id}/{concurrencyStamp}")]
public async Task Delete(Guid id, string concurrencyStamp, CancellationToken cancellationToken)
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

**Shared exception handler example:**

```csharp
public partial class SharedExceptionHandler
{
    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
    
    public virtual string GetMessage(Exception exception)
    {
        return Localizer[nameof(AppStrings.UnknownException)];
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

**Persian translation (`AppStrings.fa.resx`):**
```xml
<data name="DuplicateCategoryName" xml:space="preserve">
  <value>دسته‌بندی با نام '{0}' از قبل وجود دارد.</value>
</data>

<data name="PrivilegedDeviceLimitMessage" xml:space="preserve">
  <value>از مجموع {0} دستگاه با دسترسی کامل، شما {1} دستگاه را استفاده کرده‌اید.
پس از رسیدن به {0} دستگاه، دستگاه‌های اضافی با امکانات محدود خواهد بود.</value>
</data>

<data name="UserLockedOut" xml:space="preserve">
  <value>حساب کاربری شما قفل شده است. {0} دیگر دوباره امتحان کنید.</value>
</data>
```

**Usage with parameters:**

```csharp
// Single parameter
var message = Localizer[nameof(AppStrings.DuplicateCategoryName), categoryName];
// Result (English): "Category with name 'Electronics' already exists."
// Result (Persian): "دسته‌بندی با نام 'Electronics' از قبل وجود دارد."

// Multiple parameters
var message = Localizer[nameof(AppStrings.PrivilegedDeviceLimitMessage), maxDevices, usedDevices];
// Result (English): "From 5 devices allowed for full features, you've used 3..."
// Result (Persian): "از مجموع 5 دستگاه با دسترسی کامل، شما 3 دستگاه را استفاده کرده‌اید..."

// Complex parameter (humanized time)
throw new BadRequestException(
    Localizer[nameof(AppStrings.UserLockedOut), 
              tryAgainIn.Humanize(culture: CultureInfo.CurrentUICulture)]
);
// Result (English): "Your account has been locked. Try again in 5 minutes."
// Result (Persian): "حساب کاربری شما قفل شده است. 5 دقیقه دیگر دوباره امتحان کنید."
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

### Key Features

- **LLM-Powered Translation**: Leverages state-of-the-art language models for high-quality translations
- **Preserves Existing Translations**: Only translates missing entries - never overwrites manual translations
- **Automatic File Generation**: Creates new language files automatically based on your configuration
- **Flexible Configuration**: Supports glob patterns for file discovery and multiple language targets
- **Environment Variable Support**: Secure API key management through environment variables
- **CI/CD Ready**: Designed for integration into automated build pipelines
- **Multiple LLM Providers**: Works with both OpenAI and Azure OpenAI

### Installation

Install `bit-resx` as a .NET global tool:

```bash
dotnet tool install --global Bit.ResxTranslator --prerelease
```

**Update to latest version:**
```bash
dotnet tool update --global Bit.ResxTranslator --prerelease
```

**Verify installation:**
```bash
bit-resx-translate --help
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
    "/src/Shared/Resources/*.resx",      // Only Shared resources
    "/src/Server/**/Resources/*.resx"    // Only Server resources
]
```

**`ChatOptions`**: LLM configuration:
- `"Temperature": "0"` - Deterministic translations (recommended for consistency)
- Higher values (e.g., `"0.7"`) produce more creative but less consistent translations

**`OpenAI` / `AzureOpenAI`**: LLM provider configuration:
- `Model` - The model to use (e.g., `"gpt-4o-mini"`, `"gpt-4"`, `"gpt-4-turbo"`)
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

### How It Works - Step by Step

1. **File Discovery**: 
   - Scans your project for `.resx` files matching the configured glob patterns
   - Groups files by base name (e.g., `AppStrings.resx`, `AppStrings.fa.resx`, `AppStrings.sv.resx`)

2. **Comparison**:
   - For each supported language, loads both the default and target language `.resx` files
   - Deserializes them into dictionaries of key-value pairs
   - Identifies missing keys in the target language file

3. **Translation Request**:
   - If missing translations are found, sends them to the configured LLM
   - Provides context to the AI:
     - "Act as a professional translator"
     - "Translate to {target language}"
     - "Preserve all placeholders like {0}, {1}"
     - "Maintain the same tone and technical accuracy"

4. **Translation Processing**:
   - The LLM translates all missing entries in a single batch
   - Returns translations in a structured format
   - The tool validates that placeholders are preserved

5. **File Update**:
   - Inserts the new translations into the target `.resx` file
   - Maintains XML structure and formatting
   - Preserves all existing translations and comments

6. **Repeat**:
   - Processes all language files for all `.resx` file groups

### Example Output

```
🔍 Discovering .resx files...
Found 3 .resx file groups: AppStrings, IdentityStrings, EmailStrings

📋 Processing: AppStrings.resx
  ├─ Language: fa (Persian)
  │  └─ Found 5 missing translations
  │  └─ ✅ Translated and saved
  ├─ Language: sv (Swedish)  
  │  └─ Found 3 missing translations
  │  └─ ✅ Translated and saved
  └─ Language: fr (French)
     └─ ✅ All translations up to date

📋 Processing: IdentityStrings.resx
  ├─ Language: fa (Persian)
  │  └─ ✅ All translations up to date
  ...

✨ Translation complete! Updated 2 language files.
```

### CI/CD Integration

The `bit-resx` tool is designed for CI/CD pipelines. Here's how it's used in this project's GitHub Actions:

**Example from `.github/workflows/cd.yml`:**

```yaml
- name: Install Bit.ResxTranslator
  run: dotnet tool install --global Bit.ResxTranslator --prerelease

- name: Translate Resources
  env:
    OpenAI__ApiKey: ${{ secrets.OPENAI_API_KEY }}
  run: bit-resx-translate
```

**Key Points:**
- Install the tool in your CI environment
- Pass API keys securely via CI/CD secrets
- Run before building to ensure all translations are current

### When to Run the Tool

**During Development:**
- After adding new resource keys to the default `.resx` file
- Before committing changes that include new translatable strings
- When adding support for a new language

**In CI/CD:**
- As part of every build (recommended)
- Before creating release builds
- After merging PRs that modify `.resx` files

**Manual Review:**
- Always review AI-generated translations for accuracy
- Specialized terminology may need manual correction
- Cultural context may require adjustments

### Advanced Usage

#### Translate Specific Languages Only

Temporarily modify your config to translate only specific languages:

```json
{
    "SupportedLanguages": [ "fr" ]  // Only translate French
}
```

#### Use Different Models

For higher quality (but slower/more expensive) translations:

```json
{
    "OpenAI": {
        "Model": "gpt-4"  // Instead of gpt-4o-mini
    }
}
```

#### Multiple .resx File Groups

Use multiple glob patterns to organize translations:

```json
{
    "ResxPaths": [
        "/src/Shared/Resources/*.resx",           // Shared UI strings
        "/src/Server/Boilerplate.Server.Api/Resources/*.resx"  // Email templates
    ]
}
```

### Limitations and Best Practices

**✅ Good for:**
- General UI strings and messages
- Standard validation messages
- Common terminology
- Large volumes of text requiring translation

**⚠️ May need manual review:**
- Domain-specific terminology
- Brand names and product names
- Technical jargon specific to your application
- Cultural idioms and expressions
- Legal text and terms of service

**Best Practices:**
1. **Start with high-quality English**: Clear, concise default text produces better translations
2. **Use descriptive resource keys**: Helps the AI understand context
3. **Review AI translations**: Especially for customer-facing text
4. **Maintain a glossary**: For consistent translation of key terms
5. **Test with native speakers**: When possible, validate translations with native speakers
6. **Run frequently**: Small, frequent translation updates are easier to review than large batches

### Troubleshooting

**Problem: API rate limits**
- Solution: Reduce batch size, add delays, or upgrade your API plan

**Problem: Placeholder mismatches**
- Solution: The tool validates placeholders automatically - if it fails, check your default text

**Problem: Inconsistent translations**
- Solution: Use `"Temperature": "0"` for deterministic results

**Problem: Missing translations after running**
- Solution: Check that your glob patterns match your `.resx` file locations

### Related Documentation

- Official GitHub Repository: https://github.com/bitfoundation/bitplatform/tree/develop/src/ResxTranslator
- OpenAI API Documentation: https://platform.openai.com/docs
- Azure OpenAI Documentation: https://learn.microsoft.com/azure/ai-services/openai/

---

## 6. Language Selection and Switching

The Boilerplate project provides a seamless language selection experience that allows users to choose their preferred language, which is then applied consistently across the entire application.

### How Language Selection Works

1. **User selects language**: From the Settings page, users can choose from available languages
2. **Culture is saved**: The selected culture code is stored in local storage (browser/app storage)
3. **Application reloads**: The app applies the new culture and updates all localized text
4. **Persistence**: The selected language persists across sessions and browser/app restarts

### Supported Languages

By default, the Boilerplate template supports these languages:

| Language | Culture Code | Resource Files |
|----------|--------------|----------------|
| **English** (Default) | `en` | `*.resx` |
| **Persian/Farsi** | `fa` | `*.fa.resx` |
| **Swedish** | `sv` | `*.sv.resx` |

Additional languages configured in `Bit.ResxTranslator.json`:
- Dutch (`nl`)
- Hindi (`hi`)
- Chinese (`zh`)
- Spanish (`es`)
- French (`fr`)
- Arabic (`ar`)
- German (`de`)

### Implementation Details

The culture selection system is implemented through several components:

#### 1. Culture Storage
The selected culture is stored using `IStorageService`:
- **Web**: Browser's `localStorage`
- **MAUI**: Platform-specific preferences
- **Windows**: Application settings

#### 2. Culture Application
When the application starts, it:
- Retrieves the stored culture preference
- Sets `CultureInfo.CurrentCulture` and `CultureInfo.CurrentUICulture`
- All `IStringLocalizer` calls automatically use the current culture
- All `.resx` files are accessed based on the current culture

#### 3. Settings Page Integration
Users can change their language through the Settings page, which typically includes:
- A dropdown or list of available languages
- Visual indicators (flags, language names in native script)
- Immediate application of the selected language

### Adding a New Language

To add support for a new language (for example, French `fr`):

#### Step 1: Create Resource Files

Create new `.resx` files for each resource type:
- `src/Shared/Resources/AppStrings.fr.resx`
- `src/Shared/Resources/IdentityStrings.fr.resx`
- `src/Server/Boilerplate.Server.Api/Resources/EmailStrings.fr.resx`

#### Step 2: Add Translations

You have two options:

**Option A: Use bit-resx tool (Recommended)**
1. Update `Bit.ResxTranslator.json`:
   ```json
   {
       "SupportedLanguages": ["fa", "sv", "fr"]  // Add "fr"
   }
   ```
2. Run the tool:
   ```bash
   bit-resx-translate
   ```
3. Review the AI-generated translations

**Option B: Manual Translation**
1. Copy the default `.resx` file
2. Rename it with the culture code (e.g., `AppStrings.fr.resx`)
3. Manually translate all `<value>` elements
4. Keep all resource keys unchanged

#### Step 3: Update Culture Configuration

If needed, update culture-related configuration files:

**For MAUI applications (`MainActivity.cs` or similar):**
```csharp
// Add French to DataPathPrefixes if using offline database
DataPathPrefixes = new[] { "en", "fa", "sv", "fr" }
```

**For culture selection UI:**
Add the new language option to your settings page dropdown/list.

#### Step 4: Test

Test the application with the new language:
- Verify all strings are translated correctly
- Check UI layout (some languages require more/less space)
- Test RTL languages (Arabic, Persian, Hebrew) for proper text direction
- Validate that validation messages display correctly
- Ensure email templates render properly

### RTL (Right-to-Left) Language Support

For RTL languages like Persian, Arabic, and Hebrew:

1. **Automatic Text Direction**: The application should automatically detect RTL languages and adjust text direction
2. **CSS Considerations**: Use logical properties instead of directional:
   ```scss
   // ✅ Good - works in both LTR and RTL
   margin-inline-start: 1rem;
   padding-inline-end: 2rem;
   
   // ❌ Bad - breaks in RTL
   margin-left: 1rem;
   padding-right: 2rem;
   ```
3. **Icon Mirroring**: Some icons should mirror in RTL (arrows, chevrons)
4. **Number Formatting**: Numbers may display differently in some cultures

### Testing Different Languages

**During Development:**
1. Change your browser/app language settings
2. Or set culture programmatically for testing:
   ```csharp
   CultureInfo.CurrentCulture = new CultureInfo("fr");
   CultureInfo.CurrentUICulture = new CultureInfo("fr");
   ```

**Testing Checklist:**
- [ ] All UI strings are translated
- [ ] Validation messages appear in the correct language
- [ ] Email templates use the correct language
- [ ] Date and time formats match the culture
- [ ] Number and currency formats are correct
- [ ] Text doesn't overflow UI components
- [ ] RTL languages display correctly (if applicable)
- [ ] Language persists after app restart

---

## 7. Best Practices

Follow these best practices to maintain a high-quality localization system:

### 1. Always Use Resource Keys

Never hardcode user-facing text in your code or markup.

❌ **Bad**:
```xml
<h1>Welcome to the application</h1>
<BitButton>Save</BitButton>
```

✅ **Good**:
```xml
<h1>@Localizer[nameof(AppStrings.WelcomeMessage)]</h1>
<BitButton>@Localizer[nameof(AppStrings.Save)]</BitButton>
```

### 2. Use `nameof()` for Compile-Time Safety

Always use `nameof()` when referencing resource keys to catch errors at compile time.

❌ **Bad**:
```csharp
Localizer["Name"]  // ❌ Runtime error if key doesn't exist or is renamed
ErrorMessage = "RequiredAttribute_ValidationError"  // ❌ Typos not caught
```

✅ **Good**:
```csharp
Localizer[nameof(AppStrings.Name)]  // ✅ Compile-time error if key doesn't exist
ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError)  // ✅ Refactoring-safe
```

### 3. Use AppDataAnnotationsValidator in Forms

Always use `AppDataAnnotationsValidator` instead of the standard `DataAnnotationsValidator` in Blazor EditForms.

❌ **Bad**:
```xml
<EditForm Model="model">
    <DataAnnotationsValidator />  <!-- Won't work with [DtoResourceType]! -->
    ...
</EditForm>
```

✅ **Good**:
```xml
<EditForm Model="model">
    <AppDataAnnotationsValidator />  <!-- ✅ Full localization support -->
    ...
</EditForm>
```

### 4. Organize Resource Files by Domain

Keep resource files organized by functional area for easier maintenance:

- **`AppStrings.resx`**: General application strings (UI labels, buttons, common messages)
- **`IdentityStrings.resx`**: Authentication and authorization strings (login, signup, passwords)
- **`EmailStrings.resx`**: Email template strings (notifications, confirmations, alerts)

This separation:
- Makes it easier to find the right resource file
- Allows different teams to work on different resource files
- Reduces merge conflicts
- Improves maintainability

### 5. Include Context in Resource Key Names

Use descriptive, context-rich key names that make the purpose clear.

✅ **Good** (self-documenting):
- `SignInPageTitle`
- `ProfileUpdatedSuccessfullyMessage`
- `DeleteAccountConfirmationPrompt`
- `CategoryNotEmptyError`
- `EmailConfirmationSentMessage`

❌ **Bad** (too generic):
- `Title` (Which title?)
- `Success` (Success for what operation?)
- `Prompt` (What kind of prompt?)
- `Error` (What error?)
- `Message` (What message?)

**Benefits of descriptive keys:**
- Easier to find the right key when coding
- Translators understand context better
- Reduces duplicate keys
- Makes code more self-documenting

### 6. Use Parameterized Messages, Not Concatenation

Never concatenate localized strings - different languages have different word orders.

❌ **Bad**:
```csharp
var msg = Localizer[nameof(AppStrings.Delete)] + " " + 
          itemName + " " + 
          Localizer[nameof(AppStrings.From)] + " " + 
          categoryName + "?";
```

✅ **Good**:
```csharp
// AppStrings.resx
// <value>Delete {0} from {1}?</value>

var msg = Localizer[nameof(AppStrings.DeleteItemFromCategory), itemName, categoryName];
```

**Why this matters:**
```
English: "Delete {0} from {1}?"
Some languages: "{1} থেকে {0} মুছবেন?"  (Different word order in Bengali)
```

### 7. Test with Different Languages

Always test your application with multiple languages to ensure:

**UI Layout:**
- Text doesn't overflow components (German text is often 30% longer than English)
- Buttons are wide enough for translated text
- Forms remain usable with longer labels

**RTL Support:**
- Right-to-left languages (Persian, Arabic, Hebrew) display correctly
- Icons and images are mirrored where appropriate
- Layout doesn't break with reversed text direction

**Functionality:**
- Date/time pickers use correct formats
- Number formatting matches culture expectations
- Currency symbols display correctly
- Validation messages are clear and grammatically correct

### 8. Keep Resource Files in Sync

**Use bit-resx tool regularly:**
```bash
bit-resx-translate
```

This ensures:
- All language files have the same resource keys
- New keys are automatically translated
- No missing translations in any language

**Before committing:**
- Run `bit-resx-translate` after adding new resource keys
- Review AI-generated translations for critical text
- Commit all language files together

### 9. Provide Translation Context

When adding resource strings that might be ambiguous, add XML comments:

```xml
<!-- Button text for confirming deletion -->
<data name="Delete" xml:space="preserve">
  <value>Delete</value>
</data>

<!-- Noun: the delete operation -->
<data name="DeleteOperation" xml:space="preserve">
  <value>Delete</value>
</data>
```

Comments help:
- Translators understand context
- Other developers understand usage
- AI translation tools provide better translations

### 10. Handle Pluralization Correctly

Different languages have different pluralization rules. For complex scenarios:

```csharp
// Simple approach (for languages with simple plural rules)
var msg = count == 1 
    ? Localizer[nameof(AppStrings.OneItemSelected)] 
    : Localizer[nameof(AppStrings.MultipleItemsSelected), count];

// For languages with complex plural rules, consider using ICU Message Format or similar
```

### 11. Validate Placeholder Consistency

When translating strings with placeholders, ensure they're preserved:

✅ **Correct**:
```xml
<!-- English -->
<value>Category {0} contains {1} products</value>

<!-- Persian -->
<value>دسته‌بندی {0} شامل {1} محصول است</value>  <!-- Placeholders preserved -->
```

❌ **Incorrect**:
```xml
<!-- Persian -->
<value>دسته‌بندی شامل محصول است</value>  <!-- ❌ Placeholders missing! -->
```

The `bit-resx` tool automatically validates this, but manual translations should be checked carefully.

---

## 8. Summary

The Boilerplate's localization system provides a comprehensive, production-ready solution for building multilingual applications:

### ✅ Key Features

**Centralized Translation Management**
- All translatable strings in `.resx` files
- Organized by domain (App, Identity, Email)
- Easy to find and update

**Type-Safe Access**
- Using `nameof()` for compile-time safety
- IntelliSense support throughout
- Refactoring-safe resource key references

**Automatic Validation Localization**
- Through `[DtoResourceType]` attribute
- Using `AppDataAnnotationsValidator` component
- Works seamlessly with all validation attributes

**AI-Powered Translation**
- Using the `bit-resx` tool
- Integrates with OpenAI and Azure OpenAI
- Dramatically reduces manual translation effort

**Seamless Integration**
- Works across Blazor components, controllers, and services
- Inherited `Localizer` property in base classes
- No boilerplate code required

**Runtime Language Switching**
- Users can change language without recompiling
- Culture preference persists across sessions
- Instant application of new language

**Cross-Platform Support**
- Works identically on Web, MAUI, and Windows
- RTL language support built-in
- Platform-specific culture handling

**Production-Ready**
- Used in real-world applications
- CI/CD integration examples
- Best practices baked in

### 🎯 Getting Started Checklist

To effectively use the localization system in your project:

- [ ] Understand the `.resx` file structure and location
- [ ] Use `[DtoResourceType]` on all DTOs with validation
- [ ] Always use `AppDataAnnotationsValidator` in forms
- [ ] Access strings via `Localizer[nameof(AppStrings.Key)]`
- [ ] Install and configure `bit-resx` tool
- [ ] Set up environment variables for API keys
- [ ] Run `bit-resx-translate` after adding new resource keys
- [ ] Test with multiple languages (including RTL if supported)
- [ ] Use descriptive resource key names with context
- [ ] Never hardcode user-facing text

### 📚 Additional Resources

- **bit-resx Documentation**: https://github.com/bitfoundation/bitplatform/tree/develop/src/ResxTranslator
- **.NET Globalization**: https://learn.microsoft.com/dotnet/core/extensions/globalization
- **Blazor Localization**: https://learn.microsoft.com/aspnet/core/blazor/globalization-localization
- **ISO Language Codes**: https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes

This comprehensive localization system ensures your application can serve a global audience with minimal effort, while maintaining high code quality and developer productivity.

---
