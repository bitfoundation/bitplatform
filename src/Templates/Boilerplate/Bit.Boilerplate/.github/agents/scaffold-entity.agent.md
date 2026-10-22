---
name: scaffold-entity
description: Scaffolds a complete CRUD entity across every layer of the Boilerplate solution - entity model, EF Core configuration and migration, DTO, Mapperly mapper, API controller and its shared interface, Blazor grid and add/edit pages, navigation, permission and tests.
---

# Scaffold an entity with full CRUD

The result is a new entity the user can list, add, edit and delete, wired through every layer the same way as the entities this project already ships, and covered by tests.

## Is the shape of the work known?

This skill scaffolds a shape you already know. If you were invoked directly and the request is anything more than an entity whose fields, rules and screens are already settled - unclear or negotiable fields, business rules beyond CRUD, anything touching other features - **stop and run the `ai-dlc` skill (`.github/agents/ai-dlc.agent.md`) first**, through its requirements and planning phases. Come back to this file once the entity is pinned down, and scaffold it as a task of that plan.

Do not scaffold a guess: every file this skill writes is one more thing to unpick when the requirements turn out differently.

## Start from a working entity

A copy of an entity that already works here, renamed and reshaped, beats code written from a description. Follow the one below through every layer, then build the new one the same way.

<!--#if (module == "Admin")-->
- `Category` is the small shape: a grid page and a modal that adds and edits a row. `Product` is the large one: a grid page and an add/edit page of its own, with a rich text field and an image upload. Each lives in `Features/Categories` or `Features/Products` of the server API and of Shared, in `Pages/Categories` or `Pages/Products` of Client.Core, and in the same folders under `src/Tests/Features`.
<!--#elif (sample == true || offlineDb == true)-->
- `TodoItem` (`Features/Todo` of the server API and of Shared) is the entity this project ships, but it has no grid. For the grid and the add/edit form, read the Admin module's `Category` (a modal) and `Product` (a page of its own) with the bit platform source code tools - `FindBitPlatformSymbols`, `SearchBitPlatformCode` and `GetBitPlatformSymbolSource`, scoped with `path_filter: src/Templates`. They read the template's own code, never this workspace.
<!--#else-->
- This project ships no CRUD entity. Read the Admin module's `Category` (a grid and an add/edit modal) and `Product` (a grid and an add/edit page of its own) with the bit platform source code tools - `FindBitPlatformSymbols`, `SearchBitPlatformCode` and `GetBitPlatformSymbolSource`, scoped with `path_filter: src/Templates`. They read the template's own code, never this workspace, and the Admin module's conditional directives mark what belongs to it.
<!--#endif-->
- Unless the request already settles it, ask the user whether rows are added and edited in a modal, which suits a few simple fields, or on a page of their own, which suits many fields, rich text, uploads or a complex form.

## What a finished entity has

- **Entity and configuration** in the server API: `Id` and a `long Version` concurrency token, an EF Core configuration for its indexes and relationships, and a `DbSet` on `AppDbContext`. Configurations are picked up from the assembly, so they need no registration.
<!--#if (multitenant == true)-->
- **Tenant scope**: the entity implements `ITenantAware`, which makes `AppDbContext` filter its rows to the current tenant and stamp the tenant on save. An entity without it is readable and writable across every tenant. The only exceptions are an entity that is deliberately global, or one scoped by another owner (as `TodoItem` is scoped per user); say which in the plan, and put the ownership term in every query, since nothing else adds it.
<!--#endif-->
- **Migration**, added with the command in `AGENTS.md` section 4. Never edit a migration the project already has.
- **DTO** in Shared, with `[DtoResourceType(typeof(AppStrings))]`, registered in `AppJsonContext`. Validation and display attributes are the one place a new `AppStrings.resx` key is required, since attributes take compile-time constants.
- **Who writes each property.** One the client may read but the server owns - the owner, timestamps, totals, a status only a workflow moves - is ignored by the mapper on the way in, with `[MapperIgnoreTarget]`, so a crafted request cannot set it. When what the client reads and what it writes differ a lot, split the DTO into a request and a response DTO instead of piling up ignores; the request DTO keeps `Id` and `Version`, or updates lose their concurrency check.
- **Mapper** with `Project()` for queries and `Map()` and `Patch()` for writes.
- **Controller and shared interface.** The controller derives from `AppControllerBase`, implements the interface, and carries `[ApiVersion(1)]` with the versioned route. The interface keeps the literal `api/v1/...` route the others use: the generated client has no API version to substitute, so the two are not unified. Queries go through `Project()`; a missing row is a `ResourceNotFoundException`; every method takes a `CancellationToken`. On the interface, an action that returns `IQueryable<T>` is declared as returning a list with a `default!` body, an `IActionResult` one names its type with `Produces<T>`, and `ODataQueryOptions` parameters are left out.
- **Permission.** Decide who may manage the entity. A new permission goes into `AppFeatures` with a short, unique value, since it travels in every access token, and a description, which the roles page shows. The controller's policy, the shared interface's `[AuthorizedApi]`, the page and its menu item all use it.
- **Pages** in Client.Core: a grid page and the add/edit modal or page.
- **Navigation**: the page's URL in `PageUrls`, and a menu item wherever the existing entities have one, shown only to users with the permission.
- **Tests** under `src/Tests` for what this entity adds beyond the copy: the form's validation, and every rule the server enforces, such as who may change a row or what blocks a delete.

## Done means

- The solution builds, and the migration applies to a fresh database.
- In the running app, a seeded user with the permission can list, add, edit and delete rows; saving a row someone else changed in the meantime is refused; a user without the permission sees neither the menu item nor the data.
<!--#if (multitenant == true)-->
- A user of another tenant sees none of the rows.
<!--#endif-->
- The new tests pass, and so does every test that passed before.
