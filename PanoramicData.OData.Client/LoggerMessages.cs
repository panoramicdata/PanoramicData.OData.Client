using Microsoft.Extensions.Logging;
using System.Net;

namespace PanoramicData.OData.Client;

/// <summary>
/// High-performance log message definitions using LoggerMessage.Define pattern.
/// This avoids boxing and allocations when the log level is not enabled.
/// </summary>
internal static partial class LoggerMessages
{
	#region ODataClient - Initialization (Event IDs 1-10)

	[LoggerMessage(
		EventId = 1,
		Level = LogLevel.Debug,
		Message = "ODataClient initialized with BaseUrl: {BaseUrl}")]
	public static partial void ClientInitialized(ILogger logger, string baseUrl);

	[LoggerMessage(
		EventId = 2,
		Level = LogLevel.Debug,
		Message = "Using provided HttpClient with BaseAddress: {BaseAddress}")]
	public static partial void UsingProvidedHttpClient(ILogger logger, Uri? baseAddress);

	[LoggerMessage(
		EventId = 3,
		Level = LogLevel.Debug,
		Message = "Created new HttpClient with BaseAddress: {BaseAddress}")]
	public static partial void CreatedHttpClient(ILogger logger, Uri? baseAddress);

	#endregion

	#region ODataClient - Request/Response (Event IDs 11-30)

	[LoggerMessage(
		EventId = 11,
		Level = LogLevel.Debug,
		Message = "CreateRequest - {Method} {Url}")]
	public static partial void CreateRequest(ILogger logger, HttpMethod method, string url);

	[LoggerMessage(
		EventId = 12,
		Level = LogLevel.Debug,
		Message = "CreateRequest - Adding header: {Key}={Value}")]
	public static partial void AddingHeader(ILogger logger, string key, string value);

	[LoggerMessage(
		EventId = 13,
		Level = LogLevel.Debug,
		Message = "SendWithRetryAsync - Starting request to {Url}")]
	public static partial void StartingRequest(ILogger logger, Uri? url);

	[LoggerMessage(
		EventId = 14,
		Level = LogLevel.Debug,
		Message = "SendWithRetryAsync - Sending {Method} request to {Url} (attempt {Attempt})")]
	public static partial void SendingRequest(ILogger logger, HttpMethod method, Uri? url, int attempt);

	[LoggerMessage(
		EventId = 15,
		Level = LogLevel.Debug,
		Message = "SendWithRetryAsync - Received {StatusCode} from {Url}")]
	public static partial void ReceivedResponse(ILogger logger, HttpStatusCode statusCode, Uri? url);

	[LoggerMessage(
		EventId = 16,
		Level = LogLevel.Trace,
		Message = "{RequestDetails}")]
	public static partial void LogRequestTrace(ILogger logger, string requestDetails);

	[LoggerMessage(
		EventId = 17,
		Level = LogLevel.Trace,
		Message = "{ResponseDetails}")]
	public static partial void LogResponseTrace(ILogger logger, string responseDetails);

	[LoggerMessage(
		EventId = 18,
		Level = LogLevel.Warning,
		Message = "Request to {Url} failed with {StatusCode}, attempt {Attempt}/{MaxRetries}")]
	public static partial void RetryWarning(ILogger logger, Uri? url, HttpStatusCode statusCode, int attempt, int maxRetries);

	[LoggerMessage(
		EventId = 19,
		Level = LogLevel.Warning,
		Message = "Request to {Url} {Reason}, attempt {Attempt}/{MaxRetries}")]
	public static partial void RetryException(ILogger logger, Exception ex, Uri? url, string reason, int attempt, int maxRetries);

	[LoggerMessage(
		EventId = 20,
		Level = LogLevel.Error,
		Message = "Request to {Url} failed with status {StatusCode}: {ResponseBody}")]
	public static partial void RequestFailed(ILogger logger, string url, HttpStatusCode statusCode, string responseBody);

	[LoggerMessage(
		EventId = 21,
		Level = LogLevel.Error,
		Message = "Request to {Url} returned HTML content instead of JSON (Content-Type: {ContentType}). Response preview: {Preview}")]
	public static partial void UnexpectedHtmlResponse(ILogger logger, string url, string contentType, string preview);

