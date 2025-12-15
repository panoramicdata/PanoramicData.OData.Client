# PanoramicData.OData.Client

A lightweight, modern OData V4 client library for .NET 10.

## Features

- **LINQ-like query builder** - Build OData queries using familiar C# syntax
- **Full CRUD support** - Create, Read, Update, and Delete entities
- **Pagination support** - Automatic handling of `@odata.nextLink` for large result sets
- **Function and Action support** - Call OData functions and actions
- **Retry logic** - Built-in retry handling for transient failures
- **Logging support** - Inject `ILogger` for detailed request/response logging
- **Customizable** - Configure headers, timeouts, and JSON serialization

## Installation

```bash
dotnet add package PanoramicData.OData.Client
```

## Quick Start

```csharp
using PanoramicData.OData.Client;

// Create the client
var client = new ODataClient(new ODataClientOptions
{
    BaseUrl = "https://services.odata.org/V4/OData/OData.svc/",
    ConfigureRequest = request =>
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "your-token");
    }
});

// Query entities
var query = client.For<Product>("Products")
    .Filter("Price gt 100")
    .OrderBy("Name")
    .Top(10);

var response = await client.GetAsync(query);

// Get all pages automatically
var allProducts = await client.GetAllAsync(query, cancellationToken);

// Get by key
var product = await client.GetByKeyAsync<Product, int>(123);

// Create
var newProduct = await client.CreateAsync("Products", new Product { Name = "Widget" });

// Update (PATCH)
var updated = await client.UpdateAsync<Product>("Products", 123, new { Price = 150.00 });

// Delete
await client.DeleteAsync("Products", 123);
```

## Entity Model Example

```csharp
using System.Text.Json.Serialization;

public class Product
{
    [JsonPropertyName("ID")]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public DateTimeOffset? ReleaseDate { get; set; }
    
    public int? Rating { get; set; }
    
    public decimal? Price { get; set; }
}
```

## Query Builder Features

```csharp
// Filtering with OData expressions
var query = client.For<Product>("Products")
    .Filter("Rating gt 3")
    .Top(3);

// Select specific fields
var query = client.For<Product>("Products")
    .Select("ID,Name,Price")
    .Top(3);

// Expand navigation properties
var query = client.For<Product>("Products")
    .Expand("Category,Supplier");

// Ordering
var query = client.For<Product>("Products")
    .OrderBy("Price desc")
    .Top(5);

// Paging
var query = client.For<Product>("Products")
    .Skip(20)
    .Top(10)
    .Count();

// Search
var query = client.For<Product>("Products")
    .Search("widget");

// Custom headers per query
var query = client.For<Product>("Products")
    .WithHeader("Prefer", "return=representation");

// Combine multiple options
var query = client.For<Product>("Products")
    .Filter("Rating gt 3")
    .Select("ID,Name,Price")
    .OrderBy("Price desc")
    .Top(10);
```

## Raw OData Queries

```csharp
// Use raw filter strings for complex scenarios
var query = client.For<Product>("Products")
    .Filter("contains(tolower(Name), 'widget')");

// Get raw JSON response
var json = await client.GetRawAsync("Products?$filter=Price gt 100");
```

## OData Functions and Actions

```csharp
// Call a function
var query = client.For<Product>("Products")
    .Function("Microsoft.Dynamics.CRM.SearchProducts", new { SearchTerm = "widget" });
var result = await client.CallFunctionAsync<Product, List<Product>>(query);

// Call an action
var response = await client.CallActionAsync<OrderResult>(
    "Orders(123)/Microsoft.Dynamics.CRM.Ship",
    new { TrackingNumber = "ABC123" });
```

## Logging with Dependency Injection

The client supports `ILogger` for detailed request/response logging:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PanoramicData.OData.Client;

// Set up dependency injection with logging
var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = false;
            options.TimestampFormat = "HH:mm:ss.fff ";
        });
});

var serviceProvider = services.BuildServiceProvider();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

// Create the ODataClient with logging enabled
var logger = loggerFactory.CreateLogger<ODataClient>();
var client = new ODataClient(new ODataClientOptions
{
    BaseUrl = "https://services.odata.org/V4/OData.svc/",
    Logger = logger,
    RetryCount = 3,
    RetryDelay = TimeSpan.FromMilliseconds(500)
});

// Now all requests will be logged with full details
var query = client.For<Product>("Products").Top(5);
var response = await client.GetAsync(query);
```

### Sample Log Output

```
12:34:56.789 dbug: PanoramicData.OData.Client.ODataClient[0]
      GetAsync<Product> - URL: Products?$top=5
12:34:56.890 dbug: PanoramicData.OData.Client.ODataClient[0]
      CreateRequest - GET Products?$top=5
12:34:57.123 dbug: PanoramicData.OData.Client.ODataClient[0]
      SendWithRetryAsync - Received OK from Products?$top=5
12:34:57.145 dbug: PanoramicData.OData.Client.ODataClient[0]
      GetAsync<Product> - Response received, content length: 1234
12:34:57.156 dbug: PanoramicData.OData.Client.ODataClient[0]
      GetAsync<Product> - Parsed 5 items from 'value' array
```

## Configuration Options

```csharp
var client = new ODataClient(new ODataClientOptions
{
    // Required: Base URL of the OData service
    BaseUrl = "https://api.example.com/odata",
    
    // Optional: Request timeout (default: 5 minutes)
    Timeout = TimeSpan.FromMinutes(5),
    
    // Optional: Retry configuration for transient failures
    RetryCount = 3,
    RetryDelay = TimeSpan.FromSeconds(1),
    
    // Optional: Provide your own HttpClient
    HttpClient = existingHttpClient,
    
    // Optional: ILogger for debug logging
    Logger = loggerInstance,
    
    // Optional: Custom JSON serialization settings
    JsonSerializerOptions = customOptions,
    
    // Optional: Configure headers for every request
    ConfigureRequest = request =>
    {
        request.Headers.Add("Custom-Header", "value");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token");
    }
});
```

## Exception Handling

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
    // 401 - Unauthorized
    Console.WriteLine($"Unauthorized: {ex.ResponseBody}");
}
catch (ODataForbiddenException ex)
{
    // 403 - Forbidden
    Console.WriteLine($"Forbidden: {ex.ResponseBody}");
}
catch (ODataClientException ex)
{
    // Other errors
    Console.WriteLine($"Status: {ex.StatusCode}, Body: {ex.ResponseBody}");
}
```

## Testing

The library can be tested against the public OData sample services:

```csharp
// Read-only sample service
const string ODataV4ReadOnlyUri = "https://services.odata.org/V4/OData/OData.svc/";

// Read-write sample service (creates unique session)
const string ODataV4ReadWriteUri = "https://services.odata.org/V4/OData/%28S%28readwrite%29%29/OData.svc/";

// Northwind sample service
const string NorthwindV4ReadOnlyUri = "https://services.odata.org/V4/Northwind/Northwind.svc/";

// TripPin sample service
const string TripPinV4ReadWriteUri = "https://services.odata.org/V4/TripPinServiceRW/";
```

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.
