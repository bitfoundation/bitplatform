# How to build bit platform

- [Projects](#projects)
- [Basic build requirements](#basic-build-requirements)
- [Projects build requirements](#projects-build-requirements)

<br/>

## Projects

bit platform consists of multiple different projects/products with the following being the most important ones:

- [bit platform website](../src/Websites/Platform/)
- [bit BlazorUI (Blazor components)](../src/BlazorUI/)
- [bit Project Templates](../src/Templates/)

building each one of them requires some specific steps that are explained below.

<br/>

## Basic build requirements

Building each of the bit platform projects needs the following basic requirements other than the specific requirements that are explained later:

- [.NET 9 SDK (9.0.102)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [Node.js](https://nodejs.org)

<br/>

## Projects build requirements

Building each bit platform project requires specific steps that are explained per project below:

<br/>

### bit platform Website
This website only requires the basic requirements and can be simply built by running the following command in the `Bit.Websites.Platform.Server` project folder:

```bash
dotnet build
```
and to run the project you just need to execute the following command in the same folder:

```bash
dotnet watch
```

<br/>

### bit BlazorUI (Blazor components)
The bit BlazorUI product has many sub-projects with the main one being the BlazorUI project itself.

#### BlazorUI project

To build the BlazorUI project you only need the basic requirements and simply run the following command in its folder (`Bit.BlazorUI` in the `src` folder):

```bash
dotnet build
```

#### BlazorUI Demo project

Another project is the bit BlazorUI `Demo` project which has two different projects (Web & App) with different requirements to build.

The `Web` project just like the bit platform website only needs the basic requirements and can be simply built by running the following command in the `Bit.BlazorUI.Demo.Client.Web` project folder (`Demo/Client/Web`):

```bash
dotnet build
```

The `App` project unlike the Web needs the `MAUI` workloads to build. This requirement can be installed with the following commands:

**Linux**:
```bash
dotnet workload install maui-android
```
**Note**: because of MAUI shortcomings, for now only the android version can be built on Linux.

**Windows & macOS**:
```bash
dotnet workload install maui
```

To build the App project run the following command in the `Bit.BlazorUI.Demo.Client.App` project folder (`Demo/Client/App`):

```bash
dotnet build
```

<br/>

### bit Project Templates
Like the bit BlazorUI Demo project, the project templates (located in the `src/Templates` folder) have two different projects (Web & App) with different requirements to build.

For example for the `Boilerplate` project template in the `Boilerplate/Bit.Boilerplate` folder:

The `Web` project just like the bit platform website only needs the basic requirements and can be simply built by running the following command in the `Boilerplate.Client.Web` project folder (`src/Client/Boilerplate.Client.Web`):

```bash
dotnet build
```

The `App` project unlike the Web needs the `MAUI` workloads too. This requirement can be installed with the following commands:

**Linux**:
```bash
dotnet workload install maui-android
```
**Note**: because of MAUI shortcomings, for now only the android version can be built on Linux.

**Windows & macOS**:
```bash
dotnet workload install maui
```

To build the App project run the following command in the `Boilerplate.Client.Maui` project folder (`src/Client/Boilerplate.Client.Maui`):

```bash
dotnet build
```