	#endregion

	#region ODataClient.Query (Event IDs 31-60)

	[LoggerMessage(EventId = 31, Level = LogLevel.Debug, Message = "GetAllAsync<{EntityType}> - Initial URL: {Url}")]
	public static partial void GetAllAsyncInitial(ILogger logger, string entityType, string url);

	[LoggerMessage(EventId = 32, Level = LogLevel.Debug, Message = "GetAllAsync<{EntityType}> - Fetching page {Page}, URL: {Url}")]
	public static partial void GetAllAsyncFetchingPage(ILogger logger, string entityType, int page, string url);

	[LoggerMessage(EventId = 33, Level = LogLevel.Debug, Message = "GetAllAsync<{EntityType}> - Page {Page} returned {Count} items, total so far: {Total}")]
	public static partial void GetAllAsyncPageReturned(ILogger logger, string entityType, int page, int count, int total);

	[LoggerMessage(EventId = 34, Level = LogLevel.Debug, Message = "GetAllAsync<{EntityType}> - Complete. Total items: {Total}, Count header: {Count}")]
	public static partial void GetAllAsyncComplete(ILogger logger, string entityType, int total, long? count);

	[LoggerMessage(EventId = 35, Level = LogLevel.Debug, Message = "GetAsync<{EntityType}> - URL: {Url}")]
	public static partial void GetAsync(ILogger logger, string entityType, string url);

	[LoggerMessage(EventId = 36, Level = LogLevel.Debug, Message = "GetByKeyAsync<{EntityType}> - Key: {Key}, URL: {Url}")]
	public static partial void GetByKeyAsync(ILogger logger, string entityType, object key, string url);

	[LoggerMessage(EventId = 37, Level = LogLevel.Debug, Message = "GetByKeyWithETagAsync<{EntityType}> - Key: {Key}, URL: {Url}")]
	public static partial void GetByKeyWithETagAsync(ILogger logger, string entityType, object key, string url);

	[LoggerMessage(EventId = 38, Level = LogLevel.Debug, Message = "GetByKeyWithETagAsync<{EntityType}> - ETag: {ETag}")]
	public static partial void GetByKeyWithETagResult(ILogger logger, string entityType, string? etag);

	[LoggerMessage(EventId = 39, Level = LogLevel.Debug, Message = "CallFunctionAsync<{EntityType}, {ResultType}> - URL: {Url}")]
	public static partial void CallFunctionAsync(ILogger logger, string entityType, string resultType, string url);

	[LoggerMessage(EventId = 40, Level = LogLevel.Debug, Message = "CallFunctionAsync - Response content length: {Length}")]
	public static partial void CallFunctionAsyncResponseLength(ILogger logger, int length);

	[LoggerMessage(EventId = 41, Level = LogLevel.Debug, Message = "CallFunctionAsync - Parsing 'value' property from response")]
	public static partial void CallFunctionAsyncParsingValue(ILogger logger);

	[LoggerMessage(EventId = 42, Level = LogLevel.Debug, Message = "CallFunctionAsync - Parsing entire response as {ResultType}")]
	public static partial void CallFunctionAsyncParsingEntire(ILogger logger, string resultType);

	[LoggerMessage(EventId = 43, Level = LogLevel.Debug, Message = "CallFunctionAsync - Failed to parse as OData, trying direct deserialization")]
	public static partial void CallFunctionAsyncFallback(ILogger logger);

	[LoggerMessage(EventId = 44, Level = LogLevel.Debug, Message = "GetAsync<{EntityType}> - Response received, content length: {Length}")]
	public static partial void GetAsyncResponseReceived(ILogger logger, string entityType, int length);

	[LoggerMessage(EventId = 45, Level = LogLevel.Debug, Message = "GetAsync<{EntityType}> - Parsed {Count} items from 'value' array")]
	public static partial void GetAsyncParsedItems(ILogger logger, string entityType, int count);

	[LoggerMessage(EventId = 46, Level = LogLevel.Debug, Message = "GetAsync<{EntityType}> - @odata.count: {Count}")]
	public static partial void GetAsyncODataCount(ILogger logger, string entityType, long count);

