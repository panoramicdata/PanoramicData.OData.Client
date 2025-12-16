# ETag & Optimistic Concurrency

OData V4 supports optimistic concurrency control using ETags. This prevents data loss from concurrent updates by ensuring you're modifying the same version you read.

## Table of Contents

- [Overview](#overview)
- [Reading ETags](#reading-etags)
- [Updating with ETags](#updating-with-etags)
- [Deleting with ETags](#deleting-with-etags)
- [Handling Concurrency Conflicts](#handling-concurrency-conflicts)
- [Complete Patterns](#complete-patterns)

## Overview

Optimistic concurrency with ETags:
1. Read entity - server returns ETag (version identifier) in response
2. Modify locally - make changes in your application
3. Update with ETag - send update with `If-Match` header containing ETag
4. Server validates - if ETag matches current version, update succeeds; otherwise 412 Precondition Failed

## Reading ETags

### Get Single Entity with ETag

```csharp
var result = await client.GetByKeyWithETagAsync<Product, int>(123);

var product = result.Value;
var etag = result.ETag;

Console.WriteLine($"Product: {product.Name}");
Console.WriteLine($"ETag: {etag}");
```

### Get Collection with ETag

```csharp
var query = client.For<Product>("Products").Top(10);
var response = await client.GetAsync(query);

// Collection ETag (if server provides one)
var collectionETag = response.ETag;
```

### Get Singleton with ETag

```csharp
var result = await client.GetSingletonWithETagAsync<UserProfile>("Me");

var profile = result.Value;
var etag = result.ETag;
```

## Updating with ETags

### Update with Concurrency Check

```csharp
// 1. Get entity with ETag
var result = await client.GetByKeyWithETagAsync<Product, int>(123);
var product = result.Value;
var etag = result.ETag;

// 2. Update with ETag
var updated = await client.UpdateAsync<Product>(
    "Products",
    123,
    new { Price = 39.99m },
    etag  // Sends If-Match header
);
```

### Replace with Concurrency Check

```csharp
var result = await client.GetByKeyWithETagAsync<Product, int>(123);
var etag = result.ETag;

// PUT (full replacement) with ETag
var replaced = await client.ReplaceAsync(
    "Products",
    123,
    new Product { Id = 123, Name = "New Name", Price = 49.99m },
    etag
);
```

### Update Singleton with ETag

```csharp
var result = await client.GetSingletonWithETagAsync<UserProfile>("Me");
var etag = result.ETag;

var updated = await client.UpdateSingletonAsync<UserProfile>(
    "Me",
    new { Theme = "Dark" },
    etag
);
```

## Deleting with ETags

### Delete with Concurrency Check

```csharp
// Get entity and ETag
var result = await client.GetByKeyWithETagAsync<Product, int>(123);
var etag = result.ETag;

// Show confirmation to user...

// Delete with ETag
await client.DeleteAsync("Products", 123, etag);
```

### Delete with Wildcard ETag

```csharp
// Delete regardless of version (use with caution)
await client.DeleteAsync("Products", 123, "*");
```

## Handling Concurrency Conflicts

### ODataConcurrencyException

When the server returns 412 Precondition Failed:

```csharp
try
{
    var updated = await client.UpdateAsync<Product>(
        "Products",
        123,
        new { Price = 39.99m },
        etag
    );
}
catch (ODataConcurrencyException ex)
{
    Console.WriteLine("Concurrency conflict detected!");
    Console.WriteLine($"Your ETag: {ex.RequestETag}");
    Console.WriteLine($"Current ETag: {ex.CurrentETag}");
    Console.WriteLine($"Status: {ex.StatusCode}");
    
    // Handle the conflict...
}
```

### Conflict Resolution Strategies

#### Strategy 1: Last Writer Wins

```csharp
// Simply retry without ETag (server always accepts)
var updated = await client.UpdateAsync<Product>(
    "Products",
    123,
    new { Price = 39.99m }
    // No ETag = no concurrency check
);
```

#### Strategy 2: Refresh and Retry

```csharp
async Task<Product> UpdateWithRetry(int id, object changes, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        // Get fresh entity and ETag
        var result = await client.GetByKeyWithETagAsync<Product, int>(id);
        
        try
        {
            return await client.UpdateAsync<Product>(
                "Products",
                id,
                changes,
                result.ETag
            );
        }
        catch (ODataConcurrencyException) when (attempt < maxRetries)
        {
            // Retry with new ETag
            await Task.Delay(100 * attempt);
        }
    }
    
    throw new InvalidOperationException("Max retries exceeded");
}
```

#### Strategy 3: Merge Changes

```csharp
async Task<Product> MergeAndUpdate(int id, Func<Product, object> getChanges)
{
    while (true)
    {
        var result = await client.GetByKeyWithETagAsync<Product, int>(id);
        var product = result.Value;
        
        // Let caller compute changes based on current state
        var changes = getChanges(product);
        
        try
        {
            return await client.UpdateAsync<Product>(
                "Products",
                id,
                changes,
                result.ETag
            );
        }
        catch (ODataConcurrencyException)
        {
            // Retry - getChanges will use updated product
            continue;
        }
    }
}

// Usage
var updated = await MergeAndUpdate(123, product => new
{
    Price = product.Price * 1.1m  // 10% increase from current value
});
```

#### Strategy 4: User Resolution

```csharp
async Task<Product> UpdateWithUserResolution(int id, Product userVersion)
{
    var result = await client.GetByKeyWithETagAsync<Product, int>(id);
    
    try
    {
        return await client.UpdateAsync<Product>(
            "Products",
            id,
            new { Name = userVersion.Name, Price = userVersion.Price },
            result.ETag
        );
    }
    catch (ODataConcurrencyException ex)
    {
        // Get current server version
        var serverVersion = await client.GetByKeyAsync<Product, int>(id);
        
        // Show both versions to user for resolution
        throw new ConflictException
        {
            UserVersion = userVersion,
            ServerVersion = serverVersion,
            UserETag = ex.RequestETag,
            ServerETag = ex.CurrentETag
        };
    }
}
```

## Complete Patterns

### Edit Form Pattern

```csharp
public class ProductEditViewModel
{
    public Product Product { get; set; }
    public string ETag { get; set; }
    
    public async Task LoadAsync(ODataClient client, int id)
    {
        var result = await client.GetByKeyWithETagAsync<Product, int>(id);
        Product = result.Value!;
        ETag = result.ETag!;
    }
    
    public async Task<bool> SaveAsync(ODataClient client)
    {
        try
        {
            Product = await client.UpdateAsync<Product>(
                "Products",
                Product.Id,
                new { Product.Name, Product.Price },
                ETag
            );
            
            // Get new ETag for subsequent saves
            var result = await client.GetByKeyWithETagAsync<Product, int>(Product.Id);
            ETag = result.ETag!;
            
            return true;
        }
        catch (ODataConcurrencyException)
        {
            // Notify user of conflict
            return false;
        }
    }
}
```

### Batch Updates with ETags

```csharp
var batch = client.CreateBatch();
var changeset = batch.CreateChangeset();

// Each update can have its own ETag
changeset.Update<Product, int>("Products", 1, new { Price = 10 }, etag1);
changeset.Update<Product, int>("Products", 2, new { Price = 20 }, etag2);
changeset.Delete("Products", 3, etag3);

try
{
    var response = await batch.ExecuteAsync();
}
catch (ODataConcurrencyException)
{
    // Entire changeset failed due to one conflict
}
```

## ETag Formats

Common ETag formats:

| Format | Example | Description |
|--------|---------|-------------|
| Strong | `"abc123"` | Byte-for-byte identical |
| Weak | `W/"abc123"` | Semantically equivalent |
| Timestamp | `W/"datetime'2024-01-15T10%3A30%3A00'"` | Based on modified date |
| Version | `W/"version=5"` | Sequential version number |

```csharp
// The client handles both weak and strong ETags
var result = await client.GetByKeyWithETagAsync<Product, int>(123);
Console.WriteLine(result.ETag);  // e.g., W/"abc123" or "abc123"
```

## Best Practices

1. **Always use ETags for user-edited data** - Prevents lost updates
2. **Cache ETags with entities** - Store together for later updates
3. **Handle conflicts gracefully** - Don't just show error, offer resolution
4. **Consider retry logic** - Automatic retries work for many scenarios
5. **Use wildcard carefully** - `*` bypasses concurrency check entirely
6. **Test concurrent scenarios** - Ensure conflict handling works correctly
