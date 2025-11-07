# Stage 5: Localization and Multi-language Support

Welcome to Stage 5! In this stage, you'll learn about the comprehensive localization and multi-language support system built into the project. This system enables your application to provide a seamless experience for users across different languages and cultures.

## Overview

The project uses **.NET's built-in localization system** based on `.resx` (resource) files, combined with powerful validation and UI integration. This approach provides:

- ✅ **Type-safe localization** with compile-time checking
- ✅ **Automatic validation message translation** for DTOs
- ✅ **Centralized resource management** for all languages
- ✅ **AI-powered translation** with the `bit-resx` tool
- ✅ **Support for multiple languages** with easy extensibility

---

## 1. Resx Files Structure

### Location

All resource files are located in:
[`/src/Shared/Resources/`](/src/Shared/Resources/)

### File Organization

The project includes multiple resource files for different purposes:

**Application Strings:**
- [`AppStrings.resx`](/src/Shared/Resources/AppStrings.resx) - Default language (English)
- [`AppStrings.fa.resx`](/src/Shared/Resources/AppStrings.fa.resx) - Persian/Farsi translation
- [`AppStrings.sv.resx`](/src/Shared/Resources/AppStrings.sv.resx) - Swedish translation

**Identity-Specific Strings:**
- [`IdentityStrings.resx`](/src/Shared/Resources/IdentityStrings.resx) - Default language
- [`IdentityStrings.fa.resx`](/src/Shared/Resources/IdentityStrings.fa.resx) - Persian translation
- [`IdentityStrings.sv.resx`](/src/Shared/Resources/IdentityStrings.sv.resx) - Swedish translation

### Resx File Structure Example

Here's a snippet from [`AppStrings.resx`](/src/Shared/Resources/AppStrings.resx):

```xml
<!-- .NET Validation messages -->
<data name="RequiredAttribute_ValidationError" xml:space="preserve">
    <value>The {0} field is required.</value>
</data>

<data name="MaxLengthAttribute_InvalidMaxLength" xml:space="preserve">
    <value>MaxLengthAttribute must have a Length value that is greater than zero.</value>
</data>

<data name="EmailAddressAttribute_ValidationError" xml:space="preserve">
    <value>The {0} field is not a valid e-mail address.</value>
</data>

<!-- Application-specific strings -->
<data name="Name" xml:space="preserve">
    <value>Name</value>
</data>

<data name="Color" xml:space="preserve">
    <value>Color</value>
</data>
```

And the corresponding Persian translation in [`AppStrings.fa.resx`](/src/Shared/Resources/AppStrings.fa.resx):

```xml
<data name="RequiredAttribute_ValidationError" xml:space="preserve">
    <value>مقدار {0} الزامی است.</value>
</data>

<data name="EmailAddressAttribute_ValidationError" xml:space="preserve">
    <value>مقدار {0} یک آدرس ایمیل معتبر نیست.</value>
</data>
```

### Key Characteristics

1. **Default Language File**: `AppStrings.resx` contains the default English strings
2. **Translated Files**: Use culture codes (e.g., `.fa.resx`, `.sv.resx`) for specific languages
3. **Placeholder Support**: `{0}`, `{1}`, etc., for dynamic values in validation messages
4. **Shared Across Platforms**: Used by both server and all client platforms (Web, MAUI, Windows)

---

## 2. DtoResourceType Attribute

### What is DtoResourceType?

The `[DtoResourceType]` attribute connects DTOs to resource files, enabling automatic translation of:
- Validation error messages
- Display names for properties

### Real Example: CategoryDto

Here's an actual DTO from the project - [`CategoryDto.cs`](/src/Shared/Dtos/Categories/CategoryDto.cs):

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

### How It Works

1. **`[DtoResourceType(typeof(AppStrings))]`**: Links the DTO to the `AppStrings` resource file
2. **`ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError)`**: References the resource key by name (compile-time safe!)
3. **`Display(Name = nameof(AppStrings.Name))`**: Uses the localized display name

### Benefits

✅ **Type Safety**: Using `nameof()` ensures compile-time checking - typos are caught immediately  
✅ **Automatic Translation**: Error messages automatically use the user's selected language  
✅ **Centralized Management**: All strings are managed in one place  
✅ **Consistency**: Same validation messages across the entire application

