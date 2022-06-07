# Getting Started

This document aimed to create and run a Bit-Platform project in a short period. It is assumed that you, as the developer, are familiar with the development prerequisites that follow.

## Development prerequisites

- C# as the main development language.
- [Asp.net core blazor](https://docs.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-6.0) as main development Back-End and Fron-End framework
- [CSS ](https://www.google.com/url?sa=t&amp;rct=j&amp;q=&amp;esrc=s&amp;source=web&amp;cd=&amp;cad=rja&amp;uact=8&amp;ved=2ahUKEwji-KOu0pj4AhWwm_0HHeZQDzoQFnoECAgQAQ&amp;url=https%3A%2F%2Fwww.w3schools.com%2Fcss%2F&amp;usg=AOvVaw0Xtbw_GBAChsgvZNkPLVGb)&amp; [Sass ](https://www.google.com/url?sa=t&amp;rct=j&amp;q=&amp;esrc=s&amp;source=web&amp;cd=&amp;cad=rja&amp;uact=8&amp;ved=2ahUKEwjvgoO60pj4AhUCi_0HHVmXBMkQFnoECAgQAQ&amp;url=https%3A%2F%2Fsass-lang.com%2F&amp;usg=AOvVaw0p_IRgLEbIPRGWtlW7Wph8)as stylesheet
- [Ef Core](https://docs.microsoft.com/en-us/ef/core/) as ORM to communicate with the database
- [Asp.Net Identity](https://docs.microsoft.com/en-us/aspnet/identity/overview/getting-started/introduction-to-aspnet-identity) with [JWT ](https://www.c-sharpcorner.com/article/jwt-authentication-and-authorization-in-net-6-0-with-identity-framework/)supporting for handling Authentication

## Pre Requirements tools

- Microsoft Visual Studio 2022 - Preview Version 17.3.0 Preview 1.0 or higher
- Asp.net and web development VS workload
- .Net Multi-Platform App UI development (for mobile app development target) VS workload

## Create project

Clone the [TodoTemplate](https://github.com/bitfoundation/bitplatform/tree/develop/src/Tooling/Bit.Tooling.Templates/TodoTemplate) from the Bit-Platform Github repository

# Configure The Project

## Restore NuGet packages

If automatically not started click on the &quot;Restore NuGet packages&quot; item from right-click menu of solution name in solution explorer windows.

## Database

**Connection String**

Open  **appsettings.json**  in  **TodoTemplate.Api**  project and change the  **SqlServerConnection ** connection string if you want:

&quot;ConnectionStrings&quot;: {

    "SqlServerConnection": "Data Source=.; Initial Catalog=TodoTemplateDb;Integrated Security=true"

}

## Migration

We have two options to create and migrate the database to the latest version.

1. Entity Framework Migration Command

You can use Entity Framework&#39;s built-in tools for migrations. Open  **Package Manager Console**  in Visual Studio set  **TodoTemplate.Api**  as the  **Default Project**  and run the  **Update-Database**  command as shown below:

    Update-Database -Context TodoTemplateDbContext

**2. Migrator Application**

Bit-Platform includes a **TodoTemplate.Migrator**  project in the solution. You can run this tool for database migrations on development and production platforms.

All things you need is to set this project as a startup in solution and run it.