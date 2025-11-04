# Bitify: Replace raw html/css with Bit.BlazorUI components

You are an expert at modernizing Blazor pages by replacing standard HTML elements and custom CSS with Bit.BlazorUI components and theme-aware styling.

*   **🚨 CRITICAL TOOL REQUIREMENT**: You **MUST** verify that you have access to the `DeepWiki_ask_question` tool. If this tool is NOT available in your function list, you **MUST** immediately display the following error message:
**❌ CRITICAL ERROR: DeepWiki_ask_question Tool Not Available**

## Instructions

1. **Analyze the current page** - Examine the `.razor`, `.razor.cs`, and `.razor.scss` files to identify HTML elements and custom styles that can be replaced with Bit.BlazorUI components
2. **Create a modernization inventory** - List all HTML elements, CSS classes, and functionality that could benefit from Bit.BlazorUI components
3. **Research specific Bit.BlazorUI components** - Use DeepWiki to find components that match your identified needs
4. **Create a targeted replacement plan** - Map HTML elements to appropriate Bit.BlazorUI components based on research
5. **Replace HTML with Bit.BlazorUI components** - Update the Razor markup using proper component syntax
6. **Convert custom CSS to theme-aware styling** - Replace hardcoded colors and styles with `BitColor` theme variables and `::deep` selectors
7. **Update component logic** - Modify the code-behind to work with Bit.BlazorUI component properties and events
8. **Verify and build** - Ensure the page compiles and functions correctly

## Context

- **Primary UI Library**: `Bit.BlazorUI` - Must be used instead of generic HTML elements
- **Styling Approach**: Use `.razor.scss` files with `::deep` selectors and `BitColor` theme variables

## Workflow

### Step 1: Analyze Current Implementation
First, thoroughly examine the current page to understand what can be modernized:

1. **Examine the `.razor` file**:
   - Identify all HTML elements (`<div>`, `<button>`, `<input>`, `<form>`, `<table>`, etc.)
   - Note any custom HTML attributes and event handlers
   - Look for layout structures and navigation elements
   - Document data binding patterns and component references

2. **Review the `.razor.cs` file**:
   - Understand current event handling logic
   - Note any DOM manipulation or JavaScript interop
   - Identify data binding properties and validation logic
   - Look for component lifecycle methods

3. **Analyze the `.razor.scss` file**:
   - Identify hardcoded colors that should use theme variables
   - Note custom styling that might be replaced by component features
   - Look for responsive design patterns
   - Document CSS classes that style interactive elements

### Step 2: Create Modernization Inventory
Based on your analysis, create a categorized list of replaceable elements:

**Form Elements to Replace:**
- Text inputs (`<input type="text">`) → `BitTextField`
- Dropdowns/selects (`<select>`) → `BitDropdown`
- Checkboxes (`<input type="checkbox">`) → `BitCheckbox`
- Radio buttons (`<input type="radio">`) → `BitChoiceGroup`
- Buttons (`<button>`) → `BitButton`, `BitActionButton`
- etc.

**Layout Elements to Replace:**
- Generic divs with flexbox → `BitStack`
- Grid layouts → `BitGrid`
- Card-like containers → `BitCard`
- etc.

**Navigation Elements to Replace:**
- Custom navigation bars → `BitNavBar`
- Breadcrumbs → `BitBreadcrumb`
- Menu systems → `BitNav`
- etc.

**Data Display Elements to Replace:**
- Tables → `BitDataGrid`
- Lists → `BitBasicList`
- etc.

**Styling to Modernize:**
- Hardcoded colors → `$bit-color-*` variables
- Custom component styles → `::deep` selectors
- Theme-incompatible CSS → Theme-aware alternatives

### Step 3: Research Specific Bit.BlazorUI Components
Now that you know what needs to be replaced, research the specific components:

For each category of elements you identified, ask targeted DeepWiki questions:
```
"What Bit.BlazorUI components can replace [specific HTML elements from your inventory] and what are their key properties, events, and styling options?"
```

### Step 4: Implement Replacements
Update the markup, ensuring:
- Proper property binding (`@bind-Value`, `@bind-Text`)
- Appropriate component properties (`Variant`, `Color`, `IsEnabled`, etc.)
- Correct event handling with `WrapHandled`

### Step 5: Convert Styling
- Replace hardcoded colors with `$bit-color-*` variables from `_bit-css-variables.scss`
- Use `::deep` selectors for bit component styling
- Remove unnecessary custom CSS that components handle
- Ensure theme compatibility (light/dark mode support)

### Step 6: Update Code-Behind
- Replace HTML-specific event handling with component events
- Update data binding to work with component properties
- Remove DOM manipulation code that components handle internally
- Use component references (`@ref`) if direct access is needed

### Step 7: Build
- Build the project to ensure compilation

## Common Pitfalls to Avoid

- Avoid hardcoded colors that break theme switching
- Don't override bit component styles without using `::deep`
- Don't assume component properties match HTML attributes exactly

Now proceed to analyze the current page and implement the Bit.BlazorUI modernization following these guidelines.