---

## 3. AppDataAnnotationsValidator

### What is AppDataAnnotationsValidator?

`AppDataAnnotationsValidator` is a **custom validator component** that must be used in Blazor `EditForm` components to make the `DtoResourceType` system work properly.

### Why It's Needed

The standard `DataAnnotationsValidator` doesn't support the `[DtoResourceType]` attribute. `AppDataAnnotationsValidator` extends it to:
- Read the `[DtoResourceType]` attribute from DTOs
- Load the correct resource file
- Apply localized validation messages

### Real Example: AddOrEditCategoryModal

Here's how it's used in [`AddOrEditCategoryModal.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/Categories/AddOrEditCategoryModal.razor):

```xml
<EditForm @ref="editForm" Model="category" OnValidSubmit="WrapHandled(Save)" novalidate>
    <AppDataAnnotationsValidator @ref="validatorRef" />

    <BitStack Gap="0.25rem">
        <BitTextField @bind-Value="category.Name"
                      Label="@Localizer[nameof(AppStrings.Name)]"
                      Placeholder="@Localizer[nameof(AppStrings.EnterCategoryName)]" />
        <ValidationMessage For="() => category.Name" />
        
        <!-- More form fields... -->
    </BitStack>
</EditForm>
```

### Usage Pattern

```xml
<EditForm Model="yourDto" OnValidSubmit="HandleSubmit">
    <!-- Use AppDataAnnotationsValidator instead of DataAnnotationsValidator -->
    <AppDataAnnotationsValidator />
    
    <!-- Your form fields -->
    <ValidationMessage For="() => yourDto.PropertyName" />
</EditForm>
```

### Where You'll Find It

Search the project for `AppDataAnnotationsValidator` usage:
- [`ProfileSection.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/Settings/ProfileSection.razor)
- [`SignInPanel.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/Identity/SignIn/SignInPanel.razor)
- [`AddOrEditProductPage.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/Products/AddOrEditProductPage.razor)
- And many more!

---

## 4. IStringLocalizer Usage

### What is IStringLocalizer?

`IStringLocalizer<T>` is .NET's standard interface for accessing localized strings in code. It allows you to:
- Get translated strings at runtime
- Support dynamic content localization
- Use the same resource files across server and client

### Automatic Injection in Base Classes

The project's base classes automatically inject `IStringLocalizer<AppStrings>`, so you don't need to inject it manually!

**In Components** (inheriting from `AppComponentBase`):

```csharp
// From AppComponentBase.cs
[AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
```

**In API Controllers** (inheriting from `AppControllerBase`):

```csharp
// From AppControllerBase.cs
[AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
```

This means in your components and controllers, you can directly use `Localizer` without declaring it!

---

### Usage in Razor Components

Here's a real example from [`MainLayout.razor.items.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/MainLayout.razor.items.cs):

```csharp
public partial class MainLayout
{
    private List<BitNavItem> navPanelItems = [];

    [AutoInject] protected IStringLocalizer<AppStrings> localizer = default!;

    private async Task SetNavPanelItems(ClaimsPrincipal authUser)
    {
        navPanelItems =
        [
            new()
            {
                Text = localizer[nameof(AppStrings.Home)],
                IconName = BitIconName.Home,
                Url = PageUrls.Home,
            },
            new()
            {
                Text = localizer[nameof(AppStrings.AdminPanel)],
                IconName = BitIconName.Admin,
                ChildItems = []
            },
            new()
            {
                Text = localizer[nameof(AppStrings.Categories)],
                IconName = BitIconName.BuildQueue,
                Url = PageUrls.Categories,
            }
        ];
    }
}
```

### Usage in Razor Markup