	[LoggerMessage(EventId = 47, Level = LogLevel.Debug, Message = "GetAsync<{EntityType}> - @odata.nextLink: {NextLink}")]
	public static partial void GetAsyncNextLink(ILogger logger, string entityType, string? nextLink);

	[LoggerMessage(EventId = 48, Level = LogLevel.Debug, Message = "GetAsync<{EntityType}> - ETag: {ETag}")]
	public static partial void GetAsyncETag(ILogger logger, string entityType, string? etag);

	[LoggerMessage(EventId = 49, Level = LogLevel.Debug, Message = "GetCountAsync<{EntityType}> - URL: {Url}")]
	public static partial void GetCountAsync(ILogger logger, string entityType, string url);

	[LoggerMessage(EventId = 50, Level = LogLevel.Debug, Message = "GetCountAsync<{EntityType}> - Count: {Count}")]
	public static partial void GetCountAsyncResult(ILogger logger, string entityType, long count);

	[LoggerMessage(EventId = 51, Level = LogLevel.Debug, Message = "CallActionAsync<{ResultType}> - URL: {Url}")]
	public static partial void CallActionAsync(ILogger logger, string resultType, string url);

	[LoggerMessage(EventId = 52, Level = LogLevel.Debug, Message = "CallActionAsync - Received NoContent response")]
	public static partial void CallActionAsyncNoContent(ILogger logger);

	#endregion

	#region ODataClient.Crud (Event IDs 61-80)

	[LoggerMessage(EventId = 61, Level = LogLevel.Debug, Message = "CreateAsync<{EntityType}> - EntitySet: {EntitySet}")]
	public static partial void CreateAsync(ILogger logger, string entityType, string entitySet);

	[LoggerMessage(EventId = 62, Level = LogLevel.Debug, Message = "CreateAsync<{EntityType}> - Request body: {RequestBody}")]
	public static partial void CreateAsyncRequestBody(ILogger logger, string entityType, string requestBody);

	[LoggerMessage(EventId = 63, Level = LogLevel.Debug, Message = "UpdateAsync<{EntityType}> - URL: {Url}, ETag: {ETag}")]
	public static partial void UpdateAsync(ILogger logger, string entityType, string url, string etag);

	[LoggerMessage(EventId = 64, Level = LogLevel.Debug, Message = "UpdateAsync<{EntityType}> - Added If-Match header: {ETag}")]
	public static partial void UpdateAsyncIfMatch(ILogger logger, string entityType, string etag);

	[LoggerMessage(EventId = 65, Level = LogLevel.Debug, Message = "UpdateAsync<{EntityType}> - Received 204 No Content, fetching updated entity")]
	public static partial void UpdateAsyncNoContentRefetch(ILogger logger, string entityType);

	[LoggerMessage(EventId = 66, Level = LogLevel.Debug, Message = "ReplaceAsync<{EntityType}> - URL: {Url}, ETag: {ETag}")]
	public static partial void ReplaceAsync(ILogger logger, string entityType, string url, string etag);

	[LoggerMessage(EventId = 67, Level = LogLevel.Debug, Message = "ReplaceAsync<{EntityType}> - Added If-Match header: {ETag}")]
	public static partial void ReplaceAsyncIfMatch(ILogger logger, string entityType, string etag);

	[LoggerMessage(EventId = 68, Level = LogLevel.Debug, Message = "ReplaceAsync<{EntityType}> - Received 204 No Content, fetching replaced entity")]
	public static partial void ReplaceAsyncNoContentRefetch(ILogger logger, string entityType);

	[LoggerMessage(EventId = 69, Level = LogLevel.Debug, Message = "DeleteAsync - EntitySet: {EntitySet}, Key: {Key}, URL: {Url}, ETag: {ETag}")]
	public static partial void DeleteAsync(ILogger logger, string entitySet, object key, string url, string etag);

	[LoggerMessage(EventId = 70, Level = LogLevel.Debug, Message = "DeleteAsync - Added If-Match header: {ETag}")]
	public static partial void DeleteAsyncIfMatch(ILogger logger, string etag);

