# Service Metadata

OData V4 services expose metadata that describes the data model, including entity types, properties, relationships, and available operations.

## Table of Contents

- [Overview](#overview)
- [Retrieving Metadata](#retrieving-metadata)
- [Understanding Metadata](#understanding-metadata)
- [Service Document](#service-document)
- [Using Metadata](#using-metadata)
- [Metadata Caching](#metadata-caching)

## Overview

OData services provide two discovery mechanisms:

1. **Service Document** (`/`) - Lists available entity sets, singletons, and function imports
2. **Metadata Document** (`/$metadata`) - Full CSDL schema definition

## Retrieving Metadata

### Get Parsed Metadata

```csharp
// Get structured metadata
var metadata = await client.GetMetadataAsync(cancellationToken);

Console.WriteLine($"Namespace: {metadata.Namespace}");
Console.WriteLine($"Entity Types: {metadata.EntityTypes.Count}");
Console.WriteLine($"Entity Sets: {metadata.EntitySets.Count}");
```

### Get Raw Metadata XML

```csharp
// Get raw CSDL XML
var xml = await client.GetMetadataXmlAsync(cancellationToken);
Console.WriteLine(xml);
```

## Understanding Metadata

### Entity Types

```csharp
var metadata = await client.GetMetadataAsync();

foreach (var entityType in metadata.EntityTypes)
{
    Console.WriteLine($"\nEntity: {entityType.Name}");
    Console.WriteLine($"  Base Type: {entityType.BaseType ?? "none"}");
    Console.WriteLine($"  Abstract: {entityType.IsAbstract}");
    Console.WriteLine($"  Open Type: {entityType.IsOpenType}");
    Console.WriteLine($"  Has Stream: {entityType.HasStream}");
    
    // Key properties
    Console.WriteLine($"  Key: {string.Join(", ", entityType.Key)}");
    
    // Properties
    foreach (var prop in entityType.Properties)
    {
        var nullable = prop.IsNullable ? "?" : "";
        Console.WriteLine($"  - {prop.Name}: {prop.Type}{nullable}");
    }
    
    // Navigation properties
    foreach (var nav in entityType.NavigationProperties)
    {
        var collection = nav.IsCollection ? "Collection" : "Single";
        Console.WriteLine($"  -> {nav.Name}: {nav.TargetType} ({collection})");
    }
}
```

### Entity Sets

```csharp
foreach (var entitySet in metadata.EntitySets)
{
    Console.WriteLine($"Entity Set: {entitySet.Name}");
    Console.WriteLine($"  Type: {entitySet.EntityType}");
    
    // Navigation property bindings
    foreach (var binding in entitySet.NavigationPropertyBindings)
    {
        Console.WriteLine($"  Binding: {binding.Path} -> {binding.Target}");
    }
}
```

### Singletons

```csharp
foreach (var singleton in metadata.Singletons)
{
    Console.WriteLine($"Singleton: {singleton.Name}");
    Console.WriteLine($"  Type: {singleton.Type}");
}
```

### Complex Types

```csharp
foreach (var complexType in metadata.ComplexTypes)
{
    Console.WriteLine($"Complex Type: {complexType.Name}");
    
    foreach (var prop in complexType.Properties)
    {
        Console.WriteLine($"  - {prop.Name}: {prop.Type}");
    }
}
```

### Enum Types

```csharp
foreach (var enumType in metadata.EnumTypes)
{
    Console.WriteLine($"Enum: {enumType.Name}");
    Console.WriteLine($"  Underlying Type: {enumType.UnderlyingType}");
    Console.WriteLine($"  Is Flags: {enumType.IsFlags}");
    
    foreach (var member in enumType.Members)
    {
        Console.WriteLine($"  - {member.Name} = {member.Value}");
    }
}
```

### Functions and Actions

```csharp
// Function imports (unbound functions)
foreach (var func in metadata.FunctionImports)
{
    Console.WriteLine($"Function: {func.Name}");
    Console.WriteLine($"  Reference: {func.Function}");
    Console.WriteLine($"  Entity Set: {func.EntitySet}");
}

// Action imports (unbound actions)
foreach (var action in metadata.ActionImports)
{
    Console.WriteLine($"Action: {action.Name}");
    Console.WriteLine($"  Reference: {action.Action}");
}
```

## Service Document

The service document lists all available resources:

```csharp
var serviceDoc = await client.GetServiceDocumentAsync();

Console.WriteLine($"Context: {serviceDoc.Context}", cancellationToken);

// Entity Sets
Console.WriteLine("\nEntity Sets:");
foreach (var entitySet in serviceDoc.EntitySets)
{
    Console.WriteLine($"  - {entitySet.Name} ({entitySet.Url})");
}

// Singletons
Console.WriteLine("\nSingletons:");
foreach (var singleton in serviceDoc.Singletons)
{
    Console.WriteLine($"  - {singleton.Name} ({singleton.Url})");
}

// Function Imports
Console.WriteLine("\nFunction Imports:");
foreach (var func in serviceDoc.FunctionImports)
{
    Console.WriteLine($"  - {func.Name} ({func.Url})");
}
```

### Find a Resource

```csharp
var serviceDoc = await client.GetServiceDocumentAsync();

var products = serviceDoc.GetResource("Products");
if (products != null)
{
    Console.WriteLine($"Products available at: {products.Url}");
}
```

## Using Metadata

### Validate Entity Set Exists

```csharp
var metadata = await client.GetMetadataAsync();

var entitySet = metadata.GetEntitySet("Products");
if (entitySet != null)
{
    Console.WriteLine($"Products contains: {entitySet.EntityType}");
}
else
{
    Console.WriteLine("Products entity set not found");
}
```

### Get Entity Type Information

```csharp
var metadata = await client.GetMetadataAsync();
var productType = metadata.GetEntityType("Product");

if (productType != null)
{
    // Get specific property
    var priceProperty = productType.GetProperty("Price");
    if (priceProperty != null)
    {
        Console.WriteLine($"Price type: {priceProperty.Type}");
        Console.WriteLine($"Price nullable: {priceProperty.IsNullable}");
        Console.WriteLine($"Price precision: {priceProperty.Precision}");
    }
    
    // Get navigation property
    var categoryNav = productType.GetNavigationProperty("Category");
    if (categoryNav != null)
    {
        Console.WriteLine($"Category is collection: {categoryNav.IsCollection}");
        Console.WriteLine($"Category target: {categoryNav.TargetType}");
    }
}
```

### Dynamic Service Discovery

```csharp
public async Task<List<string>> GetAvailableEntitySets()
{
    var serviceDoc = await client.GetServiceDocumentAsync();
    return serviceDoc.EntitySets.Select(e => e.Name).ToList();
}

public async Task<List<string>> GetEntityProperties(string entityTypeName)
{
    var metadata = await client.GetMetadataAsync();
    var entityType = metadata.GetEntityType(entityTypeName);
    
    return entityType?.Properties.Select(p => p.Name).ToList() 
        ?? new List<string>();
}
```

## Metadata Caching

OData metadata rarely changes during application runtime. The client provides built-in caching to improve performance and reduce unnecessary network calls.

### Enabling Caching

To enable metadata caching, set `MetadataCacheDuration` in your client options:

```csharp
var client = new ODataClient(new ODataClientOptions
{
    BaseUrl = "https://api.example.com/odata",
    MetadataCacheDuration = TimeSpan.FromHours(1) // Cache for 1 hour
});

// First call fetches from server
var metadata1 = await client.GetMetadataAsync(cancellationToken);

// Subsequent calls return cached data (no network request)
var metadata2 = await client.GetMetadataAsync(cancellationToken);
```

### Force Refresh

To bypass the cache and fetch fresh metadata from the server, use `CacheHandling.ForceRefresh`:

```csharp
// Force a refresh, ignoring cached data
var freshMetadata = await client.GetMetadataAsync(CacheHandling.ForceRefresh, cancellationToken);

// Also works for raw XML
var freshXml = await client.GetMetadataXmlAsync(CacheHandling.ForceRefresh, cancellationToken);
```

### Invalidating the Cache

If you know the metadata has changed (e.g., after a schema deployment), you can manually invalidate the cache:

```csharp
// Clear the cached metadata
client.InvalidateMetadataCache();

// Next call will fetch from the server
var metadata = await client.GetMetadataAsync(cancellationToken);
```

### CacheHandling Enum

| Value | Description |
|-------|-------------|
| `CacheHandling.Default` | Use cached data if available and not expired |
| `CacheHandling.ForceRefresh` | Bypass cache and fetch fresh data from server |

### Caching Behavior

| MetadataCacheDuration | Behavior |
|-----------------------|----------|
| `null` (default) | No caching - each call fetches from server |
| `TimeSpan.FromHours(1)` | Cache for 1 hour |
| `TimeSpan.MaxValue` | Cache indefinitely (until invalidated) |

Both `GetMetadataAsync()` and `GetMetadataXmlAsync()` share the same cache, so fetching one will benefit the other.

## OData Type Mapping

Common OData EDM types and their .NET equivalents:

| EDM Type | .NET Type |
|----------|-----------|
| Edm.String | string |
| Edm.Int32 | int |
| Edm.Int64 | long |
| Edm.Int16 | short |
| Edm.Boolean | bool |
| Edm.Decimal | decimal |
| Edm.Double | double |
| Edm.Single | float |
| Edm.DateTime | DateTime |
| Edm.DateTimeOffset | DateTimeOffset |
| Edm.Guid | Guid |
| Edm.Binary | byte[] |
| Collection(Edm.String) | List\<string\>|
