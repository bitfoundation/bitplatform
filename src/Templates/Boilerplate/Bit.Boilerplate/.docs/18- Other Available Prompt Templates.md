# Stage 18: Other Available Prompt Templates

This project includes several specialized prompt templates designed to help you with specific development tasks. Each prompt is carefully crafted to follow the project's conventions and best practices.

## Available Prompts

### 1. Scaffold Prompt (`scaffold.prompt.md`)

**File Location**: `.github/prompts/scaffold.prompt.md`

**What it does**: Generates a complete CRUD (Create, Read, Update, Delete) implementation for a new entity in your project, including all necessary layers from database to UI.

**When to use it**: When you need to add a new data entity to your application with full CRUD functionality.

**Key capabilities**:
- Creates Entity Type Configuration for EF Core
- Generates DTO (Data Transfer Object) with validation attributes
- Creates Mapper using Mapperly for high-performance object mapping
- Generates API Controller with OData support
- Creates IAppController Interface for strongly-typed HTTP client
- Adds Resource Strings to AppStrings.resx for localization
- Creates Data Grid Page for listing records
- Creates Add/Edit Page for creating and updating records
- Integrates with navigation (PageUrls.cs, NavBar, NavPanel)
- Updates AppJsonContext for JSON serialization
- Generates EF Core migration

**Example usage**: "Run scaffold.prompt.md to create a Product entity with Name, Description, Price, and CategoryId properties"

---

### 2. Resx Prompt (`resx.prompt.md`)

**File Location**: `.github/prompts/resx.prompt.md`

**What it does**: Automatically identifies hardcoded strings in your code and moves them to resource files (.resx) for proper localization support.

**When to use it**: When you have hardcoded user-facing text in your Blazor components, pages, or controllers that should be localized to support multiple languages.

**Key capabilities**:
- Identifies hardcoded user-facing strings in selected code
- Adds new entries to `AppStrings.resx` with appropriate resource keys
- Generates strongly-typed resource classes
- Updates code to use `IStringLocalizer<AppStrings>` pattern
- Uses `nameof(AppStrings.ResourceKey)` for type-safe resource access
- Preserves string formatting with placeholders (e.g., `{0}`, `{1}`)
- Follows naming conventions with descriptive resource keys

**What it won't move**:
- CSS class names or IDs
- Configuration keys
- API endpoints or URLs
- Technical constants (file extensions, mime types)
- Log messages

**Example usage**: "Run resx.prompt.md on the Dashboard.razor file to move all hardcoded strings to resource files"

---

### 3. Bitify Prompt (`bitify.prompt.md`)

**File Location**: `.github/prompts/bitify.prompt.md`

**What it does**: Modernizes your Blazor pages by replacing standard HTML elements and custom CSS with Bit.BlazorUI components and theme-aware styling.

**When to use it**: When you have pages using generic HTML elements (like `<button>`, `<input>`, `<div>`) and want to upgrade them to use the Bit.BlazorUI component library for consistency, better UX, and theme support.

**Key capabilities**:
- Analyzes current HTML markup and identifies replaceable elements
- Creates a modernization inventory of all HTML elements that can be replaced
- Uses DeepWiki to research appropriate Bit.BlazorUI components
- Replaces HTML elements with proper Bit.BlazorUI components:
  - `<button>` → `BitButton`, `BitActionButton`
  - `<input type="text">` → `BitTextField`
  - `<select>` → `BitDropdown`
  - `<input type="checkbox">` → `BitCheckbox`
  - `<input type="radio">` → `BitChoiceGroup`
  - `<table>` → `BitDataGrid`
  - Generic divs → `BitStack`, `BitCard`
  - Navigation → `BitNavBar`, `BitBreadcrumb`, `BitNav`
  - Lists → `BitBasicList`
- Converts custom CSS to theme-aware styling using `$bit-color-*` variables
- Uses `::deep` selectors for proper component styling
- Updates event handlers to use `WrapHandled` pattern
- Ensures light/dark theme compatibility

**Workflow steps**:
1. Analyzes `.razor`, `.razor.cs`, and `.razor.scss` files
2. Creates inventory of replaceable elements (form, layout, navigation, data display)
3. Researches specific Bit.BlazorUI components via DeepWiki
4. Implements replacements with proper properties and events
5. Converts styling to theme-aware approach using `$bit-color-*` variables and `::deep` selectors
6. Updates code-behind for component-specific logic
7. Verifies the build succeeds

**Example usage**: "Run bitify.prompt.md on the UserProfile.razor page to replace all HTML elements with Bit.BlazorUI components"

---

## How to Use These Prompts

To use any of these prompts, simply reference them in your conversation with GitHub Copilot:

**Examples**:
- "Run scaffold.prompt.md to create an Order entity with OrderDate, TotalAmount, CustomerId, and Status"
- "Run resx.prompt.md on all files in Components/Pages/Dashboard/"
- "Run bitify.prompt.md on the Settings page"

Each prompt will guide you through its specific workflow, following the project's conventions and best practices automatically.

---

## Important Notes

⚠️ **DeepWiki Tool Requirement**: The `bitify.prompt.md` and general development workflows require access to the `DeepWiki_ask_question` tool for researching Bit.BlazorUI components and bitplatform features. If this tool is not available, some prompts may not function optimally.

💡 **Prompt Customization**: These prompts are designed to be comprehensive starting points. You can always provide additional context or specific requirements when using them to customize the output to your needs.

🔧 **Build Verification**: All prompts emphasize building the project after changes to ensure compilation success and catch any issues early.

---