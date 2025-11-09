# Stage 5: Localization and Multi-language Support

## Overview

The Boilerplate project includes a comprehensive localization and multi-language support system that enables your application to serve users in multiple languages. The system is built on .NET's standard `.resx` resource file infrastructure and integrates seamlessly with Blazor components, validation attributes, and server-side code.

---

## 1. Resource Files (`.resx`) Structure

Resource files are the foundation of the localization system. They store translatable strings as key-value pairs.

### Location

All resource files are located in the `src/Shared/Resources/` folder:

- **`AppStrings.resx`**: The default language resource file (English by default)
- **`AppStrings.fa.resx`**: Persian/Farsi translations
- **`AppStrings.sv.resx`**: Swedish translations
- **`IdentityStrings.resx`**: Default identity-related strings
- **`IdentityStrings.fa.resx`**: Persian identity strings
- **`IdentityStrings.sv.resx`**: Swedish identity strings

### File Structure

Each `.resx` file is an XML document containing name-value pairs:

```xml
<data name="Name" xml:space="preserve">
  <value>Name</value>
</data>
<data name="Email" xml:space="preserve">
  <value>Email</value>
</data>
<data name="RequiredAttribute_ValidationError" xml:space="preserve">
  <value>The {0} field is required.</value>
</data>
```

### Naming Convention

- **Default language**: `[FileName].resx` (e.g., `AppStrings.resx`)
- **Translated files**: `[FileName].[culture].resx` (e.g., `AppStrings.fa.resx`, `AppStrings.sv.resx`)

The culture code follows the ISO 639-1 standard (e.g., `fa` for Persian, `sv` for Swedish, `fr` for French).

### Example: Comparing Default and Translated Files

**AppStrings.resx (English - Default):**
```xml
<data name="Email" xml:space="preserve">
  <value>Email</value>
</data>
<data name="Password" xml:space="preserve">
  <value>Password</value>
</data>
```

**AppStrings.fa.resx (Persian):**
```xml
<data name="Email" xml:space="preserve">
  <value>ایمیل</value>
</data>
<data name="Password" xml:space="preserve">
  <value>رمز عبور</value>
</data>
```

---

## 2. DTOs and the `[DtoResourceType]` Attribute

DTOs (Data Transfer Objects) use the `[DtoResourceType]` attribute to connect validation messages and display names to resource files. This ensures that validation errors and form labels are automatically localized.

### Example: CategoryDto

Here's the `CategoryDto` from `src/Shared/Dtos/Categories/CategoryDto.cs`:

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

### Key Points:

1. **`[DtoResourceType(typeof(AppStrings))]`**: Tells the localization system which resource file to use
2. **`ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError)`**: References the error message key from the resource file
3. **`Display(Name = nameof(AppStrings.Name))`**: References the display name key for UI labels

### Why Use `nameof()`?

Using `nameof()` provides compile-time safety. If you rename or remove a resource key, you'll get a compilation error rather than a runtime error.

---

## 3. AppDataAnnotationsValidator

To make `DtoResourceType` work in Blazor EditForms, you **must** use the custom `AppDataAnnotationsValidator` component instead of the standard `DataAnnotationsValidator`.

### Why is it Needed?

The `AppDataAnnotationsValidator` integrates with the `DtoResourceType` attribute to:
- Resolve localized error messages from resource files
- Support dynamic language switching
- Provide compile-time safety with `nameof()`

### Location

`src/Client/Boilerplate.Client.Core/Components/AppDataAnnotationsValidator.cs`

### Usage Example

Here's an excerpt from `AddOrEditCategoryModal.razor`:

```xml
<EditForm @ref="editForm" Model="category" OnValidSubmit="WrapHandled(Save)" novalidate>
    <AppDataAnnotationsValidator @ref="validatorRef" />

    <BitStack Gap="0.25rem">
        <BitTextField @bind-Value="category.Name"
                      Label="@Localizer[nameof(AppStrings.Name)]"
                      Placeholder="@Localizer[nameof(AppStrings.EnterCategoryName)]" />
        <ValidationMessage For="() => category.Name" />
    </BitStack>
</EditForm>
```