	[LoggerMessage(EventId = 71, Level = LogLevel.Debug, Message = "DeleteByUrlAsync - URL: {Url}")]
	public static partial void DeleteByUrlAsync(ILogger logger, string url);

	#endregion

	#region ODataClient.Delta (Event IDs 81-90)

	[LoggerMessage(EventId = 81, Level = LogLevel.Debug, Message = "GetDeltaAsync<{EntityType}> - DeltaLink: {DeltaLink}")]
	public static partial void GetDeltaAsync(ILogger logger, string entityType, string deltaLink);

	[LoggerMessage(EventId = 82, Level = LogLevel.Debug, Message = "GetDeltaAsync<{EntityType}> - Response received, content length: {Length}")]
	public static partial void GetDeltaAsyncResponseReceived(ILogger logger, string entityType, int length);

	[LoggerMessage(EventId = 83, Level = LogLevel.Debug, Message = "GetAllDeltaAsync<{EntityType}> - Initial DeltaLink: {DeltaLink}")]
	public static partial void GetAllDeltaAsyncInitial(ILogger logger, string entityType, string deltaLink);

	[LoggerMessage(EventId = 84, Level = LogLevel.Debug, Message = "GetAllDeltaAsync<{EntityType}> - Fetching page {Page}, URL: {Url}")]
	public static partial void GetAllDeltaAsyncFetchingPage(ILogger logger, string entityType, int page, string url);

	[LoggerMessage(EventId = 85, Level = LogLevel.Debug, Message = "GetAllDeltaAsync<{EntityType}> - Page {Page} returned {Count} items, {Deleted} deleted")]
	public static partial void GetAllDeltaAsyncPageReturned(ILogger logger, string entityType, int page, int count, int deleted);

	[LoggerMessage(EventId = 86, Level = LogLevel.Debug, Message = "GetAllDeltaAsync<{EntityType}> - Complete. Total items: {Total}, Total deleted: {Deleted}")]
	public static partial void GetAllDeltaAsyncComplete(ILogger logger, string entityType, int total, int deleted);

	[LoggerMessage(EventId = 87, Level = LogLevel.Debug, Message = "ParseDeltaResponse - Found deleted entity: {Id}, Reason: {Reason}")]
	public static partial void ParseDeltaResponseDeletedEntity(ILogger logger, string? id, string? reason);

	[LoggerMessage(EventId = 88, Level = LogLevel.Debug, Message = "ParseDeltaResponse<{EntityType}> - Parsed {Count} entities, {Deleted} deleted")]
	public static partial void ParseDeltaResponseComplete(ILogger logger, string entityType, int count, int deleted);

	#endregion

	#region ODataClient.Metadata (Event IDs 91-100)

	[LoggerMessage(EventId = 91, Level = LogLevel.Debug, Message = "GetMetadataAsync - Returning cached metadata (age: {Age})")]
	public static partial void GetMetadataAsyncCached(ILogger logger, TimeSpan age);

	[LoggerMessage(EventId = 92, Level = LogLevel.Debug, Message = "GetMetadataAsync - Fetching metadata from $metadata")]
	public static partial void GetMetadataAsyncFetching(ILogger logger);

	[LoggerMessage(EventId = 93, Level = LogLevel.Debug, Message = "GetMetadataAsync - Parsing metadata, content length: {Length}")]
	public static partial void GetMetadataAsyncParsing(ILogger logger, int length);

	[LoggerMessage(EventId = 94, Level = LogLevel.Debug, Message = "GetMetadataXmlAsync - Returning cached metadata XML (age: {Age})")]
	public static partial void GetMetadataXmlAsyncCached(ILogger logger, TimeSpan age);

	[LoggerMessage(EventId = 95, Level = LogLevel.Debug, Message = "GetMetadataXmlAsync - Fetching metadata from $metadata")]
	public static partial void GetMetadataXmlAsyncFetching(ILogger logger);

	[LoggerMessage(EventId = 96, Level = LogLevel.Debug, Message = "InvalidateMetadataCache - Clearing cached metadata")]
	public static partial void InvalidateMetadataCache(ILogger logger);

	#endregion

	#region ODataClient.ServiceDocument (Event IDs 101-110)

