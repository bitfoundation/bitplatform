# Stage 8: Blazor Pages, Components, Styling & Navigation

Welcome to Stage 8! In this stage, you'll learn about the Blazor UI architecture, component structure, styling system, and navigation in the Boilerplate project. We'll explore real examples from the actual codebase to see how everything works together.

---

## 1. Component Structure: The Three-File Pattern

In this project, each Blazor component or page follows a **three-file structure** that separates concerns for better organization and maintainability:

### Example: ProductPage

Let's examine the `ProductPage` component located at:
- 📄 **Razor file**: `/src/Client/Boilerplate.Client.Core/Components/Pages/ProductPage.razor`
- 📄 **Code-behind file**: `/src/Client/Boilerplate.Client.Core/Components/Pages/ProductPage.razor.cs`
- 📄 **SCSS file**: `/src/Client/Boilerplate.Client.Core/Components/Pages/ProductPage.razor.scss`

### 1.1 The `.razor` File (UI Markup)

The `.razor` file contains the component's HTML markup and Blazor component tags:

```xml
@attribute [Route(PageUrls.Product + "/{Id:int}")]
@attribute [Route("{culture?}" + PageUrls.Product + "/{Id:int}")]
@attribute [AppResponseCache(SharedMaxAge = 3600 * 24, MaxAge = 60 * 5)]
@inherits AppPageBase

<AppPageData ShowGoBackButton="true" Title="@product?.Name" />

<section>
    <BitStack Gap="2rem" Class="root-stack">
        @if (isLoadingProduct)
        {
            <BitCard FullWidth Background="BitColorKind.Tertiary" Style="padding:3rem 1rem;">
                <BitStack Horizontal HorizontalAlign="BitAlignment.Center" Gap="2rem">
                    <BitShimmer Height="256px" Width="50%" Background="BitColor.PrimaryBackground" />
                    <BitStack AutoHeight Grows>
                        <BitShimmer Height="6rem" Width="100%" />
                        <!-- Loading skeleton UI -->
                    </BitStack>
                </BitStack>
            </BitCard>
        }
        else
        {
            <BitCard FullWidth Style="padding:3rem 1rem;">
                <BitStack Horizontal HorizontalAlign="BitAlignment.Center" Gap="2rem" Class="product-stack">
                    <ProductImage Src="@GetProductImageUrl(product)" Alt="@product.PrimaryImageAltText" Width="50%" />
                    <BitStack AutoWidth Grows>
                        <BitText Typography="BitTypography.H2">@product.Name</BitText>
                        <BitText Color="BitColor.Info">@product.CategoryName</BitText>
                        <BitText Typography="BitTypography.H4">@product.FormattedPrice</BitText>
                        <BitButton AutoLoading OnClick="WrapHandled(Buy)">
                            @Localizer[nameof(AppStrings.Buy)]
                        </BitButton>
                    </BitStack>
                </BitStack>
            </BitCard>
        }
    </BitStack>
</section>
```

**Key Points:**
- Uses `@attribute` directives for routing and caching configuration
- Inherits from `AppPageBase` for enhanced lifecycle methods
- Uses Bit.BlazorUI components like `BitStack`, `BitCard`, `BitButton`, `BitText`, `BitShimmer`
- Event handlers wrapped with `WrapHandled()` for automatic exception handling
- No `@code` block - all logic is in the code-behind file

### 1.2 The `.razor.cs` File (Code-Behind Logic)

The `.razor.cs` file contains the component's logic, data, and methods:

```csharp
using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class ProductPage
{
    [Parameter] public int Id { get; set; }

    [AutoInject] private SignInModalService signInModalService = default!;
    [AutoInject] private IProductViewController productViewController = default!;

    private ProductDto? product;
    private List<ProductDto>? similarProducts;
    private List<ProductDto>? siblingProducts;
    private bool isLoadingProduct = true;
    private bool isLoadingSimilarProducts = true;
    private bool isLoadingSiblingProducts = true;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();
        await Task.WhenAll(LoadProduct(), LoadSimilarProducts(), LoadSiblingProducts());
    }

    private async Task LoadProduct()
    {
        try
        {
            product = await productViewController.Get(Id, CurrentCancellationToken);
        }
        finally
        {
            isLoadingProduct = false;
            StateHasChanged();
        }
    }

    private async Task Buy()
    {
        if ((await AuthenticationStateTask).User.IsAuthenticated() is false && 
            await signInModalService.SignIn() is false)
        {
            SnackBarService.Error(Localizer[nameof(AppStrings.YouNeedToSignIn)]);
            return;
        }

        SnackBarService.Success(Localizer[nameof(AppStrings.PurchaseSuccessful)]);
    }

    private string? GetProductImageUrl(ProductDto? product) => 
        product?.GetPrimaryMediumImageUrl(AbsoluteServerAddress);
}
```