### Key Points:

- Replace `<DataAnnotationsValidator />` with `<AppDataAnnotationsValidator />`
- Validation messages will automatically be localized based on the user's selected language
- Works seamlessly with all standard validation attributes (`Required`, `MaxLength`, `EmailAddress`, etc.)

---

## 4. Using `IStringLocalizer<T>` in Code

The `IStringLocalizer<T>` interface is used to access localized strings programmatically in C# code.

### In Components (Razor Pages)

All components that inherit from `AppComponentBase` automatically have access to the `Localizer` property:

**Location**: `src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs`

```csharp
[AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
```

**Usage in Razor files**:

From `AddOrEditCategoryModal.razor`:

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
```

### In Controllers (Server-Side)

All API controllers that inherit from `AppControllerBase` have access to the `Localizer` property:

**Location**: `src/Server/Boilerplate.Server.Api/Controllers/AppControllerBase.cs`

```csharp
public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected AppDbContext DbContext = default!;

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
}
```

**Usage in Controllers**:

```csharp
throw new ResourceValidationException(Localizer[nameof(AppStrings.DuplicateCategoryName), category.Name!]);
```

### In Services

For services that don't inherit from a base class, inject `IStringLocalizer<T>` directly:

**Example from `EmailServiceJobsRunner.cs`**:

```csharp
[AutoInject] IStringLocalizer<AppStrings> localizer = default!;

[AutoInject] private IStringLocalizer<EmailStrings> emailLocalizer = default!;
```

### Localization with Parameters

Resource strings can include placeholders like `{0}`, `{1}`, etc.:

**Resource file**:
```xml
<data name="AreYouSureWannaDelete" xml:space="preserve">
  <value>Are you sure you want to delete {0}?</value>
</data>
```

**Usage**:
```csharp
var message = Localizer[nameof(AppStrings.AreYouSureWannaDelete), categoryName];
// Result: "Are you sure you want to delete Electronics?"
```

---

## 5. The `bit-resx` Tool - Automated Translation

The `bit-resx` tool (also known as `Bit.ResxTranslator`) is a .NET global tool that automates the translation of `.resx` files using Large Language Models (LLMs) like OpenAI or Azure OpenAI.

### What Does It Do?

- Compares the default language `.resx` file with translated versions
- Identifies missing translations in target language files
- Uses AI (OpenAI/Azure OpenAI) to automatically translate missing entries
- Preserves existing translations and placeholder formats (like `{0}`)
- Updates `.resx` files with new translations while maintaining XML structure

### Installation

Install as a global .NET tool:

```bash
dotnet tool install --global Bit.ResxTranslator --prerelease
```

### Configuration

Create a `Bit.ResxTranslator.json` file in your project root:

```json
{
  "DefaultLanguage": "en",
  "SupportedLanguages": ["fa", "sv", "fr", "de"],
  "ResxPaths": [
    "src/Shared/Resources/*.resx",
    "src/Server/Boilerplate.Server.Api/Resources/*.resx"
  ],
  "OpenAI": {
    "Model": "gpt-4",
    "ApiKey": "your-api-key-here"
  }
}
```

**Security Note**: Use environment variables for API keys instead of hardcoding them:

```json
{
  "OpenAI": {
    "ApiKey": "${OPENAI_API_KEY}"
  }
}
```

### Usage

Run the translation command:

```bash
bit-resx-translate
```

The tool will:
1. Find all `.resx` files based on the configured paths
2. Identify missing translations for each supported language
3. Translate missing entries using the configured LLM
4. Update the `.resx` files with new translations

### How It Works

1. **File Discovery**: Finds all primary `.resx` files matching the configured glob patterns
2. **Comparison**: For each supported language, compares the default `.resx` with the translated version
3. **Translation**: Sends missing keys and values to the LLM with instructions to:
   - Translate the text
   - Preserve placeholders (e.g., `{0}`, `{1}`)
   - Maintain the same tone and context
4. **Update**: Inserts translated values back into the target `.resx` files

### CI/CD Integration

The `bit-resx` tool can be integrated into CI/CD pipelines to ensure translations are always up-to-date. The bitplatform repository uses it in GitHub Actions workflows to automate translations during builds.

---

## 6. Language Selection and Switching

### Client-Side Language Selection

Users can select their preferred language from the Settings page. The selected language is stored locally and applied across all components.

### How It Works

1. **Culture Selection**: User selects a language from a dropdown
2. **Storage**: The selected culture is saved to local storage
3. **Application**: The application reloads with the new culture
4. **Persistence**: The selected language persists across sessions

### Supported Languages

By default, the Boilerplate supports:
- **English** (`en`) - Default
- **Persian/Farsi** (`fa`)
- **Swedish** (`sv`)

You can add more languages by:
1. Creating new `.resx` files with the appropriate culture code
2. Adding the culture to the supported languages configuration
3. Translating the resource strings (manually or using `bit-resx`)

---

## 7. Best Practices

### 1. Always Use Resource Keys

Never hardcode user-facing text:

❌ **Bad**:
```csharp
<h1>Welcome to the application</h1>
```

✅ **Good**:
```csharp
<h1>@Localizer[nameof(AppStrings.WelcomeMessage)]</h1>
```

### 2. Use `nameof()` for Compile-Time Safety

Always use `nameof()` when referencing resource keys:

❌ **Bad**:
```csharp
Localizer["Name"] // Runtime error if key doesn't exist
```

✅ **Good**:
```csharp
Localizer[nameof(AppStrings.Name)] // Compile-time error if key doesn't exist
```

### 3. Use AppDataAnnotationsValidator in Forms

Always use `AppDataAnnotationsValidator` instead of `DataAnnotationsValidator` in Blazor EditForms:

❌ **Bad**:
```xml
<EditForm Model="model">
    <DataAnnotationsValidator />
    ...