	[LoggerMessage(EventId = 101, Level = LogLevel.Debug, Message = "GetServiceDocumentAsync - Fetching service document")]
	public static partial void GetServiceDocumentAsyncFetching(ILogger logger);

	[LoggerMessage(EventId = 102, Level = LogLevel.Debug, Message = "GetServiceDocumentAsync - Parsing service document, content length: {Length}")]
	public static partial void GetServiceDocumentAsyncParsing(ILogger logger, int length);

	[LoggerMessage(EventId = 103, Level = LogLevel.Debug, Message = "ParseServiceDocument - Parsed {Count} resources")]
	public static partial void ParseServiceDocumentComplete(ILogger logger, int count);

	[LoggerMessage(EventId = 104, Level = LogLevel.Debug, Message = "ParseServiceDocument - Found resource: {Name} ({Kind})")]
	public static partial void ParseServiceDocumentResource(ILogger logger, string name, ODataServiceResourceKind kind);

	#endregion

	#region ODataClient.Singleton (Event IDs 111-120)

	[LoggerMessage(EventId = 111, Level = LogLevel.Debug, Message = "GetSingletonAsync<{EntityType}> - Singleton: {Singleton}")]
	public static partial void GetSingletonAsync(ILogger logger, string entityType, string singleton);

	[LoggerMessage(EventId = 112, Level = LogLevel.Debug, Message = "GetSingletonWithETagAsync<{EntityType}> - Singleton: {Singleton}")]
	public static partial void GetSingletonWithETagAsync(ILogger logger, string entityType, string singleton);

	[LoggerMessage(EventId = 113, Level = LogLevel.Debug, Message = "GetSingletonWithETagAsync<{EntityType}> - ETag: {ETag}")]
	public static partial void GetSingletonWithETagResult(ILogger logger, string entityType, string? etag);

	[LoggerMessage(EventId = 114, Level = LogLevel.Debug, Message = "UpdateSingletonAsync<{EntityType}> - Singleton: {Singleton}, ETag: {ETag}")]
	public static partial void UpdateSingletonAsync(ILogger logger, string entityType, string singleton, string etag);

	[LoggerMessage(EventId = 115, Level = LogLevel.Debug, Message = "UpdateSingletonAsync<{EntityType}> - Received 204 No Content, fetching updated singleton")]
	public static partial void UpdateSingletonAsyncNoContentRefetch(ILogger logger, string entityType);

	#endregion

	#region ODataClient.Stream (Event IDs 121-130)

	[LoggerMessage(EventId = 121, Level = LogLevel.Debug, Message = "GetStreamAsync - URL: {Url}")]
	public static partial void GetStreamAsync(ILogger logger, string url);

	[LoggerMessage(EventId = 122, Level = LogLevel.Debug, Message = "SetStreamAsync - URL: {Url}, ContentType: {ContentType}")]
	public static partial void SetStreamAsync(ILogger logger, string url, string contentType);

	[LoggerMessage(EventId = 123, Level = LogLevel.Debug, Message = "GetStreamPropertyAsync - URL: {Url}")]
	public static partial void GetStreamPropertyAsync(ILogger logger, string url);

	[LoggerMessage(EventId = 124, Level = LogLevel.Debug, Message = "SetStreamPropertyAsync - URL: {Url}, ContentType: {ContentType}")]
	public static partial void SetStreamPropertyAsync(ILogger logger, string url, string contentType);

	#endregion

	#region ODataClient.References (Event IDs 131-140)

	[LoggerMessage(EventId = 131, Level = LogLevel.Debug, Message = "AddReferenceAsync - URL: {Url}, Target: {Target}")]
	public static partial void AddReferenceAsync(ILogger logger, string url, string target);

	[LoggerMessage(EventId = 132, Level = LogLevel.Debug, Message = "RemoveReferenceAsync - URL: {Url}")]
	public static partial void RemoveReferenceAsync(ILogger logger, string url);

	[LoggerMessage(EventId = 133, Level = LogLevel.Debug, Message = "SetReferenceAsync - URL: {Url}, Target: {Target}")]
	public static partial void SetReferenceAsync(ILogger logger, string url, string target);

