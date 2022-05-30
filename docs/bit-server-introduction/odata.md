# OData

So, what's OData? following text which is copied from [http://odata.org](http://odata.org) describes odata well:

OData \(Open Data Protocol\) is an ISO/IEC approved, OASIS standard that defines a set of best practices for building and consuming "RESTful APIs". OData helps you "focus on your business logic" while building RESTful APIs. OData RESTful APIs are easy to consume. The OData metadata, a machine-readable description of the data model of the APIs, enables the creation of powerful generic client proxies and tools.

Using bit platform, you can build OData services very easily, and we generate C\# proxies for you automatically that you can use in Xamarin Forms & Blazor apps. We also have out of the box support for Open-API \(Swagger\). Using [azure auto rest](https://github.com/Azure/autorest) tools, you can generate client side for almost any language you want. You can also send raw http requests to odata services and you can expect raw responses.

**An OData controller has full built-in support for paging/filtering/sorting/projection/grouping and aggregation.**

At client side, you develop LINQ queries, and then OData sends that query to server side and server returns data based on your query. OData supports batch requests as well which results into better performance & scability.

In bit apps, you develop odata controllers for your DTO \(Data transfer objects\) classes.

Instead of sending your "domain models/entities" to client, you send DTO to the client. Your "model/entities" gets complicated over time based on business requirements, and at the client side you need something less complicated and easier to use. DTO \(Something similar to ViewModel in MVC\) is a common best practice in modern software development world.

**"Model/Entity" - DTO examples:**

Example 1: Consider following "models/entities":

```csharp
public class Product : IEntity
{
    public int Id { get;set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public Category Category { get; set; }
    [ForeignKey(nameof(Category))]
    public int CategoryId { get; set; }
}

public class Category : IEntity
{
    public int Id { get;set; }
    public string Name { get; set; }
}
```

You can have following DTO:

```csharp
public class ProductDto : IDto
{
    public int Id { get;set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } // We've added "Name" of category as "CategoryName" into ProductDto class. So, at client side, we can create a list of products very easily and every product has its cateogry name included.
}
```

This is called flattening.

Example 2: Consider following "models/entities":

```csharp
public class Customer : IEntity
{
    public int Id { get;set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public bool IsActive { get; set; }
    public List<Order> Orders { get; set; } = new List<Order>();
}

public class Order : IEntity
{
    public int Id { get;set; }
    public string Description { get; set; }
    public Customer Customer { get; set; }
    [ForeignKey(nameof(Customer))]
    public int CustomerId { get; set; }
}
```

You can have following DTO:

```csharp
public class CustomerDto : IDto
{
    public int Id { get;set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public bool HasOrder { get; set; } // You need this in one of your forms for example.
}
```

You can create as many as DTOs from your "models/entities". You can add calculated fields, apply flattening etc.

To follow best practices, keep these rules in your mind:

1- **Do not inherit from "models/entities" in your DTO classes.** For example, CustomerDto does not inherit from Customer.

2- **Do not declare a property with type of your "models/entities" in your DTO classes.** For example, ProductDto has no property of type Category model. Do not use models/enities enums and complex types in your dto clasess too.

3- Develop a DTO for every Task you've. For example, if you want to show customers names and their orders count, create a DTO for this task. And when you want to create a Customer registration form which accepts customer data, develop a new DTO for that task.

