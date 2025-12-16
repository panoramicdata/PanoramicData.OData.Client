# Singleton Entities

OData V4 singletons are special entities that exist as a single instance without a key. Common examples include `/Me` (current user) or `/Company` (current company context).

## Table of Contents

- [Overview](#overview)
- [Getting a Singleton](#getting-a-singleton)
- [Updating a Singleton](#updating-a-singleton)
- [With Concurrency Control](#with-concurrency-control)

## Overview

Unlike entity sets that contain multiple entities accessed by key (e.g., `/Products(1)`), singletons are accessed directly by name:

- `/Me` - Current user
- `/Company` - Company settings
- `/Configuration` - Application configuration

## Getting a Singleton

```csharp
// Get the current user singleton
var me = await client.GetSingletonAsync<User>("Me");
Console.WriteLine($"Current user: {me.DisplayName}");

// Get company settings
var company = await client.GetSingletonAsync<CompanySettings>("Company");
Console.WriteLine($"Company: {company.Name}");
```

### With Custom Headers

```csharp
var headers = new Dictionary<string, string>
{
    { "Accept-Language", "en-US" }
};

var me = await client.GetSingletonAsync<User>("Me", headers);
```

### Get Singleton with ETag

Retrieve the singleton along with its ETag for concurrency control:

```csharp
var result = await client.GetSingletonWithETagAsync<User>("Me");

var me = result.Value;
var etag = result.ETag;

Console.WriteLine($"User: {me.DisplayName}");
Console.WriteLine($"ETag: {etag}");
```

## Updating a Singleton

Update singleton properties using PATCH:

```csharp
var updated = await client.UpdateSingletonAsync<User>(
    "Me",
    new { DisplayName = "John Doe", Theme = "Dark" }
);

Console.WriteLine($"Updated: {updated.DisplayName}");
```

### Update Multiple Fields

```csharp
var patchValues = new
{
    DisplayName = "John Doe",
    Email = "john@example.com",
    Preferences = new
    {
        Theme = "Dark",
        Language = "en-US"
    }
};

var updated = await client.UpdateSingletonAsync<User>("Me", patchValues);
```

## With Concurrency Control

Use ETags for optimistic concurrency when updating singletons:

```csharp
// 1. Get singleton with ETag
var result = await client.GetSingletonWithETagAsync<User>("Me");
var me = result.Value;
var etag = result.ETag;

// 2. Modify and update with ETag
try
{
    var updated = await client.UpdateSingletonAsync<User>(
        "Me",
        new { DisplayName = "New Name" },
        etag  // Pass ETag for If-Match header
    );
    Console.WriteLine("Update successful");
}
catch (ODataConcurrencyException ex)
{
    Console.WriteLine($"Concurrency conflict!");
    Console.WriteLine($"Your ETag: {ex.RequestETag}");
    Console.WriteLine($"Current ETag: {ex.CurrentETag}");
    // Handle by refreshing and retrying
}
```

### Retry Pattern

```csharp
async Task<User> UpdateSingletonWithRetry(string newName, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        var result = await client.GetSingletonWithETagAsync<User>("Me");
        
        try
        {
            return await client.UpdateSingletonAsync<User>(
                "Me",
                new { DisplayName = newName },
                result.ETag
            );
        }
        catch (ODataConcurrencyException)
        {
            if (i == maxRetries - 1) throw;
            await Task.Delay(100 * (i + 1)); // Exponential backoff
        }
    }
    
    throw new InvalidOperationException("Max retries exceeded");
}
```

## Entity Model Example

```csharp
public class User
{
    public string Id { get; set; } = string.Empty;
    
    public string DisplayName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string? Theme { get; set; }
    
    public UserPreferences? Preferences { get; set; }
}

public class UserPreferences
{
    public string Theme { get; set; } = "Light";
    
    public string Language { get; set; } = "en-US";
    
    public bool NotificationsEnabled { get; set; } = true;
}
```

## Common Use Cases

### User Profile

```csharp
// Get current user profile
var me = await client.GetSingletonAsync<UserProfile>("Me");

// Update preferences
await client.UpdateSingletonAsync<UserProfile>(
    "Me",
    new { Theme = "Dark", Timezone = "UTC" }
);
```

### Application Settings

```csharp
// Get application configuration
var config = await client.GetSingletonAsync<AppConfig>("Configuration");

// Update settings (admin only)
await client.UpdateSingletonAsync<AppConfig>(
    "Configuration",
    new { MaintenanceMode = false, MaxUploadSize = 10485760 }
);
```

### Organization Context

```csharp
// Get current organization
var org = await client.GetSingletonAsync<Organization>("Organization");
Console.WriteLine($"Org: {org.Name}, Plan: {org.SubscriptionPlan}");
```