	[LoggerMessage(EventId = 134, Level = LogLevel.Debug, Message = "DeleteReferenceAsync - URL: {Url}")]
	public static partial void DeleteReferenceAsync(ILogger logger, string url);

	#endregion

	#region ODataAsyncOperation (Event IDs 141-160)

	[LoggerMessage(EventId = 141, Level = LogLevel.Debug, Message = "ODataAsyncOperation - Polling status at {Url}")]
	public static partial void AsyncOperationPolling(ILogger logger, string url);

	[LoggerMessage(EventId = 142, Level = LogLevel.Debug, Message = "ODataAsyncOperation - Poll response: {StatusCode}")]
	public static partial void AsyncOperationPollResponse(ILogger logger, HttpStatusCode statusCode);

	[LoggerMessage(EventId = 143, Level = LogLevel.Debug, Message = "ODataAsyncOperation - Updated monitor URL: {Url}")]
	public static partial void AsyncOperationUpdatedUrl(ILogger logger, Uri? url);

	[LoggerMessage(EventId = 144, Level = LogLevel.Debug, Message = "ODataAsyncOperation - Operation completed successfully")]
	public static partial void AsyncOperationCompleted(ILogger logger);

	[LoggerMessage(EventId = 145, Level = LogLevel.Warning, Message = "ODataAsyncOperation - Failed to deserialize result")]
	public static partial void AsyncOperationDeserializeFailed(ILogger logger, Exception ex);

	[LoggerMessage(EventId = 146, Level = LogLevel.Warning, Message = "ODataAsyncOperation - Operation failed: {Error}")]
	public static partial void AsyncOperationFailed(ILogger logger, string? error);

	[LoggerMessage(EventId = 147, Level = LogLevel.Debug, Message = "ODataAsyncOperation - Waiting for completion, timeout: {Timeout}")]
	public static partial void AsyncOperationWaiting(ILogger logger, string timeout);

	[LoggerMessage(EventId = 148, Level = LogLevel.Debug, Message = "ODataAsyncOperation - Attempting to cancel at {Url}")]
	public static partial void AsyncOperationCancelling(ILogger logger, string url);

	[LoggerMessage(EventId = 149, Level = LogLevel.Debug, Message = "ODataAsyncOperation - Cancellation accepted")]
	public static partial void AsyncOperationCancelled(ILogger logger);

	[LoggerMessage(EventId = 150, Level = LogLevel.Warning, Message = "ODataAsyncOperation - Cancellation not accepted: {StatusCode}")]
	public static partial void AsyncOperationCancelNotAccepted(ILogger logger, HttpStatusCode statusCode);

	#endregion

	#region ODataClient.Async (Event IDs 161-170)

	[LoggerMessage(EventId = 161, Level = LogLevel.Debug, Message = "CallActionAsyncWithPreferAsync<{ResultType}> - URL: {Url}")]
	public static partial void CallActionAsyncWithPreferAsync(ILogger logger, string resultType, string url);

	[LoggerMessage(EventId = 162, Level = LogLevel.Warning, Message = "CallActionAsyncWithPreferAsync - 202 Accepted but no Location header")]
	public static partial void CallActionAsyncNoLocationHeader(ILogger logger);

	[LoggerMessage(EventId = 163, Level = LogLevel.Debug, Message = "CallActionAsyncWithPreferAsync - Async operation started, monitor URL: {Url}")]
	public static partial void CallActionAsyncMonitorUrl(ILogger logger, string url);

	[LoggerMessage(EventId = 164, Level = LogLevel.Debug, Message = "CallActionAsyncWithPreferAsync - Completed synchronously")]
	public static partial void CallActionAsyncCompletedSync(ILogger logger);

	[LoggerMessage(EventId = 165, Level = LogLevel.Debug, Message = "ExecuteBatchAsyncWithPreferAsync - Executing batch with {Count} items")]
	public static partial void ExecuteBatchAsyncWithPreferAsync(ILogger logger, int count);

	[LoggerMessage(EventId = 166, Level = LogLevel.Debug, Message = "ExecuteBatchAsyncWithPreferAsync - Async operation started, monitor URL: {Url}")]
	public static partial void ExecuteBatchAsyncMonitorUrl(ILogger logger, string url);

