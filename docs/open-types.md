# Open Types (Dynamic Properties)

OData V4 supports open types - entity types that can have dynamic properties not defined in the schema. This allows entities to store additional key-value pairs beyond their declared properties.

## Table of Contents

- [Overview](#overview)
- [Defining Open Type Entities](#defining-open-type-entities)
- [Reading Dynamic Properties](#reading-dynamic-properties)
- [Writing Dynamic Properties](#writing-dynamic-properties)
- [Type Conversions](#type-conversions)
- [Complete Example](#complete-example)

## Overview

Open types allow entities to include properties not declared in the metadata. Common use cases:
- Custom fields added by users
- Extension attributes
- Dynamic metadata
- Tenant-specific properties

## Defining Open Type Entities

Inherit from `ODataOpenType` to support dynamic properties:

```csharp
using PanoramicData.OData.Client;

public class Contact : ODataOpenType
{
    // Declared properties
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Dynamic properties are handled by the base class
}
```

## Reading Dynamic Properties

### Check If Property Exists

```csharp
var contact = await client.GetByKeyAsync<Contact, int>(1);

if (contact.HasDynamicProperty("CustomField1"))
{
    Console.WriteLine("CustomField1 exists!");
}
```

### Get String Property

```csharp
var contact = await client.GetByKeyAsync<Contact, int>(1);

var customValue = contact.GetDynamicString("CustomField1");
Console.WriteLine($"Custom field: {customValue}");
```

### Get Typed Properties

```csharp
// Integer
int? count = contact.GetDynamicInt("ViewCount");

// Boolean
bool? isActive = contact.GetDynamicBool("IsActive");

// Decimal/Double
decimal? rating = contact.GetDynamicDecimal("Rating");
double? score = contact.GetDynamicDouble("Score");

// DateTime
DateTime? createdAt = contact.GetDynamicDateTime("CustomCreatedAt");
DateTimeOffset? modifiedAt = contact.GetDynamicDateTimeOffset("CustomModifiedAt");

// Guid
Guid? externalId = contact.GetDynamicGuid("ExternalSystemId");
```

### Get Complex Type Property

```csharp
// Complex object stored as dynamic property
var address = contact.GetDynamicProperty<Address>("CustomAddress");

if (address != null)
{
    Console.WriteLine($"Street: {address.Street}");
    Console.WriteLine($"City: {address.City}");
}
```

### Get All Dynamic Property Names

```csharp
var contact = await client.GetByKeyAsync<Contact, int>(1);

foreach (var propertyName in contact.GetDynamicPropertyNames())
{
    Console.WriteLine($"Dynamic property: {propertyName}");
}
```

## Writing Dynamic Properties

### Set Dynamic Property

```csharp
var contact = new Contact
{
    FirstName = "John",
    LastName = "Doe",
    Email = "john@example.com"
};

// Add dynamic properties
contact.SetDynamicProperty("Department", "Engineering");
contact.SetDynamicProperty("EmployeeNumber", 12345);
contact.SetDynamicProperty("StartDate", DateTime.Today);
contact.SetDynamicProperty("IsManager", true);

// Create the entity with dynamic properties
var created = await client.CreateAsync("Contacts", contact);
```

### Set Complex Type Property

```csharp
contact.SetDynamicProperty("CustomAddress", new Address
{
    Street = "123 Main St",
    City = "Seattle",
    PostalCode = "98101"
});
```

### Update Dynamic Properties

```csharp
// Dynamic properties can be updated like any other
await client.UpdateAsync<Contact>(
    "Contacts", 
    1, 
    new 
    { 
        Department = "Marketing",  // Update dynamic property
        EmployeeNumber = 54321
    }
);
```

### Remove Dynamic Property

```csharp
var contact = await client.GetByKeyAsync<Contact, int>(1);

if (contact.HasDynamicProperty("ObsoleteField"))
{
    bool removed = contact.RemoveDynamicProperty("ObsoleteField");
    Console.WriteLine($"Removed: {removed}");
}
```

## Type Conversions

### Get with Default Value

```csharp
// Returns default if property doesn't exist
int count = contact.GetDynamicInt("ViewCount") ?? 0;
bool isActive = contact.GetDynamicBool("IsActive") ?? false;
string? category = contact.GetDynamicString("Category") ?? "Uncategorized";
```

### Try Get Pattern

```csharp
if (contact.TryGetDynamicProperty<decimal>("CustomPrice", out var price))
{
    Console.WriteLine($"Price: {price:C}");
}
else
{
    Console.WriteLine("No custom price set");
}
```

### Handle Missing Properties

```csharp
var rating = contact.GetDynamicInt("Rating");
if (rating.HasValue)
{
    Console.WriteLine($"Rating: {rating.Value}");
}
else
{
    Console.WriteLine("Not rated");
}
```

## Complete Example

### Entity Model

```csharp
public class Product : ODataOpenType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? CategoryId { get; set; }
}

public class CustomDimensions
{
    public double? Height { get; set; }
    public double? Width { get; set; }
    public double? Depth { get; set; }
    public string? Unit { get; set; }
}
```

### Working with Dynamic Properties

```csharp
// Create product with custom fields
var product = new Product
{
    Name = "Custom Widget",
    Price = 29.99m
};

// Add various dynamic properties
product.SetDynamicProperty("SKU", "WDGT-001");
product.SetDynamicProperty("InStock", true);
product.SetDynamicProperty("StockCount", 150);
product.SetDynamicProperty("LastRestocked", DateTime.UtcNow);
product.SetDynamicProperty("Dimensions", new CustomDimensions
{
    Height = 10.5,
    Width = 5.0,
    Depth = 3.0,
    Unit = "cm"
});

// Create
var created = await client.CreateAsync("Products", product);

// Read back
var fetched = await client.GetByKeyAsync<Product, int>(created.Id);

Console.WriteLine($"Product: {fetched.Name}");
Console.WriteLine($"SKU: {fetched.GetDynamicString("SKU")}");
Console.WriteLine($"In Stock: {fetched.GetDynamicBool("InStock")}");
Console.WriteLine($"Stock Count: {fetched.GetDynamicInt("StockCount")}");
Console.WriteLine($"Last Restocked: {fetched.GetDynamicDateTime("LastRestocked")}");

var dimensions = fetched.GetDynamicProperty<CustomDimensions>("Dimensions");
if (dimensions != null)
{
    Console.WriteLine($"Size: {dimensions.Height}x{dimensions.Width}x{dimensions.Depth} {dimensions.Unit}");
}

// List all custom properties
Console.WriteLine("\nAll dynamic properties:");
foreach (var propName in fetched.GetDynamicPropertyNames())
{
    Console.WriteLine($"  - {propName}");
}
```

### Query with Dynamic Properties

```csharp
// Filter by dynamic property (if server supports it)
var query = client.For<Product>("Products")
    .Filter("SKU eq 'WDGT-001'");

var products = await client.GetAsync(query);
```

## Serialization

Dynamic properties are serialized alongside declared properties:

```json
{
    "Id": 1,
    "Name": "Custom Widget",
    "Price": 29.99,
    "SKU": "WDGT-001",
    "InStock": true,
    "StockCount": 150,
    "LastRestocked": "2024-01-15T10:30:00Z",
    "Dimensions": {
        "Height": 10.5,
        "Width": 5.0,
        "Depth": 3.0,
        "Unit": "cm"
    }
}
```

## Best Practices

1. **Use declared properties when possible** - Only use dynamic properties for truly variable data
2. **Document expected dynamic properties** - Even if not in schema, document commonly used fields
3. **Handle missing properties gracefully** - Always check for existence or use null-coalescing
4. **Use consistent types** - Don't store same property as different types across entities
5. **Consider validation** - Validate dynamic property values in your application layer
