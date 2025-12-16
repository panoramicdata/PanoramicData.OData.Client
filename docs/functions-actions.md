# Functions and Actions

OData V4 supports two types of operations:
- **Functions**: Read-only operations that return data (GET)
- **Actions**: Operations that may have side effects (POST)

## Table of Contents

- [Overview](#overview)
- [Calling Functions](#calling-functions)
- [Calling Actions](#calling-actions)
- [Bound vs Unbound Operations](#bound-vs-unbound-operations)
- [Common Patterns](#common-patterns)

## Overview

| Type | HTTP Method | Side Effects | Return Value |
|------|-------------|--------------|--------------|
| Function | GET | No | Required |
| Action | POST | Yes | Optional |

## Calling Functions

Functions are read-only operations invoked with GET.

### Unbound Functions

Functions not attached to an entity:

```csharp
// GET /GetProductsByCategory(category='Electronics')
var query = client.For<Product>("Products")
    .Function("GetProductsByCategory", new { category = "Electronics" });

var products = await client.CallFunctionAsync<Product, List<Product>>(query);

foreach (var product in products)
{
    Console.WriteLine(product.Name);
}
```

### Bound Functions (Entity Set)

Functions attached to an entity set:

```csharp
// GET /Products/Namespace.MostPopular()
var query = client.For<Product>("Products")
    .Function("Namespace.MostPopular");

var products = await client.CallFunctionAsync<Product, List<Product>>(query);
```

### Bound Functions (Single Entity)

Functions attached to a specific entity:

```csharp
// GET /Products(1)/Namespace.GetRelated()
var query = client.For<Product>("Products")
    .Key(1)
    .Function("Namespace.GetRelated");

var related = await client.CallFunctionAsync<Product, List<Product>>(query);
```

### Function with Multiple Parameters

```csharp
// GET /SearchProducts(term='widget',minPrice=10,maxPrice=100)
var query = client.For<Product>("Products")
    .Function("SearchProducts", new 
    { 
        term = "widget",
        minPrice = 10,
        maxPrice = 100
    });

var results = await client.CallFunctionAsync<Product, List<Product>>(query);
```

### Function Returning Single Value

```csharp
// GET /Products(1)/Namespace.CalculateDiscount(percentage=10)
var query = client.For<Product>("Products")
    .Key(1)
    .Function("Namespace.CalculateDiscount", new { percentage = 10 });

var discountedPrice = await client.CallFunctionAsync<Product, decimal>(query);
Console.WriteLine($"Discounted price: {discountedPrice}");
```

## Calling Actions

Actions can have side effects and are invoked with POST.

### Simple Action

```csharp
// POST /Products(1)/Namespace.Publish
var result = await client.CallActionAsync<PublishResult>(
    "Products(1)/Namespace.Publish"
);

Console.WriteLine($"Published: {result.Success}");
```

### Action with Parameters

```csharp
// POST /Orders(123)/Namespace.Ship
// Body: { "TrackingNumber": "ABC123", "Carrier": "FedEx" }
var result = await client.CallActionAsync<ShipmentResult>(
    "Orders(123)/Namespace.Ship",
    new 
    { 
        TrackingNumber = "ABC123",
        Carrier = "FedEx"
    }
);

Console.WriteLine($"Shipped via: {result.Carrier}");
```

### Action Without Return Value

```csharp
// POST /Products(1)/Namespace.Archive
// Returns 204 No Content
await client.CallActionAsync<object>("Products(1)/Namespace.Archive");
```

### Unbound Action

```csharp
// POST /ResetDatabase
// Body: { "Confirm": true }
await client.CallActionAsync<object>(
    "ResetDatabase",
    new { Confirm = true }
);
```

### Action with Custom Headers

```csharp
var headers = new Dictionary<string, string>
{
    { "Prefer", "return=representation" },
    { "If-Match", "*" }
};

var result = await client.CallActionAsync<Product>(
    "Products(1)/Namespace.Clone",
    new { NewName = "Widget Copy" },
    headers
);
```

## Bound vs Unbound Operations

### Unbound Operations

Not attached to any entity, called directly:

```csharp
// Unbound function
// GET /GetServerTime()
var query = client.For<object>("")
    .Function("GetServerTime");
var time = await client.CallFunctionAsync<object, DateTime>(query);

// Unbound action
// POST /SendNotification
await client.CallActionAsync<object>("SendNotification", new { Message = "Hello" });
```

### Entity Set Bound Operations

Attached to a collection:

```csharp
// Function bound to Products collection
// GET /Products/Namespace.TopRated()
var query = client.For<Product>("Products")
    .Function("Namespace.TopRated");

// Action bound to Products collection
// POST /Products/Namespace.DiscountAll
await client.CallActionAsync<object>(
    "Products/Namespace.DiscountAll",
    new { Percentage = 10 }
);
```

### Entity Bound Operations

Attached to a specific entity:

```csharp
// Function bound to single Product
// GET /Products(1)/Namespace.GetSuggestions()
var query = client.For<Product>("Products")
    .Key(1)
    .Function("Namespace.GetSuggestions");

// Action bound to single Product
// POST /Products(1)/Namespace.Publish
await client.CallActionAsync<object>("Products(1)/Namespace.Publish");
```

## Common Patterns

### Search Function

```csharp
public async Task<List<Product>> SearchProducts(string term, int maxResults = 10)
{
    var query = client.For<Product>("Products")
        .Function("Search", new { term, maxResults });
    
    return await client.CallFunctionAsync<Product, List<Product>>(query) 
        ?? new List<Product>();
}
```

### Workflow Action

```csharp
public async Task<WorkflowResult> SubmitForApproval(int documentId)
{
    return await client.CallActionAsync<WorkflowResult>(
        $"Documents({documentId})/Namespace.SubmitForApproval",
        new { Comments = "Please review" }
    );
}
```

### Bulk Action

```csharp
public async Task<BulkResult> DeleteExpiredProducts()
{
    return await client.CallActionAsync<BulkResult>(
        "Products/Namespace.DeleteExpired",
        new { OlderThanDays = 365 }
    );
}
```

### Calculated Value Function

```csharp
public async Task<decimal> GetTotalOrderValue(int orderId)
{
    var query = client.For<Order>("Orders")
        .Key(orderId)
        .Function("Namespace.CalculateTotal", new { IncludeTax = true });
    
    return await client.CallFunctionAsync<Order, decimal>(query);
}
```

## Entity Models for Results

```csharp
public class PublishResult
{
    public bool Success { get; set; }
    public string? PublishedUrl { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}

public class ShipmentResult
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public DateTimeOffset EstimatedDelivery { get; set; }
}

public class WorkflowResult
{
    public string Status { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}

public class BulkResult
{
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = [];
}
```

## Error Handling

```csharp
try
{
    var result = await client.CallActionAsync<ProcessResult>(
        "Orders(123)/Namespace.Process"
    );
}
catch (ODataNotFoundException)
{
    Console.WriteLine("Order not found");
}
catch (ODataClientException ex) when (ex.StatusCode == 400)
{
    Console.WriteLine($"Invalid request: {ex.ResponseBody}");
}
catch (ODataClientException ex) when (ex.StatusCode == 409)
{
    Console.WriteLine("Conflict - order already processed");
}
```
