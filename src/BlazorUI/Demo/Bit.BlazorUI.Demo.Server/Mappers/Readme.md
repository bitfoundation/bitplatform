When you have an IQueryable of an Entity or Model classes from EntityFrameworkCore, 
you ultimately need to convert it into an IQueryable of DTO classes and return it to the client.
The client can also implement pagination during the API call by sending values for $top, $skip and sort by $orderby in request query string.
Ultimately, the query is executed by aspnetcore and the data gets streamed from the database to the client, which is the most optimal case.
For this, you need to write a `Project` for each Entity you intend to return a query of DTO class from.

You also write a `Map` for when a DTO is sent to the server for create or update api,
so you can convert it to an Entity and save it in the database using ef core.
You also need a Map method to convert DTO to Entity and vice versa.

You also need a `Patch` method for the update scenario, to perform update operation, first get the Entity from the database with its Id,
then patch the latest changes from the DTO sent by the client, and finally save it.

These methods (`Project`, `Map` and `Patch`) are gets called in controllers.

For more information and to learn about customizing the mapping process, visit the website below:
https://mapperly.riok.app/docs/intro/