	#endregion

	#region ODataClient.Batch (Event IDs 171-190)

	[LoggerMessage(EventId = 171, Level = LogLevel.Debug, Message = "CreateBatch - Creating new batch request builder")]
	public static partial void CreateBatch(ILogger logger);

	[LoggerMessage(EventId = 172, Level = LogLevel.Debug, Message = "ExecuteBatchAsync - Executing batch with {Count} items, boundary: {Boundary}")]
	public static partial void ExecuteBatchAsync(ILogger logger, int count, string boundary);

	[LoggerMessage(EventId = 173, Level = LogLevel.Debug, Message = "ExecuteBatchAsync - Response received, content length: {Length}, content-type: {ContentType}")]
	public static partial void ExecuteBatchAsyncResponse(ILogger logger, long length, string? contentType);

	[LoggerMessage(EventId = 174, Level = LogLevel.Warning, Message = "ParseBatchResponse - Could not find boundary in content-type: {ContentType}")]
	public static partial void ParseBatchResponseNoBoundary(ILogger logger, string contentType);

	[LoggerMessage(EventId = 175, Level = LogLevel.Debug, Message = "ParseBatchResponse - Parsed {Count} operation results")]
	public static partial void ParseBatchResponseComplete(ILogger logger, int count);

	[LoggerMessage(EventId = 176, Level = LogLevel.Debug, Message = "ParseOperationResponse - Failed to deserialize response body for operation {Id}")]
	public static partial void ParseOperationResponseDeserializeFailed(ILogger logger, Exception ex, string? id);

	[LoggerMessage(EventId = 177, Level = LogLevel.Warning, Message = "ParseJsonBatchResponse - Failed to parse JSON batch response")]
	public static partial void ParseJsonBatchResponseFailed(ILogger logger, Exception ex);

	[LoggerMessage(EventId = 178, Level = LogLevel.Debug, Message = "ParseJsonBatchResponse - Failed to deserialize response for operation {Id}")]
	public static partial void ParseJsonBatchResponseDeserializeFailed(ILogger logger, Exception ex, string? id);

	#endregion

	#region ODataClient.CrossJoin (Event IDs 191-200)

	[LoggerMessage(EventId = 191, Level = LogLevel.Debug, Message = "CrossJoin - Creating cross-join for entity sets: {EntitySets}")]
	public static partial void CrossJoinCreating(ILogger logger, string entitySets);

	[LoggerMessage(EventId = 192, Level = LogLevel.Debug, Message = "GetCrossJoinAsync - URL: {Url}")]
	public static partial void GetCrossJoinAsync(ILogger logger, string url);

	[LoggerMessage(EventId = 193, Level = LogLevel.Debug, Message = "GetCrossJoinAsync - Response received, content length: {Length}")]
	public static partial void GetCrossJoinAsyncResponse(ILogger logger, int length);

	[LoggerMessage(EventId = 194, Level = LogLevel.Debug, Message = "GetAllCrossJoinAsync - Initial URL: {Url}")]
	public static partial void GetAllCrossJoinAsyncInitial(ILogger logger, string url);

	[LoggerMessage(EventId = 195, Level = LogLevel.Debug, Message = "GetAllCrossJoinAsync - Fetching page {Page}, URL: {Url}")]
	public static partial void GetAllCrossJoinAsyncFetchingPage(ILogger logger, int page, string url);

	[LoggerMessage(EventId = 196, Level = LogLevel.Debug, Message = "GetAllCrossJoinAsync - Page {Page} returned {Count} rows")]
	public static partial void GetAllCrossJoinAsyncPageReturned(ILogger logger, int page, int count);

	[LoggerMessage(EventId = 197, Level = LogLevel.Debug, Message = "GetAllCrossJoinAsync - Complete. Total rows: {Total}")]
	public static partial void GetAllCrossJoinAsyncComplete(ILogger logger, int total);

	[LoggerMessage(EventId = 198, Level = LogLevel.Debug, Message = "ParseCrossJoinResponse - Parsed {Count} rows")]
	public static partial void ParseCrossJoinResponseComplete(ILogger logger, int count);