From [`AddOrEditCategoryModal.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/Categories/AddOrEditCategoryModal.razor):

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
              Label="@Localizer[nameof(AppStrings.Name)]"
              Placeholder="@Localizer[nameof(AppStrings.EnterCategoryName)]" />

<BitButton OnClick="Save">
    @Localizer[nameof(AppStrings.Save)]
</BitButton>
```

### Usage Pattern

```csharp
// Access a localized string
string localizedText = Localizer[nameof(AppStrings.YourResourceKey)];

// In Razor markup
@Localizer[nameof(AppStrings.YourResourceKey)]
```

### Common Use Cases

1. **UI Labels and Text**:
   ```csharp
   Text = localizer[nameof(AppStrings.Home)]
   ```

2. **Button Labels**:
   ```xml
   <BitButton>@Localizer[nameof(AppStrings.Save)]</BitButton>
   ```

3. **Page Titles**:
   ```xml
   <BitText>@Localizer[nameof(AppStrings.Dashboard)]</BitText>
   ```

4. **Error Messages in Code**:
   ```csharp
   throw new BadRequestException(Localizer[nameof(AppStrings.InvalidRequest)]);
   ```

---

## 5. bit-resx Tool: AI-Powered Translation

### What is bit-resx?

`Bit.ResxTranslator` is a .NET global tool that **automatically translates** `.resx` resource files into multiple languages using Large Language Models (LLMs) like OpenAI or Azure OpenAI.

### Key Features

✅ **Identifies Missing Translations**: Compares default language files with target language files  
✅ **AI-Powered Translation**: Uses GPT models to generate accurate, context-aware translations  
✅ **Preserves Placeholders**: Keeps `{0}`, `{1}` placeholders in correct positions  
✅ **Creates New Language Files**: Automatically creates missing `.resx` files for new languages  
✅ **CI/CD Integration**: Can be run as part of your build pipeline

### Installation

```bash
dotnet tool install --global Bit.ResxTranslator
```

### Configuration

Create a `Bit.ResxTranslator.json` file in your project root:

```json
{
  "DefaultLanguage": "en",
  "SupportedLanguages": [ "nl", "fa", "sv", "hi", "zh", "es", "fr", "ar", "de" ],
  "ResxPaths": [ "src/**/*.resx" ],
  "OpenAI": {
    "Model": "gpt-4.1-mini",
    "Endpoint": "https://models.inference.ai.azure.com",
    "ApiKey": null
  },
  "AzureOpenAI": {
    "Model": "gpt-4.1-mini",
    "Endpoint": "https://yourResourceName.openai.azure.com/openai/deployments/yourDeployment",
    "ApiKey": null
  }
}
```

**Configuration Options:**
- `DefaultLanguage`: Your primary language (e.g., "en", "en-US")
- `SupportedLanguages`: Array of .NET culture codes for target languages
- `ResxPaths`: Glob patterns to find `.resx` files
- `OpenAI` / `AzureOpenAI`: LLM provider configuration

### Usage

Run the translation tool:

```bash
bit-resx-translate
```

### How It Works

1. **Scan**: Finds all `.resx` files matching your `ResxPaths` pattern
2. **Compare**: Identifies keys present in default language but missing in target languages
3. **Translate**: Uses AI to translate missing strings while preserving placeholders
4. **Update**: Writes translations back to target language `.resx` files
5. **Create**: Creates new language files if they don't exist

### CI/CD Integration

The tool is integrated into the project's GitHub Actions workflows:

```yaml
- name: Use Bit.ResxTranslator
  run: |
    dotnet tool install --global Bit.ResxTranslator
    bit-resx-translate
```

This ensures automatic translation during build and deployment, so published applications always have up-to-date multi-language support!

### Real-World Example

When you add a new string to `AppStrings.resx`:

```xml
<data name="WelcomeMessage" xml:space="preserve">
    <value>Welcome to our application!</value>
</data>
```

Running `bit-resx-translate` automatically adds translations to other language files:

**`AppStrings.fa.resx` (Persian):**
```xml
<data name="WelcomeMessage" xml:space="preserve">
    <value>به برنامه ما خوش آمدید!</value>
</data>
```

**`AppStrings.sv.resx` (Swedish):**
```xml
<data name="WelcomeMessage" xml:space="preserve">
    <value>Välkommen till vår applikation!</value>
</data>
```

---

## 6. Adding a New Language

Want to add support for a new language? Here's how:

### Step 1: Update Configuration

Add the language code to `Bit.ResxTranslator.json`:

```json
{
  "SupportedLanguages": [ "nl", "fa", "sv", "hi", "zh", "es", "fr", "ar", "de", "ja" ]
  //                                                                           ^^^^ Add Japanese
}
```

### Step 2: Run Translation Tool

```bash
bit-resx-translate
```

This creates new `.resx` files:
- `AppStrings.ja.resx`
- `IdentityStrings.ja.resx`

### Step 3: Verify and Refine

Review the generated translations and make manual corrections if needed. The AI does a great job, but human review ensures quality!

---

## 7. Best Practices

### ✅ DO

1. **Always use `nameof()`** for resource keys to ensure type safety
   ```csharp
   [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
   ```

2. **Use `AppDataAnnotationsValidator`** in all `EditForm` components
   ```xml
   <EditForm Model="dto">
       <AppDataAnnotationsValidator />
   </EditForm>
   ```

3. **Leverage base class injection** - `Localizer` is already available in components and controllers

4. **Run `bit-resx-translate`** after adding new strings to keep translations up-to-date

5. **Review AI translations** before committing to ensure accuracy

### ❌ DON'T

1. **Don't hardcode strings** in UI or validation messages
   ```csharp
   // ❌ Bad
   [Required(ErrorMessage = "This field is required")]
   
   // ✅ Good
   [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
   ```

2. **Don't use magic strings** for resource keys
   ```csharp
   // ❌ Bad
   Localizer["Name"]
   
   // ✅ Good
   Localizer[nameof(AppStrings.Name)]
   ```

3. **Don't forget to use `AppDataAnnotationsValidator`**
   ```xml
   <!-- ❌ Bad - won't work with DtoResourceType -->
   <DataAnnotationsValidator />
   
   <!-- ✅ Good -->
   <AppDataAnnotationsValidator />
   ```

---

## 8. Localization Flow Summary

Here's how it all fits together:

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Add string to AppStrings.resx (English)                 │
│    <data name="Name"><value>Name</value></data>             │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. Run bit-resx-translate                                   │
│    → AI generates translations for all languages            │
│    → Creates/updates AppStrings.fa.resx, AppStrings.sv.resx│
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. Use in DTO with DtoResourceType                         │
│    [Display(Name = nameof(AppStrings.Name))]                │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. Use in Razor with Localizer                             │
│    @Localizer[nameof(AppStrings.Name)]                      │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 5. User sees content in their language!                     │
│    English: "Name"  |  Persian: "نام"  |  Swedish: "Namn"  │
└─────────────────────────────────────────────────────────────┘
```

---

## 9. Advanced: Multiple Resource Files

The project uses separate resource files for different purposes:

### AppStrings
General application strings used across the app
- Location: [`/src/Shared/Resources/AppStrings.resx`](/src/Shared/Resources/AppStrings.resx)
- Usage: `IStringLocalizer<AppStrings>`

### IdentityStrings
Identity-specific strings (authentication, authorization)
- Location: [`/src/Shared/Resources/IdentityStrings.resx`](/src/Shared/Resources/IdentityStrings.resx)
- Usage: `IStringLocalizer<IdentityStrings>`

### Example: Using IdentityStrings

```csharp
[AutoInject] IStringLocalizer<IdentityStrings> identityLocalizer = default!;

var message = identityLocalizer[nameof(IdentityStrings.PasswordTooShort)];
```

---

## 10. Platform Support

The localization system works seamlessly across **all platforms**:

✅ **Blazor Server** (Server.Web)  
✅ **Blazor WebAssembly** (Client.Web)  
✅ **.NET MAUI** (Client.Maui) - Android, iOS, macOS  
✅ **Windows Forms** (Client.Windows)  
✅ **API Controllers** (Server.Api)

The same resource files and localization code work everywhere!

---

## Summary

You've learned about the project's comprehensive localization system:

✅ **Resx Files**: Centralized resource management with default and translated files  
✅ **DtoResourceType**: Automatic validation message translation for DTOs  
✅ **AppDataAnnotationsValidator**: Required component for localized validation  
✅ **IStringLocalizer**: Runtime string localization in code and markup  
✅ **bit-resx Tool**: AI-powered automatic translation with CI/CD integration  

This system enables you to build truly international applications with minimal effort. Add new languages in minutes, and leverage AI to handle the translation work!

---
