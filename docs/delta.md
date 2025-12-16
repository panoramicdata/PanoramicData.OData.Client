# Delta Queries (Change Tracking)

OData V4 delta queries allow you to efficiently retrieve only the changes since your last query. This is essential for synchronization scenarios.

## Table of Contents

- [Overview](#overview)
- [Initial Query](#initial-query)
- [Getting Changes](#getting-changes)
- [Understanding Delta Responses](#understanding-delta-responses)
- [Complete Sync Pattern](#complete-sync-pattern)

## Overview

Delta queries enable efficient synchronization by:
1. First query returns all data plus a `@odata.deltaLink`
2. Subsequent queries use the delta link to get only changes
3. Changes include added, modified, and deleted entities

## Initial Query

Start with a regular query that requests a delta link:

```csharp
// Get initial data
var query = client.For<Product>("Products")
    .Filter("Price gt 50")
    .OrderBy("Name");

var response = await client.GetAllAsync(query);

// Store the delta link for future sync
var deltaLink = response.DeltaLink;

// Process initial data
foreach (var product in response.Value)
{
    SaveToLocalDatabase(product);
}

// Save delta link for later
await SaveDeltaLink(deltaLink);
```

## Getting Changes

Use the delta link to get changes since the last query:

```csharp
// Load previously saved delta link
var deltaLink = await LoadDeltaLink();

// Get changes
var delta = await client.GetDeltaAsync<Product>(deltaLink);

// Process added/modified entities
foreach (var product in delta.Value)
{
    UpdateLocalDatabase(product);
}

// Process deleted entities
foreach (var deleted in delta.Deleted)
{
    DeleteFromLocalDatabase(deleted.Id);
}

// Save new delta link for next sync
await SaveDeltaLink(delta.DeltaLink);
```

### Get All Delta Pages

If changes span multiple pages:

```csharp
var deltaLink = await LoadDeltaLink();

// Automatically follows nextLink pagination
var delta = await client.GetAllDeltaAsync<Product>(deltaLink);

Console.WriteLine($"Modified: {delta.Value.Count}");
Console.WriteLine($"Deleted: {delta.Deleted.Count}");

await SaveDeltaLink(delta.DeltaLink);
```

## Understanding Delta Responses

### ODataDeltaResponse Structure

```csharp
public class ODataDeltaResponse<T>
{
    // Added or modified entities
    public List<T> Value { get; }
    
    // Deleted entity information
    public List<ODataDeletedEntity> Deleted { get; }
    
    // Link for next sync
    public string? DeltaLink { get; set; }
    
    // Link for next page (if paginated)
    public string? NextLink { get; set; }
    
    // Total count (if requested)
    public long? Count { get; set; }
}
```

### Deleted Entity Information

```csharp
public class ODataDeletedEntity
{
    // Entity identifier (usually the @odata.id)
    public string? Id { get; set; }
    
    // Reason for deletion: "deleted" or "changed"
    // "changed" means entity no longer matches filter
    public string? Reason { get; set; }
}
```

### Processing Deleted Entities

```csharp
var delta = await client.GetDeltaAsync<Product>(deltaLink);

foreach (var deleted in delta.Deleted)
{
    if (deleted.Reason == "deleted")
    {
        // Entity was actually deleted
        DeleteFromLocalDatabase(deleted.Id);
    }
    else if (deleted.Reason == "changed")
    {
        // Entity still exists but no longer matches filter
        // e.g., Price changed from 60 to 40 (now < 50)
        RemoveFromFilteredView(deleted.Id);
    }
}
```

## Complete Sync Pattern

### Initial Sync

```csharp
public async Task<string?> PerformInitialSync()
{
    var query = client.For<Product>("Products")
        .Select("Id,Name,Price,ModifiedDate")
        .OrderBy("Id");

    var response = await client.GetAllAsync(query);
    
    // Clear and repopulate local database
    await ClearLocalProducts();
    await BulkInsertProducts(response.Value);
    
    Console.WriteLine($"Initial sync: {response.Value.Count} products");
    
    return response.DeltaLink;
}
```

### Incremental Sync

```csharp
public async Task<string?> PerformIncrementalSync(string deltaLink)
{
    var delta = await client.GetAllDeltaAsync<Product>(deltaLink);
    
    // Apply changes
    var added = 0;
    var updated = 0;
    var deleted = 0;
    
    foreach (var product in delta.Value)
    {
        if (await ExistsLocally(product.Id))
        {
            await UpdateLocal(product);
            updated++;
        }
        else
        {
            await InsertLocal(product);
            added++;
        }
    }
    
    foreach (var deletedEntity in delta.Deleted)
    {
        await DeleteLocal(deletedEntity.Id);
        deleted++;
    }
    
    Console.WriteLine($"Sync complete: +{added}, ~{updated}, -{deleted}");
    
    return delta.DeltaLink;
}
```

### Full Sync Service

```csharp
public class ProductSyncService
{
    private readonly ODataClient _client;
    private string? _deltaLink;
    
    public ProductSyncService(ODataClient client)
    {
        _client = client;
    }
    
    public async Task SyncAsync()
    {
        if (string.IsNullOrEmpty(_deltaLink))
        {
            // First sync
            _deltaLink = await PerformInitialSync();
        }
        else
        {
            // Incremental sync
            try
            {
                _deltaLink = await PerformIncrementalSync(_deltaLink);
            }
            catch (ODataNotFoundException)
            {
                // Delta link expired, perform full sync
                Console.WriteLine("Delta link expired, performing full sync");
                _deltaLink = await PerformInitialSync();
            }
        }
    }
    
    private async Task<string?> PerformInitialSync()
    {
        var query = _client.For<Product>("Products");
        var response = await _client.GetAllAsync(query);
        
        await SaveProducts(response.Value);
        return response.DeltaLink;
    }
    
    private async Task<string?> PerformIncrementalSync(string deltaLink)
    {
        var delta = await _client.GetAllDeltaAsync<Product>(deltaLink);
        
        await ApplyChanges(delta.Value, delta.Deleted);
        return delta.DeltaLink;
    }
    
    private async Task ApplyChanges(
        List<Product> modified, 
        List<ODataDeletedEntity> deleted)
    {
        foreach (var product in modified)
        {
            await UpsertProduct(product);
        }
        
        foreach (var del in deleted)
        {
            await DeleteProduct(del.Id);
        }
    }
}
```

## Best Practices

1. **Store delta links persistently** - Save them to database or file
2. **Handle expired links** - Fall back to full sync if delta link fails
3. **Consider token lifespan** - Some servers expire delta tokens after time period
4. **Filter carefully** - Entities that stop matching filter appear as "deleted" with reason "changed"
5. **Handle pagination** - Use `GetAllDeltaAsync` to follow all pages

## Error Handling

```csharp
try
{
    var delta = await client.GetDeltaAsync<Product>(deltaLink);
    // Process changes...
}
catch (ODataNotFoundException)
{
    // Delta link expired or invalid
    Console.WriteLine("Delta link expired, performing full resync");
    await PerformInitialSync();
}
catch (ODataClientException ex) when (ex.StatusCode == 410)
{
    // 410 Gone - delta link no longer valid
    Console.WriteLine("Delta link gone, performing full resync");
    await PerformInitialSync();
}
```