**Key Points:**
- Uses `[AutoInject]` attribute for dependency injection (instead of constructor injection)
- Inherits dependencies from `AppPageBase` (like `Localizer`, `SnackBarService`, `CurrentCancellationToken`)
- Uses enhanced lifecycle method `OnInitAsync()` instead of `OnInitializedAsync()`
- All service calls pass `CurrentCancellationToken` for automatic request cancellation
- Private fields for component state management

### 1.3 The `.razor.scss` File (Component Styles)

The `.razor.scss` file contains isolated, scoped styles for the component:

```scss
@import '../../Styles/abstracts/_media-queries.scss';

section {
    width: 100%;
    display: flex;
    justify-content: center;
}

::deep {
    .root-stack {
        max-width: 60rem;
    }

    .product-stack {
        display: flex !important;

        @include lt-sm {
            flex-direction: column !important;
        }
    }
}
```

**Key Points:**
- Imports shared SCSS utilities (media queries, variables)
- Uses `::deep` selector to style child Bit.BlazorUI components
- Responsive design with media query mixins (`@include lt-sm`)
- Scoped to this component only (won't affect other components)

---

## 2. SCSS Styling Architecture

The project uses a sophisticated SCSS architecture for styling. Let's explore each layer:

### 2.1 Isolated Component Styles (`.razor.scss`)

Each component has its own `.razor.scss` file that creates **CSS isolation**:

```scss
// ProductPage.razor.scss
section {
    width: 100%;
    display: flex;
    justify-content: center;
}
```

These styles are **scoped** to the component and won't leak to other components. Blazor automatically generates unique identifiers to ensure isolation.

### 2.2 The `::deep` Selector for Styling Child Components

When you need to style **child components** (especially Bit.BlazorUI components) from a parent component's stylesheet, use the `::deep` selector:

```scss
::deep {
    .root-stack {
        max-width: 60rem;
    }

    .product-stack {
        display: flex !important;
        
        @include lt-sm {
            flex-direction: column !important;
        }
    }
}
```

**Why `::deep` is needed:**
- Bit.BlazorUI components have their own internal structure and CSS classes
- Without `::deep`, your styles won't penetrate the component boundary
- With `::deep`, you can customize the appearance of child components while maintaining encapsulation

**Real-world examples from the project:**

```scss
// From CategoriesPage.razor.scss
::deep {
    .categories-datagrid {
        height: calc(100vh - 20rem);
    }
}

// From SignInModal.razor.scss
::deep {
    .sign-in-modal {
        .bit-modal-content {
            width: 90vw;
            max-width: 28rem;
        }
    }
}
```

### 2.3 Global Styles (`app.scss`)

The main global stylesheet is located at `/src/Client/Boilerplate.Client.Core/Styles/app.scss`:

```scss
@import '../Styles/abstracts/_media-queries.scss';
@import '../Styles/abstracts/_bit-css-variables.scss';

:root[bit-theme="dark"] {
    // Dark theme customizations
}

* {
    box-sizing: border-box;
    font-family: "Segoe UI";
    -webkit-text-size-adjust: none;
    -webkit-font-smoothing: antialiased;
    -webkit-tap-highlight-color: transparent;
}

html, body, #app-container {
    margin: 0;
    padding: 0;
    width: 100%;
    height: 100%;
    overflow: auto;
    scroll-behavior: smooth;

    @include lt-md {
        overflow: hidden;
    }
}

h1, h2, h3, h4, h5 {
    margin: 0;
}
```

**Purpose:**
- Defines global resets and base styles
- Imports shared utilities and theme variables
- Applies to the entire application
- Sets up responsive behavior with media queries

### 2.4 Theme Color Variables (`_bit-css-variables.scss`)

The project uses theme color variables located at `/src/Client/Boilerplate.Client.Core/Styles/abstracts/_bit-css-variables.scss`.

**🚨 CRITICAL: Always Use Theme Variables, Never Hardcode Colors**

This file exposes all Bit.BlazorUI theme colors as SCSS variables that **automatically support dark/light mode switching**:

```scss
// Primary colors
$bit-color-primary: var(--bit-clr-pri);
$bit-color-primary-hover: var(--bit-clr-pri-hover);
$bit-color-primary-active: var(--bit-clr-pri-active);

// Secondary colors
$bit-color-secondary: var(--bit-clr-sec);
$bit-color-secondary-hover: var(--bit-clr-sec-hover);

// Background colors
$bit-color-background-primary: var(--bit-clr-bg-pri);
$bit-color-background-secondary: var(--bit-clr-bg-sec);
$bit-color-background-tertiary: var(--bit-clr-bg-ter);

// Foreground colors
$bit-color-foreground-primary: var(--bit-clr-fg-pri);
$bit-color-foreground-secondary: var(--bit-clr-fg-sec);

// Border colors
$bit-color-border-primary: var(--bit-clr-brd-pri);
$bit-color-border-secondary: var(--bit-clr-brd-sec);

// Semantic colors
$bit-color-info: var(--bit-clr-inf);
$bit-color-success: var(--bit-clr-suc);
$bit-color-warning: var(--bit-clr-wrn);
$bit-color-error: var(--bit-clr-err);

// Neutrals (grays)
$bit-color-neutrals-white: var(--bit-clr-ntr-white);
$bit-color-neutrals-black: var(--bit-clr-ntr-black);
$bit-color-neutrals-gray10: var(--bit-clr-ntr-gray10);
$bit-color-neutrals-gray20: var(--bit-clr-ntr-gray20);
// ... up to gray220
```

**Example Usage in SCSS:**

```scss
@import '../../Styles/abstracts/_bit-css-variables.scss';

.my-component {
    background-color: $bit-color-background-primary;
    color: $bit-color-foreground-primary;
    border: 1px solid $bit-color-border-primary;

    &:hover {
        background-color: $bit-color-background-primary-hover;
    }
}
```

**Example Usage in C# (Razor):**

```xml
<BitCard Background="BitColorKind.Primary">
    <BitText Color="BitColor.PrimaryForeground">Hello World</BitText>
</BitCard>
```

**Why This Matters:**
- ✅ **Dark/Light Mode Support**: Variables automatically change when theme switches
- ✅ **Consistency**: All colors follow the design system
- ✅ **Maintainability**: Change theme colors in one place, affects entire app
- ❌ **Never do this**: `background-color: #ffffff;` or `color: rgb(255, 255, 255);`
- ✅ **Always do this**: Use theme variables or Bit.BlazorUI component color properties

---

## 3. Bit.BlazorUI Component Library

### 3.1 What is Bit.BlazorUI?

**Bit.BlazorUI** is the primary UI component library used throughout this project. It provides:

- 🎨 **70+ Production-Ready Components**: Buttons, text fields, data grids, modals, dropdowns, date pickers, charts, and more
- 🌙 **Built-in Dark/Light Mode**: Automatic theme switching with no extra configuration
- ♿ **Accessibility**: ARIA-compliant, keyboard navigation, screen reader support
- 📱 **Responsive Design**: Works seamlessly across desktop, tablet, and mobile
- 🎯 **Type-Safe**: Full IntelliSense support with comprehensive documentation
- 🚀 **High Performance**: Optimized for Blazor Server, WASM, and Hybrid scenarios

### 3.2 Comprehensive Documentation

Bit.BlazorUI has **extensive documentation** available at:

🔗 **https://blazorui.bitplatform.dev**

The documentation includes:
- **Live interactive demos** for every component
- **Code examples** (copy-paste ready)
- **API reference** with all parameters and events
- **Customization guides** and best practices
- **Accessibility information**

### 3.3 Common Components Used in This Project

Here are examples of frequently used Bit.BlazorUI components:

#### BitButton
```xml
<BitButton OnClick="WrapHandled(HandleClick)">
    Click Me
</BitButton>

<BitButton Variant="BitVariant.Outline" Color="BitColor.Primary">
    Outline Button
</BitButton>

<BitButton IconName="BitIconName.Add" AutoLoading OnClick="WrapHandled(SaveAsync)">
    Save
</BitButton>
```

#### BitTextField
```xml
<BitTextField @bind-Value="model.Name" 
              Label="Product Name"
              Placeholder="Enter product name"
              Required />
```

#### BitStack (Layout)
```xml
<BitStack Horizontal Gap="1rem" HorizontalAlign="BitAlignment.Center">
    <BitButton>Button 1</BitButton>
    <BitButton>Button 2</BitButton>
    <BitButton>Button 3</BitButton>
</BitStack>

<BitStack Vertical Gap="2rem">
    <BitCard>Card 1</BitCard>
    <BitCard>Card 2</BitCard>
</BitStack>
```

#### BitCard
```xml
<BitCard FullWidth Background="BitColorKind.Tertiary">
    <BitText Typography="BitTypography.H4">Card Title</BitText>
    <BitText>Card content goes here...</BitText>
</BitCard>
```

#### BitDataGrid
```xml
<BitDataGrid Items="products" 
             TGridItem="ProductDto"
             Virtualize
             Loading="isLoading"
             Pagination="@dataGridPagination">
    <BitDataGridPropertyColumn Property="p => p.Name" Title="Product Name" Sortable />
    <BitDataGridPropertyColumn Property="p => p.Price" Title="Price" Sortable />
    <BitDataGridTemplateColumn Title="Actions">
        <BitButton Size="BitSize.Small" OnClick="() => EditProduct(context)">
            Edit
        </BitButton>
    </BitDataGridTemplateColumn>
</BitDataGrid>
```

#### BitText
```xml
<BitText Typography="BitTypography.H1">Heading 1</BitText>
<BitText Typography="BitTypography.H2">Heading 2</BitText>
<BitText Typography="BitTypography.Body1">Body text</BitText>
<BitText Color="BitColor.Info">Info message</BitText>
<BitText Color="BitColor.Error">Error message</BitText>
```

#### BitModal
```xml
<BitModal @bind-IsOpen="isModalOpen" Title="Add Product">
    <BitStack Gap="1rem">
        <BitTextField @bind-Value="newProduct.Name" Label="Name" />
        <BitTextField @bind-Value="newProduct.Price" Label="Price" />
        
        <BitStack Horizontal Gap="1rem">
            <BitButton OnClick="WrapHandled(SaveProduct)">Save</BitButton>
            <BitButton Variant="BitVariant.Outline" OnClick="() => isModalOpen = false">
                Cancel
            </BitButton>
        </BitStack>
    </BitStack>
</BitModal>
```

### 3.4 Automatic DeepWiki Integration

When you ask questions about Bit.BlazorUI in **GitHub Copilot Chat**, the system automatically queries the DeepWiki knowledge base to provide accurate, up-to-date information.

**You don't need to manually search the documentation** - just ask naturally:

✅ **Example Questions:**
- "How do I create a data grid with sorting and pagination?"
- "Show me how to use BitDatePicker with validation"
- "How can I customize the color of a BitButton?"
- "What's the difference between BitStack and BitScrollablePane?"
- "How do I make a BitModal full-screen on mobile?"

The AI assistant will automatically:
1. Query the Bit.BlazorUI documentation via DeepWiki
2. Find relevant examples and API information
3. Provide code samples tailored to this project's conventions

---

## 4. Navigation System with PageUrls

The project uses a centralized navigation system through the `PageUrls` class.

### 4.1 PageUrls Class Structure

Located at `/src/Shared/PageUrls.cs` and related partial files:

```csharp
namespace Boilerplate.Shared;

public static partial class PageUrls
{
    public const string Home = "/";
    public const string NotFound = "/not-found";
    public const string Terms = "/terms";
    public const string Settings = "/settings";
    public const string About = "/about";
    public const string Categories = "/categories";
    public const string Dashboard = "/dashboard";
    public const string Products = "/products";
    public const string AddOrEditProduct = "/add-edit-product";
    public const string Todo = "/todo";
    public const string Product = "/product";
    public const string SystemPrompts = "/system-prompts";
    public const string Authorize = "/authorize";
    public const string Roles = "/user-groups";
    public const string Users = "/users";
}
```

**Benefits:**
- ✅ **Type-safe navigation**: No magic strings, IntelliSense support
- ✅ **Refactoring-friendly**: Rename URLs in one place
- ✅ **Compile-time errors**: Catch broken links before runtime

### 4.2 Using PageUrls in Components

**In Razor files (for routing):**
```xml
@attribute [Route(PageUrls.Products)]
@attribute [Route("{culture?}" + PageUrls.Products)]
```

**In C# code (for navigation):**
```csharp
NavigationManager.NavigateTo(PageUrls.Products);
NavigationManager.NavigateTo($"{PageUrls.Product}/{productId}");
```

**In Razor markup (for links):**
```xml
<BitLink Href="@PageUrls.Products">View Products</BitLink>

<BitNavLink Href="@PageUrls.Dashboard">
    <BitIcon IconName="BitIconName.ViewDashboard" />
    Dashboard
</BitNavLink>
```

### 4.3 Multi-language Route Support

The project supports culture-specific routing:

```csharp
@attribute [Route(PageUrls.Product + "/{Id:int}")]
@attribute [Route("{culture?}" + PageUrls.Product + "/{Id:int}")]
```

This allows URLs like:
- `/product/123` (default culture)
- `/en/product/123` (English)
- `/fr/product/123` (French)
- `/fa/product/123` (Persian)

---

## 5. Component Base Classes

The project provides two base classes with built-in functionality:

### 5.1 AppComponentBase

Located at `/src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs`

**All components inherit from this base class**, which provides:

#### Automatic Dependency Injection
No need to manually inject common services - they're already available:

```csharp
public partial class MyComponent : AppComponentBase
{
    protected override async Task OnInitAsync()
    {
        // These are automatically available from AppComponentBase:
        var user = await AuthenticationStateTask;
        var message = Localizer[nameof(AppStrings.Welcome)];
        
        NavigationManager.NavigateTo(PageUrls.Home);
        SnackBarService.Success("Operation completed!");
        
        await StorageService.SetItem("key", "value");
        var config = Configuration["AppSettings:ApiKey"];
    }
}
```

**Pre-injected Services:**
- `NavigationManager` - For navigation
- `Localizer` - For localization
- `SnackBarService` - For showing notifications
- `AuthenticationStateTask` - For auth state
- `PubSubService` - For pub/sub messaging
- `StorageService` - For local/session storage
- `Configuration` - For app settings
- `ExceptionHandler` - For error handling
- `AuthManager` - For authentication operations
- `JSRuntime` - For JavaScript interop
- `CurrentCancellationToken` - For request cancellation

#### Enhanced Lifecycle Methods

Instead of standard Blazor lifecycle methods, use these **safer alternatives** that automatically handle exceptions:

```csharp
// ✅ Use this instead of OnInitializedAsync()
protected override async Task OnInitAsync()
{
    // Exceptions are automatically caught and handled
    await LoadDataAsync();
}

// ✅ Use this instead of OnParametersSetAsync()
protected override async Task OnParamsSetAsync()
{
    // Called when parameters change
    await RefreshDataAsync();
}

// ✅ Use this for first render only
protected override async Task OnAfterFirstRenderAsync()
{
    // Called only once, after first render
    await JSRuntime.InvokeVoidAsync("initializeComponent");
}
```

**Why use enhanced lifecycle methods?**
- ✅ Automatic exception handling (won't crash the app)
- ✅ Exceptions displayed to users via SnackBar or modal
- ✅ Logged for debugging
- ✅ Prevents error boundary from being triggered

#### WrapHandled for Event Handlers

Use `WrapHandled` to wrap event handlers for automatic exception handling:

```xml
<!-- ✅ CORRECT: Wrapped event handlers -->
<BitButton OnClick="WrapHandled(HandleClick)">Click Me</BitButton>
<BitButton OnClick="WrapHandled(async () => await SaveAsync())">Save</BitButton>
<BitTextField OnChange="WrapHandled((string value) => model.Name = value)" />

<!-- ❌ INCORRECT: Unwrapped event handlers -->
<BitButton OnClick="HandleClick">Click Me</BitButton>
<BitButton OnClick="async () => await SaveAsync()">Save</BitButton>
```

**Real-world example from ProductPage:**
```xml
<BitButton AutoLoading OnClick="WrapHandled(Buy)">
    @Localizer[nameof(AppStrings.Buy)]
</BitButton>
```

### 5.2 AppPageBase

Located at `/src/Client/Boilerplate.Client.Core/Components/Pages/AppPageBase.cs`

**Pages inherit from AppPageBase**, which extends `AppComponentBase` with page-specific features:

```csharp
public abstract partial class AppPageBase : AppComponentBase
{
    [Parameter] public string? culture { get; set; }

    // Validates culture parameter and redirects to 404 if invalid
    // Supports multi-language routing
}
```

**When to use:**
- ✅ Use `AppPageBase` for **pages** (routable components with `@page` directive)
- ✅ Use `AppComponentBase` for **regular components** (non-routable)

**Example:**
```csharp
// Page component
public partial class ProductsPage : AppPageBase
{
    // ...
}

// Regular component
public partial class ProductCard : AppComponentBase
{
    // ...
}
```

---

## 6. Best Practices Summary

### ✅ DO:
1. **Use the three-file structure**: Separate `.razor`, `.razor.cs`, and `.razor.scss` files
2. **Use Bit.BlazorUI components**: Prefer `BitButton` over `<button>`, `BitText` over `<span>`, etc.
3. **Use theme color variables**: Always use `$bit-color-*` variables in SCSS and `BitColor` enum in C#/Razor
4. **Use `::deep` selector**: When styling child Bit.BlazorUI components
5. **Use `WrapHandled()`**: Wrap all event handlers for automatic exception handling
6. **Use enhanced lifecycle methods**: `OnInitAsync()`, `OnParamsSetAsync()`, `OnAfterFirstRenderAsync()`
7. **Use `PageUrls` constants**: For type-safe navigation and routing
8. **Inherit from base classes**: `AppPageBase` for pages, `AppComponentBase` for components
9. **Use `[AutoInject]`**: For dependency injection in components
10. **Pass `CurrentCancellationToken`**: To all async service calls

### ❌ DON'T:
1. **Don't hardcode colors**: Never use `#ffffff`, `rgb(255,0,0)`, etc.
2. **Don't use `@code` blocks**: Put logic in `.razor.cs` code-behind files
3. **Don't use standard lifecycle methods**: Use enhanced versions instead
4. **Don't use unwrapped event handlers**: Always use `WrapHandled()`
5. **Don't use magic string URLs**: Use `PageUrls` constants
6. **Don't forget `::deep`**: When styling Bit.BlazorUI components

---

## 7. Quick Reference

### Component Lifecycle
```csharp
protected override async Task OnInitAsync()
{
    // Runs once when component initializes
}

protected override async Task OnParamsSetAsync()
{
    // Runs when parameters change
}

protected override async Task OnAfterFirstRenderAsync()
{
    // Runs once after first render (client-side only)
}
```

### Event Handler Patterns
```xml
<!-- Sync method -->
<BitButton OnClick="WrapHandled(HandleClick)">Click</BitButton>

<!-- Async method -->
<BitButton OnClick="WrapHandled(HandleClickAsync)">Click</BitButton>

<!-- Inline sync -->
<BitButton OnClick="WrapHandled(() => counter++)">Increment</BitButton>

<!-- Inline async -->
<BitButton OnClick="WrapHandled(async () => await SaveAsync())">Save</BitButton>

<!-- With parameter -->
<BitTextField OnChange="WrapHandled((string value) => model.Name = value)" />
```

### Navigation Patterns
```csharp
// Simple navigation
NavigationManager.NavigateTo(PageUrls.Products);

// Navigation with parameter
NavigationManager.NavigateTo($"{PageUrls.Product}/{productId}");

// Navigation with query string
NavigationManager.NavigateTo($"{PageUrls.Products}?category=cars");

// Force reload
NavigationManager.NavigateTo(PageUrls.Home, forceLoad: true);
```

### Styling Patterns
```scss
@import '../../Styles/abstracts/_bit-css-variables.scss';
@import '../../Styles/abstracts/_media-queries.scss';

.my-component {
    background-color: $bit-color-background-primary;
    color: $bit-color-foreground-primary;
    
    @include lt-md {
        flex-direction: column;
    }
}

::deep {
    .bit-button {
        border-radius: 8px;
    }
}
```

---