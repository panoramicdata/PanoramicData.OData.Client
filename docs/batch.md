# Batch Operations

OData V4 supports batch requests that combine multiple operations into a single HTTP request. This reduces network round-trips and enables atomic transactions via changesets.

## Table of Contents

- [Overview](#overview)
- [Creating a Batch](#creating-a-batch)
- [Batch Operations](#batch-operations)
- [Changesets (Atomic Transactions)](#changesets-atomic-transactions)
- [Executing the Batch](#executing-the-batch)
- [Processing Results](#processing-results)

## Overview

Batch requests allow you to:
- Combine multiple operations into one HTTP request
- Reduce network latency
- Group operations into atomic changesets (all succeed or all fail)
- Mix read and write operations

## Creating a Batch

```csharp
// Create a batch builder
var batch = client.CreateBatch();
```

## Batch Operations

### GET Operations

```csharp
var batch = client.CreateBatch();

// Get entities by key
var getOp1 = batch.Get<Product, int>("Products", 1);
var getOp2 = batch.Get<Product, int>("Products", 2);
var getOp3 = batch.Get<Customer>("Customers", "ALFKI");

// Execute and retrieve results
var response = await batch.ExecuteAsync();

var product1 = response.GetResult<Product>(getOp1);
var product2 = response.GetResult<Product>(getOp2);
var customer = response.GetResult<Customer>(getOp3);
```

### CREATE Operations

```csharp
var batch = client.CreateBatch();

var createOp = batch.Create("Products", new Product
{
    Name = "New Widget",
    Price = 29.99m
});

var response = await batch.ExecuteAsync();
var created = response.GetResult<Product>(createOp);
Console.WriteLine($"Created ID: {created.Id}");
```

### UPDATE Operations

```csharp
var batch = client.CreateBatch();

var updateOp = batch.Update<Product, int>(
    "Products", 
    123, 
    new { Price = 39.99 }
);

// With ETag for concurrency
var updateWithETag = batch.Update<Product, int>(
    "Products", 
    456, 
    new { Price = 49.99 },
    etag: "W/\"abc123\""
);

var response = await batch.ExecuteAsync();
```

### DELETE Operations

```csharp
var batch = client.CreateBatch();

var deleteOp1 = batch.Delete("Products", 123);
var deleteOp2 = batch.Delete("Products", 456, etag: "W/\"xyz789\"");

var response = await batch.ExecuteAsync();
```

## Changesets (Atomic Transactions)

A changeset groups multiple operations that must all succeed or all fail atomically.

```csharp
var batch = client.CreateBatch();

// Create a changeset for atomic operations
var changeset = batch.CreateChangeset();

// All operations in the changeset are atomic
var createOp = changeset.Create("Products", new Product { Name = "Widget A" });
var updateOp = changeset.Update<Product, int>("Products", 100, new { InStock = false });
var deleteOp = changeset.Delete("Products", 200);

// Execute - all succeed or all fail
var response = await batch.ExecuteAsync();

// Check if changeset succeeded
if (response.IsChangesetSuccessful(changeset))
{
    var created = response.GetResult<Product>(createOp);
    Console.WriteLine($"All operations succeeded. Created: {created.Id}");
}
else
{
    Console.WriteLine("Changeset failed - all operations rolled back");
}
```

### Mixing Reads and Changesets

```csharp
var batch = client.CreateBatch();

// Read operation (not in changeset)
var getOp = batch.Get<Product, int>("Products", 1);

// Atomic changeset
var changeset = batch.CreateChangeset();
var createOp = changeset.Create("Products", new Product { Name = "New" });
var updateOp = changeset.Update<Product, int>("Products", 2, new { Price = 10 });

// Another read operation
var getOp2 = batch.Get<Customer>("Customers", "ALFKI");

var response = await batch.ExecuteAsync();
```

### Multiple Changesets

```csharp
var batch = client.CreateBatch();

// First changeset
var changeset1 = batch.CreateChangeset();
changeset1.Create("Products", new Product { Name = "Product A" });
changeset1.Create("Products", new Product { Name = "Product B" });

// Second changeset (independent of first)
var changeset2 = batch.CreateChangeset();
changeset2.Update<Customer, string>("Customers", "ALFKI", new { City = "Berlin" });
changeset2.Update<Customer, string>("Customers", "ANATR", new { City = "Madrid" });

// Each changeset is atomic independently
var response = await batch.ExecuteAsync();
```

## Executing the Batch

```csharp
// Simple execution
var response = await batch.ExecuteAsync();

// With cancellation token
var response = await batch.ExecuteAsync(cancellationToken);
```

## Processing Results

### Check Overall Success

```csharp
var response = await batch.ExecuteAsync();

if (response.HasErrors)
{
    foreach (var error in response.Errors)
    {
        Console.WriteLine($"Operation {error.OperationId} failed: {error.StatusCode} - {error.Message}");
    }
}
```

### Get Individual Results

```csharp
var batch = client.CreateBatch();
var getOp = batch.Get<Product, int>("Products", 1);
var createOp = batch.Create("Products", new Product { Name = "Test" });

var response = await batch.ExecuteAsync();

// Get results by operation ID
var product = response.GetResult<Product>(getOp);
var created = response.GetResult<Product>(createOp);

// Check if specific operation succeeded
if (response.TryGetResult<Product>(getOp, out var result))
{
    Console.WriteLine($"Got: {result.Name}");
}
```

### Check Operation Status

```csharp
var response = await batch.ExecuteAsync();

foreach (var operationResult in response.Results)
{
    Console.WriteLine($"Operation {operationResult.OperationId}: {operationResult.StatusCode}");
    
    if (!operationResult.IsSuccess)
    {
        Console.WriteLine($"  Error: {operationResult.ErrorMessage}");
    }
}
```

## Best Practices

1. **Use changesets for related operations** - If operations depend on each other, put them in a changeset
2. **Keep batches reasonable size** - Very large batches may timeout or be rejected by servers
3. **Handle partial failures** - Operations outside changesets can fail independently
4. **Check server limits** - Some servers limit batch size or nesting depth