To send DTO to client side, you develop DtoController. [Examples can be found here](https://github.com/bitfoundation/bitplatform/tree/master/Samples/ODataSamples/).

## 1- CustomerDtoControllerSample

In Bit-OData, you develop DtoController instead of ApiController. Note that you can continue developing API controllers side by side.

So, lets take a look at new codes. First you've to configure web api & odata toghether by following code:

```csharp
dependencyManager.RegisterDefaultWebApiAndODataConfiguration(); // instead of dependencyManager.RegisterDefaultWebApiConfiguration();
```

You can use following code to add OData to your app:

```csharp
dependencyManager.RegisterODataMiddleware(odataDependencyManager =>
{
    odataDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration();
});
```

Finally add following code to your project:

```csharp
[assembly: ODataModule("MyApp" /*Use any name you prefer to use here. This is a route prefix for your odata requests.*/)]
```

In CustomersController you see these codes:

```csharp
public virtual IDtoEntityMapper<CustomerDto, Customer> Mapper { get; set; }

[Function]
public virtual async Task<IQueryable<CustomerDto>> GetActiveCustomers(CancellationToken cancellationToken)
{
    return Mapper.FromEntityQueryToDtoQuery((await CustomersRepository.GetAllAsync(cancellationToken)).Where(c => c.IsActive == true));
}
```

Mapper automatically maps "model/entities" classes to DTO classes. It uses [AutoMapper](http://automapper.org/) by default and the way we use auto mapper will not slow down your app as described [here](/docs/blog/why-auto-mapper-has-no-performance-penalty.md)

Note that you don't have to use bit repository here. You don't have to use entity framework either. You can use mongo db, simple array etc. We need some customer dto instances only.

**\[Function\] is used to return data**, it has to return data, and it accepts simple type parameters like int, string, enum, etc. **\[Action\] is used to do something**, its return value is optional, and it accepts almost anything.

Run the app and you're good to go. That's a swagger's console you see by default. By opening [http://localhost:9000/odata/MyApp/$metadata](http://localhost:9000/odata/MyApp/$metadata) you see $metadata. $metadata describes your DTO classes, complex types, enums, actions and functions in a standard format. There are tools in several languages to generate client side proxy for you. You can see the list of libraries and tools [here](http://www.odata.org/libraries/). Note that you can call OData controllers using jquery ajax, fetch, etc too, as they're REST APIs.

Try GetActiveCustomers, It runs something like this on a database:

```sql
select * from Customers inner join Cities on Id = CustomerId where IsActive = 1 /*1 means true for sql server database. It comes from your server side linq query: Where(c => c.IsActive == true))*/
```

It accepts several parameters such as $filter, $order by etc. These are standard OData parameters and they work no matter where the data is come from \(Bit repository, entity framework's db context, simple array etc\).

Try CityId eq 1 for $filter, it returns customers located in City 1 only!

It runs something like this on a database:

```sql
select * from Customers inner join Cities on Id = CustomerId where IsActive = 1 and CityId = 1
```

As you see, the filter we've developed at server side \(c =&gt; c.IsActive == true\) is **combined** with query we passed from client side \(CityId eq 1\). Filters are combined with "AND", so there is no security risk at all.

Try Id,FirstName,LastName for $select, it returns those properties of customers only!

It runs something like this on a database:

```sql
select Id,FirstName,LastName from Customers where IsActive = 1 /*true*/
```

There is no join between customers and cities as we've not requested CityName property. It's smart!

By default server returns all Dto properties and we've to provide $select if we prefer to retrive less columns. But **server won't send associations by default**.

Imaging that CustomerDto has a list of OrderDto called Orders.

```csharp
public class CustomerDto: IDto
{
    public List<OrderDto> Orders { get; set; }
    // ... the rest of other props
}
```

If you prefer to load cusotmers by their orders, you've to provide **Orders** to $expand. $expand is something similar to Include method of entity framework.

OData supports filtering, ordering, projection, paging etc in a standard way. Almost all UI vendors have support for OData in their rad components such as data grid etc. You can create amazing excel sheets and dashboard using its odata support. In C\#/TypeScript/JavaScript you're able to write LINQ queries to consume odata resources. For example:

```csharp
context.customers.GetActiveCustomers().Where(c => c.CityId == 1).ToArray(); // C#

context.customers.getActiveCustomers().filter(c => c.CityId == 1).toArray(); // JavaScript/TypeScript

// This will be converted to $fitler > CityId eq 1
```

**OData action:**

GetActiveCustomers is an OData function. Let's write an OData action to send an email to a customer by customerId and message. \(Imaging every customer has an email in database\).

```csharp
public class SendEmailToCustomerArgs
{
    public int customerId { get; set; }

    public string message { get; set; }
}

[Action]
public async Task SendEmailToCustomer(SendEmailToCustomerArgs args)
{
    // ...
}
```

As you can see this action has no return value \(Task is an async equivalent of void\). There is a class called SendEmailToCustomerArgs. **Each property of that class describes one of your parameters.** It can have a properties such as public CustomerDto\[\] customers { get; set; } etc.

To accept a complex type, you've to define your complex type as following:

```csharp
[ComplexType]
public class Location
{
    public int Lat { get;set; }
    public int Lon { get;set; }
}
```

SendEmailToCustomerArgs can accepts following parameters too:

```csharp
public class SendEmailToCustomerParams
{
    public Location location { get; set; } // complex type parameter

    public CustomerDto[] customers { get; set; } // array of customer dto
}
```

Note that action can have no parameter, or parameters with types of dto/complex type/simple type\(string,int,...\). **But you may not accept your model as a parameter.** For example, you may not accept a list of your customer models in your action. Try using list of customer dto instead.

**OData Single result:**

Imaging you want to create a function which returns customer by its Id. You might develop something like this:

```csharp
[Function]
public virtual async Task<CustomerDto> GetCustomerById(int customerId, CancellationToken cancellationToken)
{
    return await Mapper.FromEntityQueryToDtoQuery((await CustomersRepository.GetAllAsync(cancellationToken)))
                       .FirstAsync(c => c.Id == customerId, cancellationToken);
}
```

First, it converts model query to dto query, then it returns customer by id. We recommend you to use SingleResult here. It has following advantages:

1- In case no data is found, it returns 404 - NotFound status code.

2- It supports $select and $exapnd, so if you want, you can return some properties of the customer when you call GetCustomerById, and you can retrive that customer with or without her orders.

Let's rewrite that as following:

```csharp
[Function]
public virtual async Task<SingleResult<CustomerDto>> GetCustomerById2(int customerId, CancellationToken cancellationToken)
{
    return SingleResult.Create(Mapper.FromEntityQueryToDtoQuery((await CustomersRepository.GetAllAsync(cancellationToken)))
        .Where(c => c.Id == customerId)); // We use .Where instead of .First
}
```

Now you can use $select & $expand.

**Associations:**

Imaging you've CategoryDto & ProductDto classes where one category has many products. You can write followings:

```csharp
public class CategoryDto : IDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public List<ProductDto> Products { get; set; }
}

public class ProductDto : IDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public CategoryDto Category { get; set; }
}
```

You provide such a details for following reasons:

1- OData is a query language, for example, you can execute complicated queries such as "categories with 'Test' in their names" \($filter\) directly from the client side. You can load customers which are located in the specific city with or without their orders \($exapnd\).

2- We offer a local \(sqlLite-webSql-indexedDb\) database creation from your DTO\(s\) in C\#/JavaScript/TypeScript, so you can use linq queries to work with the offline database as like as your odata server. We also offer a sync service to push/pull changes to/from online server and local database. To create that database, we need those information.

**DtoSetController:**

DtoSetController needs 3 things: Model-Dto-Repository. It gives you Create-Read-Update-Delete over that repository. It's really simple and extendable. You can write actions/functions there and you can override following Create\|Read\|Update\|Delete methods to customize them.

DtoSetController's sample:

```csharp
public class CategoriesController : DtoSetController<CategoryDto, Category, int /* Key type */>
{

}
```

**Custom Dto-Model mapping:**

Imaging CategoryDto has a property called HasProduct. It's a calculated property by something like this: category =&gt; category.Products.Any\(\)

To handle this, you've to develop custom mapping using AutoMapper facilities. For example:

```csharp
public class MyAppDtoEntityMapperConfiguration : IDtoEntityMapperConfiguration
{
    public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
    {
        mapperConfigExpression.CreateMap<Category, CategoryDto>()
            .ForMember(category => category.HasProduct, config => config.MapFrom(category => category.Products.Any()));
    }
}

// AppStartup class
dependencyManager.RegisterDtoEntityMapperConfiguration<MyAppDtoEntityMapperConfiguration>();
```

Run the app, you can insert/update/delete/read categories and products using swagger ui. You can invoke OData queries such as $filter there.

ToDo:

How to generate C\# proxies?

Background Job Worker

SignalR

Identity Server

Automated Tests

Project creation using bit CLI

