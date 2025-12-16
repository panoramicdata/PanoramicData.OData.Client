# Querying Data

This document covers all query options supported by the PanoramicData.OData.Client library.

## Table of Contents

- [Basic Queries](#basic-queries)
- [Filtering ($filter)](#filtering-filter)
- [Selecting Fields ($select)](#selecting-fields-select)
- [Expanding Related Entities ($expand)](#expanding-related-entities-expand)
- [Ordering Results ($orderby)](#ordering-results-orderby)
- [Paging ($skip, $top)](#paging-skip-top)
- [Counting Results ($count)](#counting-results-count)
- [Searching ($search)](#searching-search)
- [Aggregations ($apply)](#aggregations-apply)
- [Computed Properties ($compute)](#computed-properties-compute)
- [Derived Types (Type Casting)](#derived-types-type-casting)
- [Helper Methods](#helper-methods)

## Basic Queries

Create a query using the fluent query builder:

```csharp
using PanoramicData.OData.Client;

var client = new ODataClient(new ODataClientOptions
{
    BaseUrl = "https://services.odata.org/V4/OData/OData.svc/"
});

// Create a query builder for Products
var query = client.For<Product>("Products");

// Execute the query
var response = await client.GetAsync(query);

// Access results
foreach (var product in response.Value)
{
    Console.WriteLine($"{product.Id}: {product.Name}");
}
```

## Filtering ($filter)

### Using Raw Filter Strings

```csharp
// Simple comparison
var query = client.For<Product>("Products")
    .Filter("Price gt 100");

// Multiple conditions
var query = client.For<Product>("Products")
    .Filter("Price gt 100 and Rating ge 4");

// String functions
var query = client.For<Product>("Products")
    .Filter("contains(Name, 'Widget')");

var query = client.For<Product>("Products")
    .Filter("startswith(Name, 'Super')");

var query = client.For<Product>("Products")
    .Filter("endswith(Name, 'Pro')");

// Case-insensitive search
var query = client.For<Product>("Products")
    .Filter("contains(tolower(Name), 'widget')");
```

### Using LINQ Expressions

```csharp
// Simple comparison
var query = client.For<Product>("Products")
    .Filter(p => p.Price > 100);

// Multiple conditions
var query = client.For<Product>("Products")
    .Filter(p => p.Price > 100 && p.Rating >= 4);

// String operations
var query = client.For<Product>("Products")
    .Filter(p => p.Name.Contains("Widget"));

var query = client.For<Product>("Products")
    .Filter(p => p.Name.StartsWith("Super"));

// Collection contains (IN clause)
var categories = new[] { "Electronics", "Tools" };
var query = client.For<Product>("Products")
    .Filter(p => categories.Contains(p.Category));
// Generates: Category in ('Electronics','Tools')

// Any/All on collections
var query = client.For<Person>("People")
    .Filter(p => p.Emails.Any(e => e.Contains("@company.com")));
// Generates: Emails/any(e: contains(e,'@company.com'))
```

### Combining Multiple Filters

```csharp
// Multiple Filter() calls are combined with AND
var query = client.For<Product>("Products")
    .Filter("Price gt 50")
    .Filter("Rating ge 3")
    .Filter("InStock eq true");
// Generates: $filter=(Price gt 50) and (Rating ge 3) and (InStock eq true)
```

## Selecting Fields ($select)

```csharp
// Select specific fields (reduces payload)
var query = client.For<Product>("Products")
    .Select("Id,Name,Price");

// Using expression
var query = client.For<Product>("Products")
    .Select(p => new { p.Id, p.Name, p.Price });
```

## Expanding Related Entities ($expand)

```csharp
// Expand navigation properties
var query = client.For<Product>("Products")
    .Expand("Category");

// Multiple expansions
var query = client.For<Product>("Products")
    .Expand("Category,Supplier");

// Using expression
var query = client.For<Product>("Products")
    .Expand(p => p.Category);

// Nested expansion (raw string)
var query = client.For<Order>("Orders")
    .Expand("Customer($expand=Address)");
```

## Ordering Results ($orderby)

```csharp
// Ascending order
var query = client.For<Product>("Products")
    .OrderBy("Name");

// Descending order
var query = client.For<Product>("Products")
    .OrderBy("Price desc");

// Multiple ordering
var query = client.For<Product>("Products")
    .OrderBy("Category,Price desc");

// Using expression
var query = client.For<Product>("Products")
    .OrderBy(p => p.Name);

var query = client.For<Product>("Products")
    .OrderBy(p => p.Price, descending: true);

// Using key-value pairs
var orderBys = new Dictionary<string, bool>
{
    { "Category", false },  // ascending
    { "Price", true }       // descending
};
var query = client.For<Product>("Products")
    .OrderBy(orderBys);
```

## Paging ($skip, $top)

```csharp
// Get first 10 items
var query = client.For<Product>("Products")
    .Top(10);

// Skip first 20, take next 10
var query = client.For<Product>("Products")
    .Skip(20)
    .Top(10);
```

### Server-Driven Paging

When the server returns more results than fit in one response, it includes `@odata.nextLink`:

```csharp
// Get single page
var response = await client.GetAsync(query);
// response.NextLink contains URL for next page if more results exist

// Get all pages automatically
var allProducts = await client.GetAllAsync(query, cancellationToken);
```

## Counting Results ($count)

```csharp
// Include count in response
var query = client.For<Product>("Products")
    .Top(10)
    .Count();

var response = await client.GetAsync(query);
Console.WriteLine($"Total matching: {response.Count}");
Console.WriteLine($"Returned in this page: {response.Value.Count}");
```

### Count-Only Query

```csharp
// Get only the count, no entities
var count = await client.GetCountAsync<Product>();

// With filter
var query = client.For<Product>("Products")
    .Filter("Price gt 100");
var count = await client.GetCountAsync(query);
```

## Searching ($search)

```csharp
// Full-text search (server must support it)
var query = client.For<Product>("Products")
    .Search("widget blue");
```

## Aggregations ($apply)

```csharp
// Group by and aggregate
var query = client.For<Product>("Products")
    .Apply("groupby((Category),aggregate(Price with average as AvgPrice))");

// Filter then aggregate
var query = client.For<Product>("Products")
    .Apply("filter(Rating ge 4)/groupby((Category),aggregate($count as Count))");
```

## Computed Properties ($compute)

OData V4.01 feature for computed properties:

```csharp
var query = client.For<OrderLine>("OrderLines")
    .Compute("Price mul Quantity as Total")
    .Select("ProductName,Price,Quantity,Total");
```

## Derived Types (Type Casting)

Query derived types in an inheritance hierarchy:

```csharp
// Get only employees (derived from Person)
var query = client.For<Person>("People")
    .OfType<Employee>("Microsoft.OData.Service.Models.Employee");

// Using simple type name
var employeeQuery = client.For<Person>("People")
    .OfType<Employee>();

// Cast without changing result type
var query = client.For<Person>("People")
    .Cast("Microsoft.OData.Service.Models.Employee")
    .Filter("EmployeeId ne null");
```

## Helper Methods

### Get First or Default

```csharp
var query = client.For<Product>("Products")
    .Filter("Name eq 'Widget'");
    
var product = await client.GetFirstOrDefaultAsync(query);
// Returns null if no match
```

### Get Single

```csharp
var query = client.For<Product>("Products")
    .Filter("Name eq 'UniqueWidget'");
    
var product = await client.GetSingleAsync(query);
// Throws if zero or more than one result

var product = await client.GetSingleOrDefaultAsync(query);
// Throws only if more than one result
```

### Get by Key

```csharp
// Get single entity by key
var product = await client.GetByKeyAsync<Product, int>(123);

// With additional query options
var query = client.For<Product>("Products")
    .Expand("Category");
var product = await client.GetByKeyAsync<Product, int>(123, query);
```

## Custom Headers

```csharp
// Add headers to specific query
var query = client.For<Product>("Products")
    .WithHeader("Prefer", "return=representation")
    .WithHeader("OData-MaxVersion", "4.01");
```

## Raw JSON Response

```csharp
// Get raw JSON when you need full control
var json = await client.GetRawAsync("Products?$filter=Price gt 100");
// Returns JsonDocument for manual parsing
```
