# Async Long-Running Operations

OData V4 supports async operations for long-running requests. The server returns 202 Accepted with a monitor URL, allowing the client to poll for completion.

## Table of Contents

- [Overview](#overview)
- [Async Action Calls](#async-action-calls)
- [Polling for Completion](#polling-for-completion)
- [Async Batch Operations](#async-batch-operations)
- [Timeout and Cancellation](#timeout-and-cancellation)

## Overview

Long-running operations flow:
1. Client sends request with `Prefer: respond-async` header
2. Server returns `202 Accepted` with `Location` header pointing to monitor URL
3. Client polls monitor URL until operation completes
4. Server returns final result when ready

## Async Action Calls

### Start Async Operation

```csharp
// Start an operation that may take time
var result = await client.CallActionAsyncWithPreferAsync<ReportResult>(
    "Reports/Namespace.GenerateLargeReport",
    new { StartDate = DateTime.Today.AddYears(-1), EndDate = DateTime.Today }
);

if (result.IsAsync)
{
    Console.WriteLine("Operation started asynchronously");
    Console.WriteLine($"Monitor URL: {result.AsyncOperation.MonitorUrl}");
    
    // Wait for completion
    var report = await result.AsyncOperation.WaitForCompletionAsync();
    Console.WriteLine($"Report generated: {report.ReportUrl}");
}
else
{
    // Server completed synchronously
    Console.WriteLine($"Completed immediately: {result.SynchronousResult.ReportUrl}");
}
```

### Simple Wait Pattern

If you just want to wait for the result regardless of sync/async:

```csharp
var report = await client.CallActionAndWaitAsync<ReportResult>(
    "Reports/Namespace.GenerateLargeReport",
    new { StartDate = DateTime.Today.AddYears(-1), EndDate = DateTime.Today },
    timeout: TimeSpan.FromMinutes(10)
);

Console.WriteLine($"Report URL: {report.ReportUrl}");
```

## Polling for Completion

### Manual Polling

```csharp
var result = await client.CallActionAsyncWithPreferAsync<ProcessResult>(
    "DataImport/Namespace.ProcessLargeFile",
    new { FileId = fileId }
);

if (result.IsAsync)
{
    var asyncOp = result.AsyncOperation;
    
    while (!asyncOp.IsCompleted)
    {
        Console.WriteLine($"Status: {asyncOp.Status}");
        
        // Poll once
        var stillRunning = await asyncOp.PollAsync();
        
        if (stillRunning)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
    
    if (asyncOp.Status == ODataAsyncOperationStatus.Completed)
    {
        var processResult = asyncOp.Result;
        Console.WriteLine($"Processed {processResult.RecordCount} records");
    }
    else
    {
        Console.WriteLine($"Failed: {asyncOp.ErrorMessage}");
    }
}
```

### Custom Poll Interval

```csharp
var result = await client.CallActionAsyncWithPreferAsync<ExportResult>(
    "Data/Namespace.Export",
    new { Format = "CSV" },
    pollInterval: TimeSpan.FromSeconds(10)  // Poll every 10 seconds
);
```

## Async Batch Operations

Execute batch requests asynchronously:

```csharp
var batch = client.CreateBatch();
batch.Create("Products", new Product { Name = "Widget 1" });
batch.Create("Products", new Product { Name = "Widget 2" });
// ... many more operations

var result = await client.ExecuteBatchAsyncWithPreferAsync(
    batch,
    pollInterval: TimeSpan.FromSeconds(5)
);

if (result.IsAsync)
{
    Console.WriteLine("Batch started asynchronously");
    
    // Wait for completion
    var batchResponse = await result.AsyncOperation.WaitForCompletionAsync(
        timeout: TimeSpan.FromMinutes(5)
    );
    
    Console.WriteLine($"Batch completed with {batchResponse.Results.Count} results");
}
else
{
    Console.WriteLine("Batch completed synchronously");
    var batchResponse = result.SynchronousResult;
}
```

## Timeout and Cancellation

### With Timeout

```csharp
try
{
    var report = await client.CallActionAndWaitAsync<ReportResult>(
        "Reports/Namespace.Generate",
        timeout: TimeSpan.FromMinutes(5)
    );
}
catch (TimeoutException)
{
    Console.WriteLine("Operation timed out");
}
```

### With Cancellation Token

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try
{
    var result = await client.CallActionAsyncWithPreferAsync<ProcessResult>(
        "Data/Namespace.Process",
        cancellationToken: cts.Token
    );
    
    if (result.IsAsync)
    {
        var processResult = await result.AsyncOperation.WaitForCompletionAsync(
            cancellationToken: cts.Token
        );
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```

### Cancel Async Operation

```csharp
var result = await client.CallActionAsyncWithPreferAsync<ReportResult>(
    "Reports/Namespace.GenerateLargeReport"
);

if (result.IsAsync)
{
    var asyncOp = result.AsyncOperation;
    
    // Start waiting but allow cancellation
    _ = Task.Run(async () =>
    {
        await Task.Delay(TimeSpan.FromMinutes(1));
        
        // Try to cancel if still running
        if (!asyncOp.IsCompleted)
        {
            var cancelled = await asyncOp.TryCancelAsync();
            Console.WriteLine($"Cancel request accepted: {cancelled}");
        }
    });
    
    try
    {
        var report = await asyncOp.WaitForCompletionAsync();
    }
    catch (ODataAsyncOperationException ex)
    {
        Console.WriteLine($"Operation failed: {ex.ErrorDetails}");
    }
}
```

## Operation Status

```csharp
public enum ODataAsyncOperationStatus
{
    Pending,    // Operation submitted but not started
    Running,    // Operation is in progress
    Completed,  // Operation completed successfully
    Failed,     // Operation failed
    Cancelled   // Operation was cancelled
}
```

## Error Handling

```csharp
try
{
    var result = await client.CallActionAndWaitAsync<ReportResult>(
        "Reports/Namespace.Generate",
        timeout: TimeSpan.FromMinutes(10)
    );
}
catch (TimeoutException ex)
{
    Console.WriteLine($"Operation timed out: {ex.Message}");
}
catch (ODataAsyncOperationException ex)
{
    Console.WriteLine($"Async operation failed");
    Console.WriteLine($"Monitor URL: {ex.MonitorUrl}");
    Console.WriteLine($"Error details: {ex.ErrorDetails}");
}
catch (ODataClientException ex)
{
    Console.WriteLine($"Request failed: {ex.StatusCode} - {ex.ResponseBody}");
}
```

## Complete Example

```csharp
public async Task<ExportResult> ExportDataAsync(ExportOptions options, IProgress<string>? progress = null)
{
    progress?.Report("Starting export...");
    
    var result = await client.CallActionAsyncWithPreferAsync<ExportResult>(
        "Data/Namespace.Export",
        options,
        pollInterval: TimeSpan.FromSeconds(5)
    );
    
    if (!result.IsAsync)
    {
        progress?.Report("Export completed immediately");
        return result.SynchronousResult!;
    }
    
    progress?.Report("Export running asynchronously...");
    
    var asyncOp = result.AsyncOperation!;
    
    while (!asyncOp.IsCompleted)
    {
        var stillRunning = await asyncOp.PollAsync();
        
        progress?.Report($"Status: {asyncOp.Status}");
        
        if (stillRunning)
        {
            await Task.Delay(asyncOp.PollInterval);
        }
    }
    
    if (asyncOp.Status == ODataAsyncOperationStatus.Completed)
    {
        progress?.Report("Export completed successfully");
        return asyncOp.Result!;
    }
    
    throw new ODataAsyncOperationException(
        "Export failed",
        asyncOp.MonitorUrl,
        asyncOp.ErrorMessage
    );
}
```

## Best Practices

1. **Use appropriate timeout** - Set realistic timeouts based on expected operation duration
2. **Handle both sync and async** - Server may complete immediately if data is small
3. **Implement progress reporting** - Keep users informed for long operations
4. **Clean up on cancel** - Some servers support cancellation via DELETE to monitor URL
5. **Log monitor URLs** - Helpful for debugging and manual recovery