	#endregion

	#region ODataQueryBuilder (Event IDs 201-220)

	[LoggerMessage(EventId = 201, Level = LogLevel.Debug, Message = "ODataQueryBuilder<{EntityType}>.BuildUrl() - EntitySet: '{EntitySet}'")]
	public static partial void QueryBuilderBuildUrl(ILogger logger, string entityType, string entitySet);

	[LoggerMessage(EventId = 202, Level = LogLevel.Debug, Message = "ODataQueryBuilder<{EntityType}>.BuildUrl() - Final URL: {Url}")]
	public static partial void QueryBuilderFinalUrl(ILogger logger, string entityType, string url);

	[LoggerMessage(EventId = 203, Level = LogLevel.Debug, Message = "ODataQueryBuilder - DerivedType: {DerivedType}")]
	public static partial void QueryBuilderDerivedType(ILogger logger, string derivedType);

	[LoggerMessage(EventId = 204, Level = LogLevel.Debug, Message = "ODataQueryBuilder - Key: {Key}")]
	public static partial void QueryBuilderKey(ILogger logger, object key);

	[LoggerMessage(EventId = 205, Level = LogLevel.Debug, Message = "ODataQueryBuilder - Function: {Function}")]
	public static partial void QueryBuilderFunction(ILogger logger, string function);

	[LoggerMessage(EventId = 206, Level = LogLevel.Debug, Message = "ODataQueryBuilder - Filter: {Filter}")]
	public static partial void QueryBuilderFilter(ILogger logger, string filter);

	[LoggerMessage(EventId = 207, Level = LogLevel.Debug, Message = "ODataQueryBuilder - Search: {Search}")]
	public static partial void QueryBuilderSearch(ILogger logger, string search);

	[LoggerMessage(EventId = 208, Level = LogLevel.Debug, Message = "ODataQueryBuilder - Select: {Select}")]
	public static partial void QueryBuilderSelect(ILogger logger, string select);

	[LoggerMessage(EventId = 209, Level = LogLevel.Debug, Message = "ODataQueryBuilder - Expand: {Expand}")]
	public static partial void QueryBuilderExpand(ILogger logger, string expand);

	[LoggerMessage(EventId = 210, Level = LogLevel.Debug, Message = "ODataQueryBuilder - OrderBy: {OrderBy}")]
	public static partial void QueryBuilderOrderBy(ILogger logger, string orderBy);

	[LoggerMessage(EventId = 211, Level = LogLevel.Debug, Message = "ODataQueryBuilder - Skip: {Skip}")]
	public static partial void QueryBuilderSkip(ILogger logger, long skip);

	[LoggerMessage(EventId = 212, Level = LogLevel.Debug, Message = "ODataQueryBuilder - Top: {Top}")]
	public static partial void QueryBuilderTop(ILogger logger, long top);

	[LoggerMessage(EventId = 213, Level = LogLevel.Debug, Message = "ODataQueryBuilder - Count: true")]
	public static partial void QueryBuilderCount(ILogger logger);

	[LoggerMessage(EventId = 214, Level = LogLevel.Debug, Message = "ODataQueryBuilder - Apply: {Apply}")]
	public static partial void QueryBuilderApply(ILogger logger, string apply);

	[LoggerMessage(EventId = 215, Level = LogLevel.Debug, Message = "ODataQueryBuilder - Compute: {Compute}")]
	public static partial void QueryBuilderCompute(ILogger logger, string compute);

	#endregion

	#region ODataCrossJoinBuilder (Event IDs 221-230)

	[LoggerMessage(EventId = 221, Level = LogLevel.Debug, Message = "ODataCrossJoinBuilder.BuildUrl() - EntitySets: {EntitySets}")]
	public static partial void CrossJoinBuilderBuildUrl(ILogger logger, string entitySets);

	[LoggerMessage(EventId = 222, Level = LogLevel.Debug, Message = "ODataCrossJoinBuilder.BuildUrl() - Final URL: {Url}")]
	public static partial void CrossJoinBuilderFinalUrl(ILogger logger, string url);

	#endregion
}
