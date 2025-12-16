# CRUD Operations

This document covers Create, Read, Update, and Delete operations.

## Table of Contents

- [Reading Entities](#reading-entities)
- [Creating Entities](#creating-entities)
- [Updating Entities (PATCH)](#updating-entities-patch)
- [Replacing Entities (PUT)](#replacing-entities-put)
- [Deleting Entities](#deleting-entities)

## Reading Entities

### Get All Entities

```csharp
var query = client.For<Product>("Products");
var response = await client.GetAsync(query);

foreach (var product in response.Value)
{
    Console.WriteLine($"{product.Id}: {product.Name} - ${product.Price}");
}
```

### Get All Pages

When results span multiple pages, use `GetAllAsync` to fetch all automatically:

```csharp
var query = client.For<Product>("Products")
    .Filter("Price gt 50")
    .OrderBy("Name");

var response = await client.GetAllAsync(query, cancellationToken);
Console.WriteLine($"Retrieved {response.Value.Count} products");
```

### Get by Key

```csharp
// Using inferred entity set name
var product = await client.GetByKeyAsync<Product, int>(123);

// With explicit query builder for select/expand
var query = client.For<Product>("Products")
    .Select("Id,Name,Price")
    .Expand("Category");
var product = await client.GetByKeyAsync<Product, int>(123, query);
```

### Get Single Matching Entity

```csharp
var query = client.For<Product>("Products")
    .Filter("SKU eq 'ABC-123'");

// Throws if zero or more than one match
var product = await client.GetSingleAsync(query);

// Returns null if zero matches, throws if more than one
var product = await client.GetSingleOrDefaultAsync(query);

// Returns first match or null
var product = await client.GetFirstOrDefaultAsync(query);
```

## Creating Entities

Use `CreateAsync` to POST a new entity:

```csharp
var newProduct = new Product
{
    Name = "Super Widget",
    Description = "A fantastic widget",
    Price = 29.99m,
    Rating = 5
};

var created = await client.CreateAsync("Products", newProduct);
Console.WriteLine($"Created with ID: {created.Id}");
```

### With Custom Headers

```csharp
var headers = new Dictionary<string, string>
{
    { "Prefer", "return=representation" }
};

var created = await client.CreateAsync("Products", newProduct, headers);
```

### Entity Model

Define your entity class with proper JSON serialization attributes:

```csharp
using System.Text.Json.Serialization;

public class Product
{
    [JsonPropertyName("ID")]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public decimal? Price { get; set; }
    
    public int? Rating { get; set; }
    
    public DateTimeOffset? ReleaseDate { get; set; }
    
    // Navigation property
    public Category? Category { get; set; }
}
```

## Updating Entities (PATCH)

Use `UpdateAsync` for partial updates (PATCH):

```csharp
// Update only specific fields
var updated = await client.UpdateAsync<Product>(
    "Products", 
    123,                           // key
    new { Price = 39.99 }          // fields to update
);

Console.WriteLine($"Updated price: {updated.Price}");
```

### Updating Multiple Fields

```csharp
var patchValues = new 
{
    Name = "Super Widget Pro",
    Price = 49.99,
    Description = "An even better widget"
};

var updated = await client.UpdateAsync<Product>("Products", 123, patchValues);
```

### With Strongly-Typed Key

```csharp
// String key
var updated = await client.UpdateAsync<Customer, string>(
    "Customers", 
    "ALFKI", 
    new { ContactName = "John Doe" }
);

// Guid key
var updated = await client.UpdateAsync<Order, Guid>(
    "Orders", 
    orderId, 
    new { Status = "Shipped" }
);
```

### With Optimistic Concurrency (ETag)

See [ETag & Concurrency](etag-concurrency.md) for details.

```csharp
// Get entity with ETag
var result = await client.GetByKeyWithETagAsync<Product, int>(123);
var product = result.Value;
var etag = result.ETag;

// Update with ETag for concurrency control
try
{
    var updated = await client.UpdateAsync<Product>(
        "Products", 
        123, 
        new { Price = 39.99 },
        etag  // Pass ETag for If-Match header
    );
}
catch (ODataConcurrencyException ex)
{
    Console.WriteLine($"Entity was modified. Your ETag: {ex.RequestETag}, Current: {ex.CurrentETag}");
}
```

## Replacing Entities (PUT)

Use `ReplaceAsync` for full entity replacement (PUT):

```csharp
var fullProduct = new Product
{
    Id = 123,
    Name = "Completely New Product",
    Description = "This replaces all fields",
    Price = 99.99m,
    Rating = 4
    // All other fields will be set to default/null
};

var replaced = await client.ReplaceAsync("Products", 123, fullProduct);
```

### Difference Between PATCH and PUT

| Aspect | PATCH (UpdateAsync) | PUT (ReplaceAsync) |
|--------|---------------------|-------------------|
| Fields sent | Only changed fields | All fields |
| Missing fields | Unchanged | Set to default/null |
| Use case | Partial update | Full replacement |

## Deleting Entities

```csharp
// Delete by key
await client.DeleteAsync("Products", 123);

// With string key
await client.DeleteAsync("Customers", "ALFKI");

// With Guid key
await client.DeleteAsync("Orders", orderId);
```

### With Optimistic Concurrency (ETag)

```csharp
// Get entity with ETag
var result = await client.GetByKeyWithETagAsync<Product, int>(123);
var etag = result.ETag;

// Delete with ETag
try
{
    await client.DeleteAsync("Products", 123, etag);
}
catch (ODataConcurrencyException ex)
{
    Console.WriteLine("Entity was modified since it was retrieved");
}
```

## Error Handling

```csharp
try
{
    var product = await client.GetByKeyAsync<Product, int>(999);
}
catch (ODataNotFoundException ex)
{
    // 404 - Entity not found
    Console.WriteLine($"Not found: {ex.RequestUrl}");
}
catch (ODataUnauthorizedException ex)
{
    // 401 - Authentication required
    Console.WriteLine($"Unauthorized: {ex.Message}");
}
catch (ODataForbiddenException ex)
{
    // 403 - Access denied
    Console.WriteLine($"Forbidden: {ex.Message}");
}
catch (ODataConcurrencyException ex)
{
    // 412 - ETag mismatch
    Console.WriteLine($"Concurrency conflict: {ex.Message}");
}
catch (ODataClientException ex)
{
    // Other errors
    Console.WriteLine($"Error {ex.StatusCode}: {ex.ResponseBody}");
}
```
