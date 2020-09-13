Duo to the announcement of Microsoft.AspNetCore.OData V7.4, I decided to prepare an article to demonstrate how to use OData in ASP.NET Core 3.1+ projects with minimal changes in the code, so you can see how much it became easy to use in new versions. This approach is valid for projects that have endpoint routing which is the recommended approach.
First with dotnet --version or dotnet --info commands or any other methods, we should be sure about installation of dot net core 3.1 sdk. Then we execute the below command:
// code here
In Visual Studio, this command is equivalent to creating a new ASP.NET Core Web API project. This project uses endpoint routing. There are a WeatherForecast model and WeatherForecasetController with a Get action that creates and returns a list of WeatherForecasts. This project doesn't involve any database.
So what should we do to make this project OData enabled?
1- Install these two packages:
// code here
2- Open Startup.cs file and modify the ConfigureService as below:
// code here
Also modify app.UseEndpoints line in Configure method as below:
// code here
Then modify the Get action in WeatherForecastController as below:
// code here
Run the app and go to this address:
// address here
The orderby=TempratureC phrase causes the WeatherForcast result items to be ordered by their temperatures when sending them to the client.
As you can see, we easily added OData to our sample app.
Generally, OData is capable of filtering records, projection, ordering, paging, grouping, aggregations, etc. We will see some provided examples in the next section.
We will use a list of customers instead of the ASP.NET Core default WeatherForcast model because it's a little vague.
For returning only woman clients: (Filtering on Enum):
// code here
To ordering woman clients based on their address street number: (Ordering on nested properties and combination of orderby and filter clauses)
// code here
To get customers with Ali in their names:

To see more querying examples in OData check out this link. There are a lot of querying capabilities including Any/All in the filter query provided by OData.
In addition, if we provide an IQueryable for OData instead of an array or list, the OData will execute the queries of paging, orderby, etc against the database and only the final result will return from the database, therefore it will have a huge effect on application performance.
The security in this method will not be in danger, because if you put a where on the server-side query,  based on app logic, the client only can (OData) query on the limited with where applied data, so it can fetch only accessed data from the database.
All of the above concepts can be applied without any changes in app routing, Swagger, etc, so you can use your best practices like using Dto instead of raw Entity along with OData features.