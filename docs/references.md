# Entity References ($ref)

OData V4 supports managing relationships between entities using entity references ($ref). This allows you to link and unlink entities without affecting the entities themselves.

## Table of Contents

- [Overview](#overview)
- [Adding References (Collection)](#adding-references-collection)
- [Removing References (Collection)](#removing-references-collection)
- [Setting References (Single-Valued)](#setting-references-single-valued)
- [Deleting References (Single-Valued)](#deleting-references-single-valued)

## Overview

Entity references manage navigation property relationships:

- **Collection navigation properties**: Can have multiple related entities (e.g., Order.Items)
- **Single-valued navigation properties**: Can have one related entity (e.g., Product.Category)

Reference operations only affect the relationship, not the entities themselves.

## Adding References (Collection)

Add an entity to a collection navigation property:

```csharp
// Add a product to a category's Products collection
// POST Categories(1)/Products/$ref
// Body: { "@odata.id": "Products(42)" }

await client.AddReferenceAsync(
    "Categories",           // Source entity set
    1,                      // Source key
    "Products",             // Navigation property
    "Products",             // Target entity set
    42                      // Target key
);
```

### Example: Link Employee to Department

```csharp
// Add employee to department's Employees collection
await client.AddReferenceAsync(
    "Departments",
    "Engineering",      // Department key
    "Employees",        // Navigation property
    "Employees",        // Target entity set
    "EMP001"           // Employee key
);
```

### Example: Add Friend to Person

```csharp
// Add a friend relationship
await client.AddReferenceAsync(
    "People",
    "russellwhyte",     // Person username
    "Friends",          // Navigation property
    "People",           // Target entity set
    "scottketchum"      // Friend username
);
```

## Removing References (Collection)

Remove an entity from a collection navigation property:

```csharp
// Remove a product from a category
// DELETE Categories(1)/Products/$ref?$id=Products(42)

await client.RemoveReferenceAsync(
    "Categories",           // Source entity set
    1,                      // Source key
    "Products",             // Navigation property
    "Products",             // Target entity set
    42                      // Target key to remove
);
```

### Example: Remove Employee from Department

```csharp
await client.RemoveReferenceAsync(
    "Departments",
    "Engineering",
    "Employees",
    "Employees",
    "EMP001"
);
```

### Example: Remove Friend

```csharp
await client.RemoveReferenceAsync(
    "People",
    "russellwhyte",
    "Friends",
    "People",
    "scottketchum"
);
```

## Setting References (Single-Valued)

Set a single-valued navigation property to a specific entity:

```csharp
// Set a product's category
// PUT Products(42)/Category/$ref
// Body: { "@odata.id": "Categories(1)" }

await client.SetReferenceAsync(
    "Products",             // Source entity set
    42,                     // Source key
    "Category",             // Navigation property (single-valued)
    "Categories",           // Target entity set
    1                       // Target key
);
```

### Example: Assign Manager

```csharp
// Set an employee's manager
await client.SetReferenceAsync(
    "Employees",
    "EMP001",           // Employee
    "Manager",          // Navigation property
    "Employees",        // Manager is also an Employee
    "MGR001"           // Manager key
);
```

### Example: Set Order Customer

```csharp
// Associate an order with a customer
await client.SetReferenceAsync(
    "Orders",
    orderId,
    "Customer",
    "Customers",
    "ALFKI"
);
```

## Deleting References (Single-Valued)

Clear a single-valued navigation property (set to null):

```csharp
// Remove the category from a product
// DELETE Products(42)/Category/$ref

await client.DeleteReferenceAsync(
    "Products",             // Source entity set
    42,                     // Source key
    "Category"              // Navigation property to clear
);
```

### Example: Remove Manager

```csharp
// Clear employee's manager reference
await client.DeleteReferenceAsync(
    "Employees",
    "EMP001",
    "Manager"
);
```

## Working with Custom Headers

```csharp
var headers = new Dictionary<string, string>
{
    { "If-Match", "*" }  // Optional: concurrency control
};

await client.AddReferenceAsync(
    "Categories",
    1,
    "Products",
    "Products",
    42,
    headers
);
```

## Complete Example: Managing Order Items

```csharp
// Scenario: Managing products in an order

// 1. Add products to order
await client.AddReferenceAsync("Orders", orderId, "Items", "Products", 101);
await client.AddReferenceAsync("Orders", orderId, "Items", "Products", 102);
await client.AddReferenceAsync("Orders", orderId, "Items", "Products", 103);

// 2. Remove a product from order
await client.RemoveReferenceAsync("Orders", orderId, "Items", "Products", 102);

// 3. Set the shipping address (single-valued)
await client.SetReferenceAsync("Orders", orderId, "ShippingAddress", "Addresses", addressId);

// 4. Clear the shipping address
await client.DeleteReferenceAsync("Orders", orderId, "ShippingAddress");
```

## Complete Example: Social Network

```csharp
// Scenario: Managing friend relationships

var currentUser = "russellwhyte";
var newFriend = "scottketchum";

// Add friend
await client.AddReferenceAsync(
    "People", currentUser, "Friends",
    "People", newFriend
);

// The relationship might be bidirectional
await client.AddReferenceAsync(
    "People", newFriend, "Friends",
    "People", currentUser
);

// Remove friend
await client.RemoveReferenceAsync(
    "People", currentUser, "Friends",
    "People", newFriend
);
```

## Entity Model Considerations

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Single-valued navigation (use SetReferenceAsync/DeleteReferenceAsync)
    public Category? Category { get; set; }
    
    // Collection navigation (use AddReferenceAsync/RemoveReferenceAsync)
    public List<Tag> Tags { get; set; } = [];
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Collection navigation
    public List<Product> Products { get; set; } = [];
}
```

## Reference Operations vs. Direct Updates

| Approach | Use Case |
|----------|----------|
| Reference operations ($ref) | Managing relationships only |
| PATCH with foreign key | Update entity and relationship together |
| Deep insert | Create entity with related entities |

```csharp
// Using $ref - only changes relationship
await client.SetReferenceAsync("Products", 42, "Category", "Categories", 1);

// Using PATCH - updates entity property directly
await client.UpdateAsync<Product>("Products", 42, new { CategoryId = 1 });
```
