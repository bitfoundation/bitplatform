# Optimized entity framework for N-Tier applications

Using Entity Framework with "ASP.NET Web API and/or MVC" is a common thing in .NET based projects. Most developers use entity framework with its default configuration. Entity framework has several features such as **Property based** Lazy loading, **automatic** change tracking etc. But every feature comes with a cost and unfortunately these features are not useful in most scenarios in N-Tier apps.

Consider a web api which returns a list of active customers from the database. With or without a repository, an entity framework's db context gets created. Then it runs a query against a database, creates an instance of the customer for each record of database result set. Then db context gets disposed. After that, [Json.NET](http://www.newtonsoft.com/json) returns json created from customers list.

When user/operator changes any of those customers in a web app or mobile app, we send changes to the server and a "new" instance of customer gets created based on that. We save that customer using "newly" created db context. So in this sample, there was no need to automatic change tracking to get active customers first.

This sample is a common thing in N-Tier world, when **most changes are made in other tiers such as browsers and mobile devices**.

In bit framework, we disable entity framework features by default. Features like "Property based Lazy loading (The lazy loading itself works properly)", "Automatic Change Tracking", "Proxy creation" etc. Bit repository, on the other hand, calls "AsNoTracking" on all your queries.

We've developed roslyn analyzers to inform you when you're doing something wrong while you're working with db context too. See below:

![](/assets/EntityFrameworkAsNoTrackingRoslynAnalyzer.PNG)

[You can see what we've done to entity framework's configuration here](https://github.com/bit-foundation/bit-framework/blob/master/src/Server/Bit.Data.EntityFramework/Implementations/EfDbContextBase.cs#L37-L42)

But when you apply those configurations, most repositories won't work properly as they are developed/tested mostly based on default entity framework's configuration. This is why we developed a new repository in bit framework instead of using any of existing repository libraries. (It has other important reasons although, such as true async support etc)

You're free to use your preferred repository, but let's take a look at some benchmarks: [You can find codes here](https://github.com/bit-foundation/bit-framework/tree/master/docs/src/EntityFrameworkOptimizedForNTierScenarios)

``` ini
BenchmarkDotNet=v0.10.9, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), ProcessorCount=8
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2101.1
```

 | Method             | Mean        | Error      | StdDev    |
 |--------------------|-------------|------------|-----------|
 | Return Empty list  | 505.5 μs    | 8.637 μs   | 8.079 μs  |
 | BitRepository      | 20.00 ms    | 0.3654 ms  | 0.3418 ms |
 | SharpRepository    | 1,965.27 ms | 38.8008 ms | 65.887 ms |

 
 [This script](https://github.com/bit-foundation/bit-framework/blob/master/docs/src/EntityFrameworkOptimizedForNTierScenarios/EntityFrameworkOptimizedForNTierScenarios/CreateTestDatabaseScript.sql) creates a database which has 10.000 customers, and each customer has 3 orders. As you can see in benchmarks, returning empty list is very fast. It's all about **micro seconds**. BitRepository has Mean with value (20.00 ms) which is acceptable as it's returning whole 10.000 customers in every request. And finally, you see Sharp repository's result which is about seconds! "It's not because of SharpRepository itself", it is because of the default configuration of entity framework.

 Default entity framework configuration slows down your app because of its proxy creation (required for property based lazy loading). Proxy creation is a slow process. Property based lazy loading slows down your app too, because when you return 10 customers to a client, it sends 10 queries to the database to retrieve their orders! This is a known problem called **N+1 problem**. Automatic change tracking which is not useful in this scenario is another reason for slowness.
 
 _________

Ninja tip: [Here you can learn how to do lazy loading withour slowing down your app + support for async lazy loading!](https://docs.bit-framework.com/docs/bit-server-side/data-access.html#bit-repository-specific-methods) We've a far better lazy loading support in comparision of property based lazy loading
