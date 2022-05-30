# Data Access

You can use Bit Data Access components, or you can use your own preferred way to read/manipulate data. But why you might choose Bit data access anyway?

1- True async support. Application with async code gets scaled better, but what does this mean to you? Each server side app has limited workers, and your app works because they work. Workers count is limited, so you've to use them carefully. When a request comes to your web api action, and you get data from database using entity framework \(For example\), your worker \(your valuable worker\) waits until database returns data. But that wait is useless \(database has its own workers\). If you use async-await, your worker handles other requests instead of waiting for a database.

2- True cancellation token support. There is a CancellationToken in every web api action you develop. If user/operator closes its browser, or if you cancel request at client side programmatically, that cancellation token gets notified. Almost all bit platform's methods accept cancellation token, and they stop their work as cancellation token gets notified.

3- Bit Data Access components are optimized for N-Tier app development. To have a better understanding about what does this mean read this [amazing article](/docs/blog/optimized-entity-framework-for-n-tier-apps.md)

## Entity Platform

Getting started: \(Sample can be found [here](https://github.com/bitfoundation/bitplatform/tree/master/Samples/DataAccessSamples/)\)

At first, you've to develop your entities. Use IEntity interface to mark your classes as entity. It's just a marker and has no member to implement.

```csharp
public class Customer : IEntity
{
}
```

Then develop a DbContext class which inherits from EfDbContextBase. The reason is described [in an article we've previously mentioned](/docs/blog/optimized-entity-platform-for-n-tier-apps.md)

```csharp
public class MyAppDbContext : EfDbContextBase
{
    public MyAppDbContext()
        : base(DefaultAppEnvironmentProvider.Current.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"))
    {
        // This constructor is needed for migrations/initializers etc.
    }

    public MyAppDbContext(IAppEnvironmentProvider appEnvironmentProvider, IDbConnectionProvider dbConnectionProvider)
        : base(appEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"), dbConnectionProvider)
    {
        // This constructor has better performance, provides implicit unit of work, etc. And it is automatically used by bit platform while processing requests etc.
    }

    public virtual DbSet<Customer> Customers { get; set; }
}
```

App environment provider provides you a way to access your configuration. By default, it reads configs from environments.json file. You don't have to use that, your db context just needs a connection string, read it from app environment provider, asp.net core's app settings, app/web config files or write connection string there directly!

See environments.json file

```javascript
"Configs": [
    {
        "Key": "AppConnectionString",
        "Value": "Data Source=.;Initial Catalog=MyAppDb;Integrated Security=True;"
    }
]
```

Then create a repository class for your project as following:

```csharp
public class MyAppRepository<TEntity> : EfRepository<TEntity>
    where TEntity : class, IEntity
{
    // You can override Repository methods here. For example you can override AddAsync
}
```

In Web API classes, use that as following:

```csharp
public class CustomersController : ApiController
{
    public virtual IRepository<Customer> CustomersRepository { get; set; } // property injection

    // You can also inject DbContext here, which is not recommended.

    [Route("customers/get-customers")]
    public virtual async Task<List<Customer>> GetCustomers(CancellationToken cancellationToken)
    {
        return await (await CustomersRepository.GetAllAsync(cancellationToken)).ToListAsync(cancellationToken);
    }

    public virtual async Task AddNewCustomer(CancellationToken cancellationToken, Customer customer)
    {
        await CustomersRepository.AddAsync(customer, cancellationToken);
        // You can call Add/Remove/Update as many as you want. No need to call SaveChanges, we save everything if no issue is found during processing this request. (Implicit unit of work)
    }
}
```

To develop a custom repository, use following codes:

```csharp
public interface IOrdersRepository : IRepository<Order>
{
    Task<long> GetOrdersCount(CancellationToken cancellationToken);
}

public class OrdersRepository : MyAppRepository<Order>, IOrdersRepository
{
    public virtual async Task<long> GetOrdersCount(CancellationToken cancellationToken)
    {
        // Note that GetAllAsync returns you a query, it does not return all data. So following code has this sql as its equivalent: select count_big(*) from Orders
        return await (await GetAllAsync(cancellationToken)).LongCountAsync(cancellationToken);
    }
}
```

These are AppStartup registrations:

```csharp
dependencyManager.Register<IDbConnectionProvider, DefaultDbConnectionProvider<SqlConnection>>(); // Uses Sql connection
dependencyManager.RegisterEfDbContext<MyAppDbContext>(); // Registers db context class
dependencyManager.RegisterAppEvents<MyAppDbContextInitializer>(); // App event to initialize db context at startup. We recommend you to use Entity framework migrations instead.
dependencyManager.RegisterRepository(typeof(MyAppRepository<>).GetTypeInfo()); // You can inject IRepository<Customer> or IRepository<any class you want>
dependencyManager.RegisterRepository(typeof(OrdersRepository).GetTypeInfo()); // It registers custom orders repository
```

## Entity Framework Core

It's as like as Entity Framework, with two differences:

1- We register entity framework core by following:

```csharp
dependencyManager.RegisterEfCoreDbContext<MyAppDbContext, SqlDbContextObjectsProvider>();
```

2- MyAppDbContext has following codes:

```csharp
public MyAppDbContext()
    : base(new DbContextOptionsBuilder().UseSqlServer(DefaultAppEnvironmentProvider.Current.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString")).Options)
{
    // This one is need for migrations/initializers etc.
}

public MyAppDbContext(IAppEnvironmentProvider appEnvironmentProvider, IDbContextObjectsProvider dbContextCreationOptionsProvider)
        : base(appEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"), dbContextCreationOptionsProvider)
{
     // This constructor has better performance, provides implicit unit of work, etc. And it is automatically used by bit platform while processing requests etc.
}
```

If you've got a complex scenario, simply drops us an [issue on github](https://github.com/bitfoundation/bitplatform/issues) or ask a question on [stackoverflow](https://stackoverflow.com/questions/tagged/bit-framework).

## Bit repository specific methods

Bit repository has several methods such as GetAll, Add, Remove etc as like as any other repository you might know, but it has following methods you can't find in other repositories:

LoadCollection - LoadReference - GetCollectionQuery

By reading the article which describes [why bit repository is optimized for N-Tier scenarios](/docs/blog/optimized-entity-framework-for-n-tier-apps.md), you'll find out we disable "property based" lazy loading by default which improves your app performance from 3 times to 100 times based on a scenario. But you can perform explicit loading as followings:

```csharp
[Route("customers/customer-explicit-sample")]
public virtual async Task CustomerLazySample(CancellationToken cancellationToken)
{
    Customer customer = await CustomersRepository.GetByIdAsync(1, cancellationToken);

    // After some works, now we need customer's orders. So we simply write:

    await CustomersRepository.LoadCollectionAsync(customer, c => c.Orders, cancellationToken); // Uses async-await + cancellation token for explicit loading! And it has no performance penalty (-:

    foreach (Order order in customer.Orders)
    {

    }
}
```

LoadReference is similar to LoadCollection, but you call it when you intend to load reference properties.

```csharp
public class Category : IEntity
{ 
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Product : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Category Category { get; set; }
    [ForeignKey(nameof(Category))]
    public int CategoryId { get;set; }
}


Product product = await ProductsRepository.GetByIdAsync(1, cancellationToken);

await ProductsRepository.LoadReferenceAsync(product, p => p.Category);

// product.Category is now retrived from database.
```

ToDo: GetCollectionQuery returns IQueryable, for example, IQueryable of orders of that customer.