</EditForm>
```

✅ **Good**:
```xml
<EditForm Model="model">
    <AppDataAnnotationsValidator />
    ...
</EditForm>
```

### 4. Organize Resource Files by Domain

- **AppStrings.resx**: General application strings
- **IdentityStrings.resx**: Authentication and authorization strings
- **EmailStrings.resx**: Email template strings

This separation makes it easier to manage and maintain translations.

### 5. Include Context in Resource Key Names

Use descriptive key names that include context:

✅ **Good**:
- `SignInPageTitle`
- `ProfileUpdatedSuccessfullyMessage`
- `DeleteAccountPrompt`

❌ **Bad**:
- `Title`
- `Success`
- `Prompt`

### 6. Test with Different Languages

Always test your application with different languages to ensure:
- Text doesn't overflow UI components
- RTL (Right-to-Left) languages display correctly
- Placeholders are properly replaced
- Validation messages are clear

---

## 8. Adding a New Language

To add support for a new language (e.g., French):

### Step 1: Create Resource Files

Create new `.resx` files for each resource type:
- `AppStrings.fr.resx`
- `IdentityStrings.fr.resx`
- `EmailStrings.fr.resx`

### Step 2: Add Translations

Either:
- Manually translate all keys from the default `.resx` file
- Or use the `bit-resx` tool to automate translation

### Step 3: Update Configuration

Add the new culture to your `Bit.ResxTranslator.json`:

```json
{
  "SupportedLanguages": ["fa", "sv", "fr"]
}
```

### Step 4: Update UI

Add the new language option to the language selection dropdown in your Settings page.

### Step 5: Test

Test the application with the new language to ensure all strings are properly translated and displayed.

---

## Summary

The Boilerplate's localization system provides:

✅ **Centralized Translation Management**: All strings in `.resx` files  
✅ **Type-Safe Access**: Using `nameof()` for compile-time safety  
✅ **Automatic Validation Localization**: Through `DtoResourceType` and `AppDataAnnotationsValidator`  
✅ **AI-Powered Translation**: Using the `bit-resx` tool  
✅ **Seamless Integration**: Works across Blazor components, controllers, and services  
✅ **Runtime Language Switching**: Users can change language without recompiling  

This comprehensive localization system ensures your application can serve a global audience with minimal effort.
