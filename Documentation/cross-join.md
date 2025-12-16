# Cross-Join Queries

OData V4 supports cross-join queries that combine multiple entity sets in a single query. This is useful for advanced scenarios where you need to correlate data across entity sets.

## Table of Contents

- [Overview](#overview)
- [Creating Cross-Join Queries](#creating-cross-join-queries)
- [Query Options](#query-options)
- [Processing Results](#processing-results)
- [Use Cases](#use-cases)

## Overview

Cross-join produces a Cartesian product of the specified entity sets, which can then be filtered to find matching combinations.

```
GET /$crossjoin(Products,Categories)?$filter=Products/CategoryId eq Categories/Id
```

This is equivalent to a SQL CROSS JOIN with a WHERE clause.

## Creating Cross-Join Queries

### Basic Cross-Join

```csharp
// Create a cross-join query
var crossJoin = client.CrossJoin("Products", "Categories");

// Execute the query
var response = await client.GetCrossJoinAsync(crossJoin);

foreach (var row in response.Value)
{
    var product = row.GetEntity<Product>("Products");
    var category = row.GetEntity<Category>("Categories");
    
    Console.WriteLine($"Product: {product?.Name}, Category: {category?.Name}");
}
```

### With Filter

Filter the cross-join to meaningful combinations:

```csharp
// Only return rows where Product.CategoryId matches Category.Id
var crossJoin = client.CrossJoin("Products", "Categories")
    .Filter("Products/CategoryId eq Categories/Id");

var response = await client.GetCrossJoinAsync(crossJoin);
```

### Multiple Entity Sets

```csharp
// Cross-join three entity sets
var crossJoin = client.CrossJoin("Products", "Categories", "Suppliers")
    .Filter("Products/CategoryId eq Categories/Id and Products/SupplierId eq Suppliers/Id");

var response = await client.GetCrossJoinAsync(crossJoin);

foreach (var row in response.Value)
{
    var product = row.GetEntity<Product>("Products");
    var category = row.GetEntity<Category>("Categories");
    var supplier = row.GetEntity<Supplier>("Suppliers");
    
    Console.WriteLine($"{product?.Name} - {category?.Name} - {supplier?.CompanyName}");
}
```

## Query Options

### Select

Select specific properties from each entity set:

```csharp
var crossJoin = client.CrossJoin("Products", "Categories")
    .Filter("Products/CategoryId eq Categories/Id")
    .Select("Products/Id,Products/Name,Categories/Name");
```

### Expand

Expand navigation properties:

```csharp
var crossJoin = client.CrossJoin("Products", "Categories")
    .Filter("Products/CategoryId eq Categories/Id")
    .Expand("Products/Supplier");
```

### OrderBy

Order the results:

```csharp
var crossJoin = client.CrossJoin("Products", "Categories")
    .Filter("Products/CategoryId eq Categories/Id")
    .OrderBy("Categories/Name,Products/Name");

// Descending order
var crossJoin = client.CrossJoin("Products", "Categories")
    .OrderBy("Products/Price desc");
```

### Paging

Use Skip and Top for pagination:

```csharp
var crossJoin = client.CrossJoin("Products", "Categories")
    .Filter("Products/CategoryId eq Categories/Id")
    .Skip(20)
    .Top(10);
```

### Count

Include total count:

```csharp
var crossJoin = client.CrossJoin("Products", "Categories")
    .Filter("Products/CategoryId eq Categories/Id")
    .Count();

var response = await client.GetCrossJoinAsync(crossJoin);
Console.WriteLine($"Total matching combinations: {response.Count}");
```

### Custom Headers

```csharp
var crossJoin = client.CrossJoin("Products", "Categories")
    .WithHeader("Prefer", "odata.maxpagesize=50");
```

## Processing Results

### Access Entity Data

```csharp
var response = await client.GetCrossJoinAsync(crossJoin);

foreach (var row in response.Value)
{
    // Check if entity exists in row
    if (row.HasEntity("Products"))
    {
        var product = row.GetEntity<Product>("Products");
        Console.WriteLine($"Product: {product?.Name}");
    }
    
    if (row.HasEntity("Categories"))
    {
        var category = row.GetEntity<Category>("Categories");
        Console.WriteLine($"Category: {category?.Name}");
    }
}
```

### Get All Pages

For large result sets with pagination:

```csharp
var crossJoin = client.CrossJoin("Products", "Categories")
    .Filter("Products/CategoryId eq Categories/Id");

// Follows nextLink automatically
var response = await client.GetAllCrossJoinAsync(crossJoin);

Console.WriteLine($"Total rows: {response.Value.Count}");
```

### Access Raw JSON

```csharp
foreach (var row in response.Value)
{
    // Access raw JSON elements if needed
    foreach (var (entitySetName, jsonElement) in row.Entities)
    {
        Console.WriteLine($"{entitySetName}: {jsonElement}");
    }
}
```

## Use Cases

### Find Products Without Categories

```csharp
// This requires a different approach - use regular queries
// Cross-join is for combining, not finding missing relationships
```

### Product-Category Matrix

```csharp
var crossJoin = client.CrossJoin("Products", "Categories")
    .Filter("Products/CategoryId eq Categories/Id")
    .Select("Products/Name,Products/Price,Categories/Name");

var response = await client.GetCrossJoinAsync(crossJoin);

var matrix = response.Value
    .Select(row => new
    {
        ProductName = row.GetEntity<Product>("Products")?.Name,
        ProductPrice = row.GetEntity<Product>("Products")?.Price,
        CategoryName = row.GetEntity<Category>("Categories")?.Name
    })
    .ToList();
```

### Multi-Table Report

```csharp
public async Task<List<SalesReportRow>> GenerateSalesReport()
{
    var crossJoin = client.CrossJoin("Orders", "Customers", "Products")
        .Filter("Orders/CustomerId eq Customers/Id and Orders/ProductId eq Products/Id")
        .Select("Orders/Id,Orders/Quantity,Orders/OrderDate,Customers/Name,Products/Name,Products/Price")
        .OrderBy("Orders/OrderDate desc")
        .Top(100);
    
    var response = await client.GetCrossJoinAsync(crossJoin);
    
    return response.Value.Select(row => new SalesReportRow
    {
        OrderId = row.GetEntity<Order>("Orders")?.Id ?? 0,
        OrderDate = row.GetEntity<Order>("Orders")?.OrderDate ?? default,
        CustomerName = row.GetEntity<Customer>("Customers")?.Name ?? "",
        ProductName = row.GetEntity<Product>("Products")?.Name ?? "",
        Quantity = row.GetEntity<Order>("Orders")?.Quantity ?? 0,
        UnitPrice = row.GetEntity<Product>("Products")?.Price ?? 0
    }).ToList();
}
```

## Entity Models

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Supplier
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

public class SalesReportRow
{
    public int OrderId { get; set; }
    public DateTimeOffset OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

## Limitations

1. **Requires at least two entity sets** - Cross-join with single entity set throws exception
2. **Can be expensive** - Without filters, produces Cartesian product
3. **Server support varies** - Not all OData servers implement cross-join
4. **Filter required for meaningful results** - Always add filter to limit combinations

## Best Practices

1. **Always filter** - Cross-join without filter returns Cartesian product (n × m rows)
2. **Use select** - Reduce payload by selecting only needed properties
3. **Limit results** - Use Top/Skip for pagination
4. **Consider alternatives** - Often $expand on regular queries is simpler and more efficient
