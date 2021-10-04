# Blazor Best Practice with BlazorDualMode 

**Blazor** is a framework provided by Microsoft that allows us to write interactive web applications in C# without using JavaScript. Blazor is Component-based and also allows code sharing between client and server in applications written in backend with C# (for example with ASP.NET Core).
Blazor Architecture is a modern architecture that makes good use of C# features to develop the project in a highly maintainable manner. Blazor, by its very nature, also allows native mobile applications to be written, and several demos for proof of concept have been presented to prove this point; But until today, the feasibility of being production-ready has not been provided.

Blazor works in two ways; **Blazor Server** and **Blazor Client**.

In Blazor Client, using Web Assembly with nearly 90% of browsers support, which allows non-JavaScript code to be executed in the browser, DLLs with HTML, CSS, images, and other web assets are downloaded first and the program works as a Single Page App (SPA). In this model, you can also use the pre-rendering or SSR technique to provide a better user experience or better SEO.
Blazor on the client-side supports .NET Standard 2.1, which practically allows you to use a wide range of Nuget packages.

Blazor also has a second implementation model called Blazor Server. In this model, the code is executed completely on the server-side and the UI interactions are transmitted to or received from the client in the form of a WebSocket. Because the code runs entirely on the server, when network communication is not good, we can see some lag in different scenarios.

The future of Blazor is defined in the first model, and overtime for the Blazor Server model, not much can be imagined. But the important and interesting point is that the project code is the same in both models and only the configuration of the two is different. So, if you are interested in starting to use Blazor, one way is to start with the Blazor Server model and then switch to the Blazor Client by changing the configuration. Of course, in the meantime, you should pay attention to some points:
1- Blazor Server supports .NET 5 and Blazor Client supports .NET Standard 2.1. In certain cases, there may be a class or method in .NET 5 which not be in .NET Standard 2.1.
2- In Blazor Server you can even connect to SQL Server with Entity Framework Core, as an example; But naturally Browser will not allow you to connect directly to SQL Server in the Client model.
These points mean that if you start the project with Blazor Server, you may do things that work in Blazor Server but do not work in Blazor Client, therefore you may not be able to switch easily and simply from Server model to Client model.

Ideally, you should have a project where you can simply switch between the two models by only changing a word and monitor the performance of the project in both cases. To achieve this, I have created a project called [BlazorDualMode](https://github.com/ysmoradi/BlazorDualMode) that performs most of the reviews automatically and will practically guarantee your easy migration from Blazor Server to Blazor Client in the future.
If you search the net, you will see projects with this name and alike concept; But this project has two important advantages that others lack:
1- Its Blazor Client model has Pre-rendering or SSR technique.
2- Its Api project is separate from its Blazor project. If we consider the Api project as a project that has access to DbContext and database along with Model, Data, etc., the separation of the Blazor project causes that even in its server model, we have to retrieve the data from the Api project as a REST API call. And through the Shared project between Api and Blazor, we can finally see the DTOs and other shared codes

Of course, in the DualMode project, only the Api project has been created, and we have nothing to do with its details, such as whether we want CQRS or not, whether we want to work with layers, and so on, and we only focus on Blazor-related issues.

In the project we have a file called Directory.build.props. When such a file is placed in a folder, all the items written in it are implicitly applied to all the csproj files in its subset.
In this file we have:
 
    <Project>
      <PropertyGroup>
        <BlazorMode>Client</BlazorMode>
        <DefineConstants Condition=" '$(BlazorMode)' == 'Client' ">$(DefineConstants);BlazorClient</DefineConstants>
        <DefineConstants Condition=" '$(BlazorMode)' == 'Server' ">$(DefineConstants);BlazorServer</DefineConstants>
        <LangVersion>preview</LangVersion>
      </PropertyGroup>
    </Project>


First, we define a variable, called BlazorMode, which can be either a Server or a Client, which is currently a Client and you can easily change it.
In the following, we have defined Constant, which allows us to condition the C# code included in the Blazor Project configuration. 
For example, write:

    #if BlazorClient
    ...
    #elif BlazorServer
    ...
    #endif

These `if` conditions that start with # are checked during compile time, and code that is set to true, is compiled and placed at the output of the program, and code that is set to false is not compiled from scratch.

The **BlazorDualMode.Api** project is the project in which the API Controllers are located and also presents or serves the project to browsers in Blazor Client mode. In fact, in this model, a request that does not reach any API Controller is delivered to Blazor. Blazor first looks for a component written for the corresponding route and executes it, and then the response is ready and sent to the client. Next, the browser downloads the DLL files and the program continues to work as a Single Page App.

The **BlazorDualMode.Shared** project is a project that shares code between Api and Blazor. For example, you can put DTOs in this field.

The **BlazorDualMode.Web** project is a project that includes Blazor components. In Server mode, this project can also be run, and you must run the Web and Api project simultaneously with the features of your desired Visual Studio or IDE for it to work properly.
 
The Program.cs, Startup.cs files, as well as the csprojs themselves, and finally the Host.cshtml file, are the ultimate difference between the two modes, and the project business code and even the Components and API Controllers are the same in both modes. The configuration is handled with the `if server` or `if client` condition, and understanding the details of the settings requires mastering Blazor itself, which is beyond the scope of this post.


