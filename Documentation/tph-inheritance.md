# Table-Per-Hierarchy (TPH) Inheritance Support

This document explains how to work with Entity Framework Table-Per-Hierarchy (TPH) inheritance when using the OData client.

## Problem

When using `CreateAsync<T>()` with an entity that uses TPH inheritance, the server needs to know the specific derived type being sent. This is done through the `@odata.type` annotation in the JSON payload.

## Solution

The `ODataTypeAnnotationConverter` automatically adds the `@odata.type` annotation when serializing derived types.

## Usage

### Basic Example

```csharp
// Define your entity hierarchy
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Employee : Person
{
    public string Department { get; set; }
    public decimal Salary { get; set; }
}

// Create an employee using the OData client
var client = new ODataClient(new ODataClientOptions
{
    BaseUrl = "https://api.example.com/odata"
});

var newEmployee = new Employee
{
    Name = "John Doe",
    Department = "Engineering",
    Salary = 75000
};

// When CreateAsync is called with a derived type, the @odata.type annotation is automatically included
var created = await client.CreateAsync<Person>("People", newEmployee);
```

**Generated JSON:**
```json
{
  "@odata.type": "#YourNamespace.Employee",
  "name": "John Doe",
  "department": "Engineering",
  "salary": 75000
}
```

> **Note:** The namespace in `@odata.type` is derived from the .NET type's full name (e.g. `#MyApp.Models.Employee`). Use `[ODataTypeAnnotation(TypeName = "...")]` on the class to override this with the exact name your server expects.

### Custom Type Names

You can customize the OData type name using the `ODataTypeAnnotationAttribute`. The `#` prefix is optional - it will be added automatically if omitted:

```csharp
[ODataTypeAnnotation(TypeName = "#CustomNamespace.ContractorType")]
public class Contractor : Person
{
    public string Company { get; set; }
}

// Equivalent - # prefix is added automatically
[ODataTypeAnnotation(TypeName = "CustomNamespace.ContractorType")]
public class Contractor : Person
{
    public string Company { get; set; }
}
```

### Always Include Type Annotation

For scenarios where you always want the type annotation (even when not polymorphic):

```csharp
[ODataTypeAnnotation(AlwaysInclude = true)]
public class Manager : Employee
{
    public int TeamSize { get; set; }
}
```

## How It Works

1. The `ODataTypeAnnotationConverter` is registered in the `JsonSerializerOptions` used by the OData client
2. When serializing an object, it detects if the runtime type differs from the declared type
3. If they differ (polymorphism detected), it adds the `@odata.type` annotation with the format `#Namespace.TypeName`
4. The annotation is placed at the beginning of the JSON object, as required by OData specifications

## Server-Side Requirements

Your OData server must:
1. Support TPH inheritance in your Entity Framework model
2. Have discriminator mapping configured in `OnModelCreating`:

```csharp
modelBuilder.Entity<Person>()
    .HasDiscriminator<string>("PersonType")
    .HasValue<Person>("Person")
    .HasValue<Employee>("Employee")
    .HasValue<Contractor>("Contractor");
```

3. Accept the `@odata.type` annotation during model binding

## Limitations

- The converter handles top-level objects only
- Nested polymorphic objects within properties are not currently supported
- Type names follow .NET's full type name convention (namespace + class name)

## Behaviour Notes

- **Anonymous patch objects** - when using `UpdateAsync` with an anonymous object (e.g. `new { Price = 99 }`), no `@odata.type` annotation is added, as intended
- **Types with `[JsonConverter]`** - if a class already has a `[JsonConverter]` attribute applied, the OData client respects it and does not inject `@odata.type`; this takes precedence over the TPH converter
- **Deep inheritance** - three or more levels of inheritance are supported; `@odata.type` always reflects the concrete runtime type

## Related

- [CRUD Operations](crud.md)
- [JSON Serialization](../README.md#json-serialization)
