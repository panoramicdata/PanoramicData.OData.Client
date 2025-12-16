# PanoramicData.OData.Client

A lightweight, modern OData V4 client library for .NET 10.

## OData V4 Feature Support

| Feature | Status | Documentation |
|---------|--------|---------------|
| **Querying** | | |
| $filter | ✅ Supported | [Querying](Documentation/querying.md#filtering-filter) |
| $select | ✅ Supported | [Querying](Documentation/querying.md#selecting-fields-select) |
| $expand | ✅ Supported | [Querying](Documentation/querying.md#expanding-related-entities-expand) |
| $orderby | ✅ Supported | [Querying](Documentation/querying.md#ordering-results-orderby) |
| $top / $skip | ✅ Supported | [Querying](Documentation/querying.md#paging-skip-top) |
| $count | ✅ Supported | [Querying](Documentation/querying.md#counting-results-count) |
| $search | ✅ Supported | [Querying](Documentation/querying.md#searching-search) |
| $apply (Aggregations) | ✅ Supported | [Querying](Documentation/querying.md#aggregations-apply) |
| $compute | ✅ Supported | [Querying](Documentation/querying.md#computed-properties-compute) |
| Lambda operators (any/all) | ✅ Supported | [Querying](Documentation/querying.md#filtering-filter) |
| Type casting (derived types) | ✅ Supported | [Querying](Documentation/querying.md#derived-types-type-casting) |
| **CRUD Operations** | | |
| Create (POST) | ✅ Supported | [CRUD](Documentation/crud.md#creating-entities) |
| Read (GET) | ✅ Supported | [CRUD](Documentation/crud.md#reading-entities) |
| Update (PATCH) | ✅ Supported | [CRUD](Documentation/crud.md#updating-entities-patch) |
| Replace (PUT) | ✅ Supported | [CRUD](Documentation/crud.md#replacing-entities-put) |
| Delete (DELETE) | ✅ Supported | [CRUD](Documentation/crud.md#deleting-entities) |
| **Batch Operations** | | |
| Batch requests | ✅ Supported | [Batch](Documentation/batch.md#creating-a-batch) |
| Changesets (atomic) | ✅ Supported | [Batch](Documentation/batch.md#changesets-atomic-transactions) |
| **Singleton Entities** | | |
| Get singleton | ✅ Supported | [Singletons](Documentation/singletons.md#getting-a-singleton) |
| Update singleton | ✅ Supported | [Singletons](Documentation/singletons.md#updating-a-singleton) |
| **Media Entities & Streams** | | |
| Get stream ($value) | ✅ Supported | [Streams](Documentation/streams.md#media-entities) |
| Set stream | ✅ Supported | [Streams](Documentation/streams.md#setting-stream-content) |
| Named stream properties | ✅ Supported | [Streams](Documentation/streams.md#named-stream-properties) |
| **Entity References ($ref)** | | |
| Add reference | ✅ Supported | [References](Documentation/references.md#adding-references-collection) |
| Remove reference | ✅ Supported | [References](Documentation/references.md#removing-references-collection) |
| Set reference | ✅ Supported | [References](Documentation/references.md#setting-references-single-valued) |
| Delete reference | ✅ Supported | [References](Documentation/references.md#deleting-references-single-valued) |
| **Delta Queries** | | |
| Delta tracking | ✅ Supported | [Delta](Documentation/delta.md#overview) |
| Deleted entities | ✅ Supported | [Delta](Documentation/delta.md#understanding-delta-responses) |
| Delta pagination | ✅ Supported | [Delta](Documentation/delta.md#getting-changes) |
| **Service Metadata** | | |
| $metadata | ✅ Supported | [Metadata](Documentation/metadata.md#retrieving-metadata) |
| Service document | ✅ Supported | [Metadata](Documentation/metadata.md#service-document) |
| **Functions & Actions** | | |
| Bound functions | ✅ Supported | [Functions & Actions](Documentation/functions-actions.md#bound-functions-entity-set) |
| Unbound functions | ✅ Supported | [Functions & Actions](Documentation/functions-actions.md#unbound-functions) |
| Bound actions | ✅ Supported | [Functions & Actions](Documentation/functions-actions.md#calling-actions) |
| Unbound actions | ✅ Supported | [Functions & Actions](Documentation/functions-actions.md#unbound-action) |
| **Async Operations** | | |
| Prefer: respond-async | ✅ Supported | [Async](Documentation/async-operations.md#async-action-calls) |
| Status polling | ✅ Supported | [Async](Documentation/async-operations.md#polling-for-completion) |
| **Advanced Features** | | |
| Cross-join ($crossjoin) | ✅ Supported | [Cross-Join](Documentation/cross-join.md#overview) |
| Open types | ✅ Supported | [Open Types](Documentation/open-types.md#overview) |
| ETag concurrency | ✅ Supported | [ETag & Concurrency](Documentation/etag-concurrency.md#overview) |
| Server-driven paging | ✅ Supported | [Querying](Documentation/querying.md#server-driven-paging) |
| Retry logic | ✅ Supported | [Configuration](#configuration-options) |
| Custom headers | ✅ Supported | [Querying](Documentation/querying.md#custom-headers) |

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
catch (ODataConcurrencyException ex)
{
    // 412 - ETag mismatch
    Console.WriteLine($"Concurrency conflict: {ex.RequestETag} vs {ex.CurrentETag}");
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

## Documentation

For detailed documentation on each feature, see the Documentation folder:

- [Querying Data](Documentation/querying.md) - Filter, select, expand, order, page, search, aggregate
- [CRUD Operations](Documentation/crud.md) - Create, read, update, delete entities
- [Batch Operations](Documentation/batch.md) - Multiple operations in single request
- [Singletons](Documentation/singletons.md) - Single-instance entities like /Me
- [Media & Streams](Documentation/streams.md) - Binary data and media entities
- [Entity References](Documentation/references.md) - Managing relationships with $ref
- [Delta Queries](Documentation/delta.md) - Change tracking and synchronization
- [Service Metadata](Documentation/metadata.md) - Discovery and schema information
- [Functions & Actions](Documentation/functions-actions.md) - Custom operations
- [Async Operations](Documentation/async-operations.md) - Long-running operations
- [Cross-Join](Documentation/cross-join.md) - Combining multiple entity sets
- [Open Types](Documentation/open-types.md) - Dynamic properties
- [ETag & Concurrency](Documentation/etag-concurrency.md) - Optimistic concurrency control

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.
