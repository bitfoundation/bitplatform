 # Bit OData

So, what's OData? following text which is copied from http://odata.org describes odata well:

OData (Open Data Protocol) is an ISO/IEC approved, OASIS standard that defines a set of best practices for building and consuming "RESTful APIs". OData helps you "focus on your business logic" while building RESTful APIs.
OData RESTful APIs are easy to consume. The OData metadata, a machine-readable description of the data model of the APIs, enables the creation of powerful generic client proxies and tools.

Using bit framework, you can build OData services very easily, and we generate C# - TypeScript - JavaScript clients for you automatically. You can use those no matter you're developing xamarin forms, angular js, angular, react js & native etc. We also have out of the box support for Open-API (Swagger). Using [azure auto rest](https://github.com/Azure/autorest) tools, you can generate client side for almost any language you want.

In bit apps, you develop odata controllers for your DTO (Data transfer objects) classes.

Instead of sending your "domain models/entities" to client, you send DTO to the client. Your "model/entities" gets complicated over time based on business requirements, and in the client side you need something less complicated and easier to use. DTO (Something similar to ViewModel in MVC) is a common best practice in modern software development world.

"Model/Entity" - DTO examples:

Example 1: Consider following "models/entities":

```csharp
public class Customer : IEntity
{
    public int Id { get;set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public bool IsActive { get; set; }
    public City City { get; set; }
    [ForeignKey(nameof(City))]
    public int CityId { get; set; }
}

public class City : IEntity
{
    public int Id { get;set; }
    public string Name { get; set; }
}
```

You can have following DTO:

```csharp
public class CustomerDto : IDto
{
    public int Id { get;set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public bool IsActive { get; set; }
    public int CityId { get; set; }
    public string CityName { get; set; } // We've added "Name" of city as "CityName" into CustomerDto class.
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
    public int OrdersCount { get; set; } // count of orders. You need this in one of your forms for example.
}
```

You can create as many as DTOs from your "models/entities". You can add calculated fields, apply flattening etc.

To follow best practices, keep these rules in your mind:

1- Do not inherit from "models/entities" in your DTO classes. For example, CustomerDto does not inherit from Customer.

2- Do not declare a property with type of your "models/entities" in your DTO classes. For example, CustomerDto has no property of type City model. Do not use models/enities enums and complex types in your dto clasess too.

3- Develop a DTO for every Task you've. For example, if you want to show customers names and their orders count, create a DTO for this task. And when you want to create a Customer registration form which accepts credit card number, develop a new DTO for that task.

To send DTO to client side, you develop DtoController. [Examples can be found here](https://github.com/bit-foundation/bit-framework/tree/master/Samples/ODataExamples/).

### 1- CustomerDtoControllerSample

In this sample we don't create two DTO classes for customers as this is a simple sample only.

In Bit-OData, you develop DtoController instead of ApiController. Note that you can continue developing API controllers side by side.

So, lets take a look at new codes. First you've to configure web api & odata toghether by following code:

```csharp
dependencyManager.RegisterDefaultWebApiAndODataConfiguration(); // instead of dependencyManager.RegisterDefaultWebApiConfiguration();
```

You can optionally add web api to your app by following the code as like as what you did before:

```csharp
dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
{
    webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();
});
```

And you can use following code to add OData to your app:

```csharp
dependencyManager.RegisterODataMiddleware(odataDependencyManager =>
{
    odataDependencyManager.RegisterODataServiceBuilder<BitODataServiceBuilder>();
    odataDependencyManager.RegisterODataServiceBuilder<MyAppODataServiceBuilder>();
    odataDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration();
});
```

In MyAppODataServiceBuilder class, you've access to ODataModelBuilder, which is useful in advanced scenarios. We create OData metadata from your DTO classes automatically, so you don't have to do anything special in most cases. So just configure your odata route:

```csharp
public override string GetODataRoute()
{
    return "MyApp";
}
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

Mapper automatically maps "model/entities" classes to DTO classes. It uses [AutoMapper](http://automapper.org/) by default and the way we use auto mapper will not slow down your app as described [here](https://docs.bit-framework.com/docs/design-backgrounds/why-auto-mapper-has-no-performance-penalty.html).

Note that you don't have to use bit repository here. You don't have to use entity framework either. You can use mongo db, simple array etc. We need some customers only.

[Function] is used to return data, it has to return data, and it accepts simple type parameters like int, string, enum, etc.
[Action] is used to do something, its return value is optional, and it accepts almost anything.

Run the app and you're good to go. That's a swagger's console you see by default. By opening http://localhost:9000/odata/MyApp/$metadata you see $metadata. $metadata describes your DTO classes, complex types, enums, actions and functions in a standard format. There are tools in several languages to generate client side proxy for you. You can see the list of libraries and tools [here](http://www.odata.org/libraries/).
Note that you can call OData controllers using jquery ajax, fetch, etc too, as they're REST APIs.

Try GetActiveCustomers, It runs something like this on a database:

```sql
select * from Customers inner join Cities on Id = CustomerId where IsActive = 1 /*true*/
```

It has several parameters such as $filter, $order by etc. These are standard OData parameters and they work no matter where the data is come from. (You don't have to use Bit's repository or EntityFramework!)

Try     CityId eq 1      for $filter, it returns customers located in City 1 only!

It runs something like this on a database:

```sql
select * from Customers inner join Cities on Id = CustomerId where IsActive = 1 /*true*/ and CityId = 1
```

As you see, the filter we've developed at server side (c => c.IsActive == true) is combined with query we passed from client side (CityId eq 1). Filters are combined with "AND", so there is no security risk at all.

Try     Id,FirstName,LastName    for $select, it returns those properties of customers only!

It runs something like this on a database:

```sql
select Id,FirstName,LastName from Customers where IsActive = 1 /*true*/
```

There is no join between customers and cities as we've not requested CityName property. It's really performance tuned and smart!

OData supports filtering, ordering, projection, paging etc in a standard way. Almost all UI vendors have support for OData in their rad components such as data grid etc. You can create amazing excel sheets and dashboard using its odata support. In C# and TypeScript you're able to write LINQ queries to consume odata resources. For example:

```csharp
context.customers.GetActiveCustomers().Where(c => c.CityId == 1).ToList(); // This will be converted to $fitler > CityId eq 1
```

Action | CRUD | SingleResult | TypeScript client | C# client | Exception handling | Logging |