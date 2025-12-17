# Code Coverage Plan

This document outlines the comprehensive testing strategy for PanoramicData.OData.Client to achieve full code coverage.

## Coverage Goals

- **Line Coverage Target**: 90%+
- **Branch Coverage Target**: 85%+
- **Method Coverage Target**: 95%+

## Test Categories

| Category | Purpose | Location |
|----------|---------|----------|
| Unit Tests | Test individual components in isolation with mocked HTTP | `UnitTests/` |
| Integration Tests | Test against live OData sample services | `IntegrationTests/` |
| Benchmarks | Performance measurement (not coverage) | `Benchmarks/` |

---

## 1. ODataClient Core (`ODataClient.cs`)

### Current Coverage Status: Partial

### Required Tests

| Method/Area | Test File | Status | Priority |
|-------------|-----------|--------|----------|
| Constructor with options | `ODataClientConstructorTests.cs` | ? Missing | High |
| Constructor with HttpClient injection | `ODataClientConstructorTests.cs` | ? Missing | High |
| Dispose pattern (owned HttpClient) | `ODataClientDisposeTests.cs` | ? Missing | High |
| Dispose pattern (external HttpClient) | `ODataClientDisposeTests.cs` | ? Missing | High |
| Request creation with custom headers | `ODataClientCrudTests.cs` | ?? Partial | Medium |
| Retry logic (success on retry) | `ODataClientRetryTests.cs` | ? Exists | Low |
| Retry logic (exhausted retries) | `ODataClientRetryTests.cs` | ?? Partial | Medium |
| Retry on HttpRequestException | `ODataClientRetryTests.cs` | ? Missing | High |
| Retry on timeout | `ODataClientRetryTests.cs` | ? Missing | High |
| Request cloning for retry | `ODataClientRetryTests.cs` | ? Missing | Medium |
| Error handling: NotFound | `ODataClientErrorTests.cs` | ? Missing | High |
| Error handling: Unauthorized | `ODataClientErrorTests.cs` | ? Missing | High |
| Error handling: Forbidden | `ODataClientErrorTests.cs` | ? Missing | High |
| Error handling: PreconditionFailed | `ODataClientETagTests.cs` | ? Exists | Low |
| Error handling: Server error | `ODataClientErrorTests.cs` | ? Missing | Medium |
| HTML response detection | `ODataClientErrorTests.cs` | ? Missing | High |
| FormatKey for int/long/Guid/string | `ODataClientKeyFormattingTests.cs` | ? Missing | Medium |
| ConfigureRequest callback | `ODataClientConfigurationTests.cs` | ? Missing | Medium |
| Trace logging | `ODataClientLoggingTests.cs` | ? Missing | Low |

### New Test Files Required

```
PanoramicData.OData.Client.Test/
??? UnitTests/
?   ??? ODataClientConstructorTests.cs      (NEW)
?   ??? ODataClientDisposeTests.cs          (NEW)
?   ??? ODataClientErrorTests.cs            (NEW)
?   ??? ODataClientKeyFormattingTests.cs    (NEW)
?   ??? ODataClientConfigurationTests.cs    (NEW)
?   ??? ODataClientLoggingTests.cs          (NEW)
```

---

## 2. Query Operations (`ODataClient.Query.cs`)

### Current Coverage Status: Partial

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `For<T>()` auto entity set naming | `ODataClientQueryTests.cs` | ? Missing | Medium |
| `For<T>(entitySetName)` | `ODataClientQueryTests.cs` | ? Missing | Low |
| `GetAsync<T>()` single page | `ODataClientQueryTests.cs` | ? Missing | High |
| `GetAllAsync<T>()` multiple pages | `ODataClientPaginationTests.cs` | ? Exists | Low |
| `GetAllAsync<T>()` with cancellation | `ODataClientQueryTests.cs` | ? Missing | Medium |
| `GetByKeyAsync<T, TKey>()` | `ODataClientQueryTests.cs` | ? Missing | High |
| `GetByKeyWithETagAsync<T, TKey>()` | `ODataClientETagTests.cs` | ? Exists | Low |
| `GetCountAsync<T>()` no filter | `ODataClientQueryTests.cs` | ? Missing | Medium |
| `GetCountAsync<T>()` with filter | `ODataClientQueryTests.cs` | ? Missing | Medium |
| `GetFirstOrDefaultAsync<T>()` | `ODataClientQueryTests.cs` | ? Missing | High |
| `GetSingleAsync<T>()` success | `ODataClientQueryTests.cs` | ? Missing | High |
| `GetSingleAsync<T>()` no match | `ODataClientQueryTests.cs` | ? Missing | High |
| `GetSingleAsync<T>()` multiple matches | `ODataClientQueryTests.cs` | ? Missing | High |
| `GetSingleOrDefaultAsync<T>()` | `ODataClientQueryTests.cs` | ? Missing | Medium |
| `CallFunctionAsync<T, TResult>()` | `ODataClientFunctionTests.cs` | ? Missing | High |
| `CallActionAsync<TResult>()` | `ODataClientActionTests.cs` | ? Missing | High |
| `CallActionAsync<TResult>()` NoContent | `ODataClientActionTests.cs` | ? Missing | Medium |
| `GetRawAsync()` | `ODataClientQueryTests.cs` | ? Missing | Medium |
| Response with @odata.count | `ODataClientQueryTests.cs` | ? Missing | Medium |
| Response with @odata.nextLink | `ODataClientPaginationTests.cs` | ? Exists | Low |
| Response with @odata.deltaLink | `ODataClientDeltaTests.cs` | ? Exists | Low |
| Response with ETag header | `ODataClientETagTests.cs` | ? Exists | Low |
| Entity set name pluralization | `ODataClientQueryTests.cs` | ? Missing | Low |

### New Test Files Required

```
PanoramicData.OData.Client.Test/
??? UnitTests/
?   ??? ODataClientQueryTests.cs         (NEW - consolidate)
?   ??? ODataClientFunctionTests.cs      (NEW)
?   ??? ODataClientActionTests.cs        (NEW)
```

---

## 3. CRUD Operations (`ODataClient.Crud.cs`)

### Current Coverage Status: Partial (via `ODataClientCrudTests.cs`)

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `CreateAsync<T>()` success | `ODataClientCrudTests.cs` | ? Exists | Low |
| `CreateAsync<T>()` with headers | `ODataClientCrudTests.cs` | ? Missing | Medium |
| `UpdateAsync<T>()` no ETag | `ODataClientCrudTests.cs` | ? Exists | Low |
| `UpdateAsync<T>()` with ETag | `ODataClientETagTests.cs` | ? Exists | Low |
| `UpdateAsync<T>()` 204 NoContent refetch | `ODataClientCrudTests.cs` | ? Missing | High |
| `UpdateAsync<T, TKey>()` typed key | `ODataClientCrudTests.cs` | ? Missing | Medium |
| `DeleteAsync()` no ETag | `ODataClientCrudTests.cs` | ? Exists | Low |
| `DeleteAsync()` with ETag | `ODataClientETagTests.cs` | ? Missing | Medium |
| `ReplaceAsync<T>()` (PUT) | `ODataClientCrudTests.cs` | ? Missing | High |
| `ReplaceAsync<T>()` with ETag | `ODataClientCrudTests.cs` | ? Missing | Medium |
| `ReplaceAsync<T>()` 204 NoContent refetch | `ODataClientCrudTests.cs` | ? Missing | High |
| `DeleteByUrlAsync()` internal | `ODataClientCrudTests.cs` | ? Missing | Low |

---

## 4. Batch Operations (`ODataClient.Batch.cs`)

### Current Coverage Status: Partial (via `ODataClientBatchTests.cs`)

### Required Tests

| Method/Scenario | Test File | Status | Priority |
|-----------------|-----------|--------|----------|
| `CreateBatch()` | `ODataClientBatchTests.cs` | ? Exists | Low |
| Batch with GET operations | `ODataClientBatchTests.cs` | ? Exists | Low |
| Batch with POST (Create) | `ODataClientBatchTests.cs` | ? Exists | Low |
| Batch with PATCH (Update) | `ODataClientBatchTests.cs` | ? Missing | High |
| Batch with DELETE | `ODataClientBatchTests.cs` | ? Missing | High |
| Batch with changesets | `ODataClientBatchTests.cs` | ? Exists | Low |
| Changeset atomic operations | `ODataClientBatchTests.cs` | ? Missing | Medium |
| Batch response parsing (multipart) | `ODataClientBatchTests.cs` | ?? Partial | Medium |
| Batch response parsing (JSON) | `ODataClientBatchTests.cs` | ? Missing | High |
| Batch with ETag on update/delete | `ODataClientBatchTests.cs` | ? Missing | Medium |
| `ODataBatchResponse` indexer | `ODataClientBatchTests.cs` | ? Missing | Medium |
| `ODataBatchResponse.GetResult<T>()` | `ODataClientBatchTests.cs` | ? Missing | High |
| `ODataBatchResponse.TryGetResult<T>()` | `ODataClientBatchTests.cs` | ? Missing | High |
| `ODataBatchResponse.HasErrors` | `ODataClientBatchTests.cs` | ? Missing | Medium |
| Content-ID extraction | `ODataClientBatchTests.cs` | ? Missing | Low |

---

## 5. Delta Queries (`ODataClient.Delta.cs`)

### Current Coverage Status: Partial (via `ODataClientDeltaTests.cs`)

### Required Tests

| Method/Scenario | Test File | Status | Priority |
|-----------------|-----------|--------|----------|
| `GetDeltaAsync<T>()` added entities | `ODataClientDeltaTests.cs` | ? Exists | Low |
| `GetDeltaAsync<T>()` modified entities | `ODataClientDeltaTests.cs` | ? Exists | Low |
| `GetDeltaAsync<T>()` deleted entities | `ODataClientDeltaTests.cs` | ? Exists | Low |
| `GetDeltaAsync<T>()` @removed annotation | `ODataClientDeltaTests.cs` | ? Missing | High |
| `GetDeltaAsync<T>()` @odata.removed | `ODataClientDeltaTests.cs` | ? Missing | Medium |
| `GetDeltaAsync<T>()` with reason | `ODataClientDeltaTests.cs` | ? Missing | Medium |
| `GetAllDeltaAsync<T>()` multiple pages | `ODataClientDeltaTests.cs` | ? Missing | High |
| `GetAllDeltaAsync<T>()` preserves deltaLink | `ODataClientDeltaTests.cs` | ? Missing | Medium |

---

## 6. Metadata Operations (`ODataClient.Metadata.cs`)

### Current Coverage Status: Good (via `ODataClientMetadataTests.cs`)

### Required Tests

| Method/Scenario | Test File | Status | Priority |
|-----------------|-----------|--------|----------|
| `GetMetadataAsync()` no cache | `ODataClientMetadataTests.cs` | ? Exists | Low |
| `GetMetadataAsync()` cached hit | `ODataClientMetadataTests.cs` | ? Exists | Low |
| `GetMetadataAsync()` cache expired | `ODataClientMetadataTests.cs` | ? Missing | High |
| `GetMetadataAsync()` ForceRefresh | `ODataClientMetadataTests.cs` | ? Missing | High |
| `GetMetadataXmlAsync()` | `ODataClientMetadataTests.cs` | ? Exists | Low |
| `GetMetadataXmlAsync()` cached | `ODataClientMetadataTests.cs` | ? Missing | Medium |
| `InvalidateMetadataCache()` | `ODataClientMetadataTests.cs` | ? Missing | High |
| Parse entity types | `ODataClientMetadataTests.cs` | ? Exists | Low |
| Parse complex types | `ODataClientMetadataTests.cs` | ? Missing | Medium |
| Parse enum types | `ODataClientMetadataTests.cs` | ? Missing | Medium |
| Parse navigation properties | `ODataClientMetadataTests.cs` | ? Missing | Medium |
| Parse functions/actions | `ODataClientMetadataTests.cs` | ? Missing | Low |

---

## 7. Singleton Operations (`ODataClient.Singleton.cs`)

### Current Coverage Status: Partial (via `ODataClientSingletonTests.cs`)

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `GetSingletonAsync<T>()` | `ODataClientSingletonTests.cs` | ? Exists | Low |
| `GetSingletonWithETagAsync<T>()` | `ODataClientSingletonTests.cs` | ? Missing | High |
| `UpdateSingletonAsync<T>()` no ETag | `ODataClientSingletonTests.cs` | ? Missing | High |
| `UpdateSingletonAsync<T>()` with ETag | `ODataClientSingletonTests.cs` | ? Missing | High |
| `UpdateSingletonAsync<T>()` 204 refetch | `ODataClientSingletonTests.cs` | ? Missing | High |

---

## 8. Stream Operations (`ODataClient.Stream.cs`)

### Current Coverage Status: Partial (via `ODataClientStreamTests.cs`)

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `GetStreamAsync<TKey>()` | `ODataClientStreamTests.cs` | ? Exists | Low |
| `SetStreamAsync<TKey>()` | `ODataClientStreamTests.cs` | ? Exists | Low |
| `GetStreamPropertyAsync<TKey>()` | `ODataClientStreamTests.cs` | ? Missing | High |
| `SetStreamPropertyAsync<TKey>()` | `ODataClientStreamTests.cs` | ? Missing | High |
| Stream with custom content type | `ODataClientStreamTests.cs` | ? Missing | Medium |

---

## 9. Reference Operations (`ODataClient.References.cs`)

### Current Coverage Status: Partial (via `ODataClientReferenceTests.cs`)

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `AddReferenceAsync()` | `ODataClientReferenceTests.cs` | ? Exists | Low |
| `RemoveReferenceAsync()` | `ODataClientReferenceTests.cs` | ? Exists | Low |
| `SetReferenceAsync()` | `ODataClientReferenceTests.cs` | ? Missing | High |
| `DeleteReferenceAsync()` | `ODataClientReferenceTests.cs` | ? Missing | High |
| Reference with string keys | `ODataClientReferenceTests.cs` | ? Missing | Medium |
| Reference with Guid keys | `ODataClientReferenceTests.cs` | ? Missing | Medium |

---

## 10. Cross-Join Operations (`ODataClient.CrossJoin.cs`)

### Current Coverage Status: Partial (via `ODataCrossJoinTests.cs`)

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `CrossJoin(entitySets)` | `ODataCrossJoinTests.cs` | ? Exists | Low |
| `GetCrossJoinAsync()` | `ODataCrossJoinTests.cs` | ? Exists | Low |
| `GetAllCrossJoinAsync()` pagination | `ODataCrossJoinTests.cs` | ? Missing | High |
| CrossJoin with filter | `ODataCrossJoinTests.cs` | ? Missing | Medium |
| CrossJoin with select | `ODataCrossJoinTests.cs` | ? Missing | Medium |
| `ODataCrossJoinResult.GetEntity<T>()` | `ODataCrossJoinResultTests.cs` | ? Exists | Low |

---

## 11. Async Operations (`ODataClient.Async.cs`)

### Current Coverage Status: Minimal

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `CallActionAsyncWithPreferAsync<T>()` sync | `ODataClientAsyncTests.cs` | ? Missing | High |
| `CallActionAsyncWithPreferAsync<T>()` async 202 | `ODataClientAsyncTests.cs` | ? Missing | High |
| `CallActionAndWaitAsync<T>()` | `ODataClientAsyncTests.cs` | ? Missing | High |
| `ExecuteBatchAsyncWithPreferAsync()` sync | `ODataClientAsyncTests.cs` | ? Missing | High |
| `ExecuteBatchAsyncWithPreferAsync()` async | `ODataClientAsyncTests.cs` | ? Missing | High |
| `ODataAsyncOperation.GetStatusAsync()` | `ODataAsyncOperationTests.cs` | ? Missing | High |
| `ODataAsyncOperation.WaitForCompletionAsync()` | `ODataAsyncOperationTests.cs` | ? Missing | High |
| Async operation timeout | `ODataAsyncOperationTests.cs` | ? Missing | Medium |
| Async operation polling interval | `ODataAsyncOperationTests.cs` | ? Missing | Low |

### New Test Files Required

```
PanoramicData.OData.Client.Test/
??? UnitTests/
?   ??? ODataClientAsyncTests.cs        (NEW)
?   ??? ODataAsyncOperationTests.cs     (NEW)
```

---

## 12. Service Document (`ODataClient.ServiceDocument.cs`)

### Current Coverage Status: Partial (via `ODataServiceDocumentTests.cs`)

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `GetServiceDocumentAsync()` | `ODataServiceDocumentTests.cs` | ? Exists | Low |
| Parse entity sets | `ODataServiceDocumentTests.cs` | ? Exists | Low |
| Parse singletons | `ODataServiceDocumentTests.cs` | ? Missing | High |
| Parse function imports | `ODataServiceDocumentTests.cs` | ? Missing | Medium |
| Parse action imports | `ODataServiceDocumentTests.cs` | ? Missing | Medium |
| `ODataServiceResource` kinds | `ODataServiceDocumentTests.cs` | ? Missing | Medium |

---

## 13. Legacy API (`ODataClient.Legacy.cs`)

### Current Coverage Status: Partial (via `ODataClientLegacyTests.cs`)

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `For(entitySetName)` fluent | `ODataClientLegacyTests.cs` | ? Exists | Low |
| `FindEntriesAsync()` deprecated | `ODataClientLegacyTests.cs` | ? Missing | Low |
| `FindEntryAsync()` deprecated | `ODataClientLegacyTests.cs` | ? Missing | Low |
| `ExecuteRawQueryAsync()` | `ODataClientLegacyTests.cs` | ? Missing | Low |
| `ODataRawResponse.GetEntries()` | `ODataClientLegacyTests.cs` | ? Missing | Low |
| `ODataRawResponse.GetEntry()` | `ODataClientLegacyTests.cs` | ? Missing | Low |
| `ODataRawResponse.Dispose()` | `ODataClientLegacyTests.cs` | ? Missing | Low |
| `ClearODataClientMetaDataCache()` static | `ODataClientLegacyTests.cs` | ? Missing | Low |

---

## 14. ODataQueryBuilder (`ODataQueryBuilder.cs`)

### Current Coverage Status: Good

### Required Tests

| Method/Scenario | Test File | Status | Priority |
|-----------------|-----------|--------|----------|
| `Key<TKey>()` int | `QueryBuilderQueryOptionsTests.cs` | ? Exists | Low |
| `Key<TKey>()` string (escaping) | `QueryBuilderQueryOptionsTests.cs` | ? Missing | High |
| `Key<TKey>()` Guid | `QueryBuilderQueryOptionsTests.cs` | ? Missing | Medium |
| `Filter()` expression | `QueryBuilderFilterTests.cs` | ? Exists | Low |
| `Filter()` raw string | `QueryBuilderFilterTests.cs` | ? Exists | Low |
| `Filter()` multiple (AND) | `QueryBuilderFilterTests.cs` | ? Exists | Low |
| `Search()` | `QueryBuilderQueryOptionsTests.cs` | ? Missing | Medium |
| `Select()` expression | `QueryBuilderQueryOptionsTests.cs` | ? Exists | Low |
| `Select()` string | `QueryBuilderQueryOptionsTests.cs` | ? Missing | Low |
| `Expand()` expression | `QueryBuilderQueryOptionsTests.cs` | ? Exists | Low |
| `Expand()` string | `QueryBuilderQueryOptionsTests.cs` | ? Missing | Low |
| `Expand()` nested expression | `NestedExpandExpressionTests.cs` | ? Exists | Low |
| `ExpandWithSelect()` | `ExpandWithSelectTests.cs` | ? Exists | Low |
| `Expand()` with NestedExpandBuilder | `ExpandWithSelectTests.cs` | ? Exists | Low |
| `Expand()` collection navigation | `ExpandWithSelectTests.cs` | ? Missing | High |
| `OrderBy()` expression asc | `QueryBuilderQueryOptionsTests.cs` | ? Exists | Low |
| `OrderBy()` expression desc | `QueryBuilderQueryOptionsTests.cs` | ? Missing | Medium |
| `OrderBy()` KeyValuePair collection | `QueryBuilderQueryOptionsTests.cs` | ? Missing | Medium |
| `OrderBy()` raw string | `QueryBuilderQueryOptionsTests.cs` | ? Missing | Low |
| `Skip()` | `QueryBuilderQueryOptionsTests.cs` | ? Exists | Low |
| `Top()` | `QueryBuilderQueryOptionsTests.cs` | ? Exists | Low |
| `Count()` | `QueryBuilderQueryOptionsTests.cs` | ? Exists | Low |
| `Function()` with parameters | `QueryBuilderQueryOptionsTests.cs` | ? Missing | High |
| `Apply()` aggregation | `QueryBuilderQueryOptionsTests.cs` | ? Missing | High |
| `Compute()` | `QueryBuilderComputeTests.cs` | ? Exists | Low |
| `WithHeader()` | `QueryBuilderQueryOptionsTests.cs` | ? Missing | Medium |
| `OfType<TDerived>()` | `ODataDerivedTypeTests.cs` | ? Exists | Low |
| `Cast()` | `ODataDerivedTypeTests.cs` | ? Missing | Medium |
| `BuildUrl()` complete | `QueryBuilderQueryOptionsTests.cs` | ? Exists | Low |

---

## 15. ODataQueryBuilder Expression Parsing (`ODataQueryBuilder.ExpressionParsing.cs`)

### Current Coverage Status: Good

### Required Tests (by expression type)

| Expression Type | Test File | Status | Priority |
|-----------------|-----------|--------|----------|
| Binary: eq, ne, gt, lt, ge, le | `QueryBuilderFilterTests.cs` | ? Exists | Low |
| Logical: and, or, not | `QueryBuilderFilterTests.cs` | ? Exists | Low |
| OR precedence (parentheses) | `QueryBuilderOrPrecedenceTests.cs` | ? Exists | Low |
| String: contains, startswith, endswith | `QueryBuilderStringFunctionTests.cs` | ? Exists | Low |
| String: tolower, toupper, trim | `QueryBuilderStringFunctionTests.cs` | ? Exists | Low |
| String: indexof, substring, concat | `QueryBuilderStringFunctionTests.cs` | ? Missing | Medium |
| String: length | `QueryBuilderStringFunctionTests.cs` | ? Missing | Medium |
| Date: year, month, day | `QueryBuilderDateTimeMathTests.cs` | ? Exists | Low |
| Date: hour, minute, second | `QueryBuilderDateTimeMathTests.cs` | ? Exists | Low |
| Date: date, time, fractionalseconds | `QueryBuilderDateTimeMathTests.cs` | ? Missing | Medium |
| Math: round, floor, ceiling | `QueryBuilderFilterTests.cs` | ? Missing | Low |
| Collection: any, all | `QueryBuilderAnyAllTests.cs` | ? Exists | Low |
| Collection lambda expressions | `QueryBuilderCollectionLambdaTests.cs` | ? Exists | Low |
| Null comparisons | `QueryBuilderFilterTests.cs` | ? Exists | Low |
| Enum comparisons | `QueryBuilderFilterTests.cs` | ? Missing | High |
| Guid comparisons | `QueryBuilderFilterTests.cs` | ? Missing | Medium |
| Boolean property access | `QueryBuilderFilterTests.cs` | ? Missing | Medium |
| Nested property access | `QueryBuilderFilterTests.cs` | ? Missing | Medium |

---

## 16. FluentODataQueryBuilder (`FluentODataQueryBuilder.cs`)

### Current Coverage Status: Partial

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `Key<TKey>()` | `FluentODataQueryBuilderTests.cs` | ? Missing | High |
| `Filter()` | `FluentODataQueryBuilderTests.cs` | ? Missing | High |
| `Search()` | `FluentODataQueryBuilderTests.cs` | ? Missing | Medium |
| `Select()` | `FluentODataQueryBuilderTests.cs` | ? Missing | Medium |
| `Expand()` | `FluentODataQueryBuilderTests.cs` | ? Missing | Medium |
| `OrderBy()` | `FluentODataQueryBuilderTests.cs` | ? Missing | Medium |
| `OrderByDescending()` | `FluentODataQueryBuilderTests.cs` | ? Missing | Medium |
| `Skip()` | `FluentODataQueryBuilderTests.cs` | ? Missing | Medium |
| `Top()` | `FluentODataQueryBuilderTests.cs` | ? Missing | Medium |
| `Count()` | `FluentODataQueryBuilderTests.cs` | ? Missing | Medium |
| `WithHeader()` | `FluentODataQueryBuilderTests.cs` | ? Missing | Medium |
| `Function()` | `FluentODataQueryBuilderTests.cs` | ? Missing | High |
| `Apply()` | `FluentODataQueryBuilderTests.cs` | ? Missing | High |
| `GetAsync()` | `FluentODataQueryBuilderTests.cs` | ? Missing | High |
| `GetJsonAsync()` | `FluentODataQueryBuilderTests.cs` | ? Missing | High |
| `GetAllAsync()` | `FluentODataQueryBuilderTests.cs` | ? Missing | High |
| `GetEntryAsync()` | `FluentODataQueryBuilderTests.cs` | ? Missing | High |
| `GetFirstOrDefaultAsync()` | `FluentODataQueryBuilderTests.cs` | ? Missing | High |
| `DeleteAsync()` | `FluentODataQueryBuilderTests.cs` | ? Missing | High |
| `DeleteEntryAsync()` | `FluentODataQueryBuilderTests.cs` | ? Missing | Low |
| `BuildUrl()` | `FluentODataQueryBuilderTests.cs` | ? Missing | High |
| FormatKey edge cases | `FluentODataQueryBuilderTests.cs` | ? Missing | Medium |
| FormatFunctionParameters | `FluentODataQueryBuilderTests.cs` | ? Missing | Medium |

### New Test Files Required

```
PanoramicData.OData.Client.Test/
??? UnitTests/
?   ??? FluentODataQueryBuilderTests.cs  (NEW)
```

---

## 17. NestedExpandBuilder (`NestedExpandBuilder.cs`)

### Current Coverage Status: Partial (via `ExpandWithSelectTests.cs`)

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `Select()` expression | `NestedExpandBuilderTests.cs` | ? Missing | High |
| `Select()` string | `NestedExpandBuilderTests.cs` | ? Missing | Medium |
| `Expand()` expression | `NestedExpandBuilderTests.cs` | ? Missing | High |
| `Expand()` string | `NestedExpandBuilderTests.cs` | ? Missing | Medium |
| `Filter()` | `NestedExpandBuilderTests.cs` | ? Missing | High |
| `OrderBy()` | `NestedExpandBuilderTests.cs` | ? Missing | Medium |
| `Top()` | `NestedExpandBuilderTests.cs` | ? Missing | Medium |
| `Skip()` | `NestedExpandBuilderTests.cs` | ? Missing | Medium |
| `Build()` all options combined | `NestedExpandBuilderTests.cs` | ? Missing | High |

### New Test Files Required

```
PanoramicData.OData.Client.Test/
??? UnitTests/
?   ??? NestedExpandBuilderTests.cs      (NEW)
```

---

## 18. ODataBatchBuilder (`ODataBatchBuilder.cs`)

### Current Coverage Status: Partial (via `ODataClientBatchTests.cs`)

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `Get<T>()` | `ODataBatchBuilderTests.cs` | ? Missing | High |
| `GetByKey<T, TKey>()` | `ODataBatchBuilderTests.cs` | ? Missing | High |
| `Create<T>()` | `ODataBatchBuilderTests.cs` | ? Missing | High |
| `Update<T>()` | `ODataBatchBuilderTests.cs` | ? Missing | High |
| `Update<T>()` with ETag | `ODataBatchBuilderTests.cs` | ? Missing | High |
| `Delete()` | `ODataBatchBuilderTests.cs` | ? Missing | High |
| `Delete()` with ETag | `ODataBatchBuilderTests.cs` | ? Missing | Medium |
| `Changeset()` action pattern | `ODataBatchBuilderTests.cs` | ? Missing | High |
| `ExecuteAsync()` | `ODataBatchBuilderTests.cs` | ? Missing | High |
| `GetAllOperations()` | `ODataBatchBuilderTests.cs` | ? Missing | Medium |
| Fluent chaining all methods | `ODataBatchBuilderTests.cs` | ? Missing | Medium |

### New Test Files Required

```
PanoramicData.OData.Client.Test/
??? UnitTests/
?   ??? ODataBatchBuilderTests.cs        (NEW)
```

---

## 19. ODataChangesetBuilder (`ODataChangesetBuilder.cs`)

### Current Coverage Status: Minimal

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| `Create<T>()` | `ODataChangesetBuilderTests.cs` | ? Missing | High |
| `Update<T>()` | `ODataChangesetBuilderTests.cs` | ? Missing | High |
| `Update<T>()` with ETag | `ODataChangesetBuilderTests.cs` | ? Missing | Medium |
| `Delete()` | `ODataChangesetBuilderTests.cs` | ? Missing | High |
| `Delete()` with ETag | `ODataChangesetBuilderTests.cs` | ? Missing | Medium |
| Fluent chaining | `ODataChangesetBuilderTests.cs` | ? Missing | Medium |

### New Test Files Required

```
PanoramicData.OData.Client.Test/
??? UnitTests/
?   ??? ODataChangesetBuilderTests.cs    (NEW)
```

---

## 20. ODataCrossJoinBuilder (`ODataCrossJoinBuilder.cs`)

### Current Coverage Status: Partial (via `ODataCrossJoinTests.cs`)

### Required Tests

| Method | Test File | Status | Priority |
|--------|-----------|--------|----------|
| Constructor with entity sets | `ODataCrossJoinBuilderTests.cs` | ? Missing | High |
| `Filter()` | `ODataCrossJoinBuilderTests.cs` | ? Missing | High |
| `Select()` | `ODataCrossJoinBuilderTests.cs` | ? Missing | High |
| `OrderBy()` | `ODataCrossJoinBuilderTests.cs` | ? Missing | Medium |
| `Skip()` | `ODataCrossJoinBuilderTests.cs` | ? Missing | Medium |
| `Top()` | `ODataCrossJoinBuilderTests.cs` | ? Missing | Medium |
| `Count()` | `ODataCrossJoinBuilderTests.cs` | ? Missing | Medium |
| `WithHeader()` | `ODataCrossJoinBuilderTests.cs` | ? Missing | Low |
| `BuildUrl()` | `ODataCrossJoinBuilderTests.cs` | ? Missing | High |

### New Test Files Required

```
PanoramicData.OData.Client.Test/
??? UnitTests/
?   ??? ODataCrossJoinBuilderTests.cs    (NEW)
```

---

## 21. Exceptions

### Current Coverage Status: Partial

### Required Tests

| Exception | Test File | Status | Priority |
|-----------|-----------|--------|----------|
| `ODataClientException` all constructors | `ODataExceptionTests.cs` | ? Missing | High |
| `ODataNotFoundException` | `ODataExceptionTests.cs` | ? Missing | High |
| `ODataUnauthorizedException` | `ODataExceptionTests.cs` | ? Missing | High |
| `ODataForbiddenException` | `ODataExceptionTests.cs` | ? Missing | High |
| `ODataConcurrencyException` | `ODataExceptionTests.cs` | ? Missing | High |
| `ODataAsyncOperationException` | `ODataAsyncOperationExceptionTests.cs` | ? Exists | Low |

### New Test Files Required

```
PanoramicData.OData.Client.Test/
??? UnitTests/
?   ??? ODataExceptionTests.cs           (NEW)
```

---

## 22. Converters (`ODataDateTimeConverter.cs`)

### Current Coverage Status: Good (via `ODataDateTimeConverterTests.cs`)

### Required Tests

| Scenario | Test File | Status | Priority |
|----------|-----------|--------|----------|
| Deserialize ISO 8601 DateTime | `ODataDateTimeConverterTests.cs` | ? Exists | Low |
| Deserialize OData format | `ODataDateTimeConverterTests.cs` | ? Exists | Low |
| Serialize DateTime | `ODataDateTimeConverterTests.cs` | ? Exists | Low |
| Nullable DateTime | `ODataDateTimeConverterTests.cs` | ? Missing | Medium |
| DateTimeOffset conversion | `ODataDateTimeConverterTests.cs` | ? Missing | Medium |

---

## 23. DTOs and Response Types

### Required Tests

| Type | Test File | Status | Priority |
|------|-----------|--------|----------|
| `ODataResponse<T>` properties | `ODataResponseTests.cs` | ? Missing | Medium |
| `ODataDeltaResponse<T>` properties | `ODataDeltaResponseTests.cs` | ? Missing | Medium |
| `ODataBatchResponse` indexer | `ODataBatchResponseTests.cs` | ? Missing | High |
| `ODataBatchResponse.GetResult<T>()` | `ODataBatchResponseTests.cs` | ? Missing | High |
| `ODataBatchResponse.TryGetResult<T>()` | `ODataBatchResponseTests.cs` | ? Missing | High |
| `ODataBatchOperationResult` | `ODataBatchResponseTests.cs` | ? Missing | Medium |
| `ODataCrossJoinResult` | `ODataCrossJoinResultTests.cs` | ? Exists | Low |
| `ODataOpenType` | `ODataOpenTypeTests.cs` | ? Exists | Low |
| `ODataClientOptions` validation | `ODataClientOptionsTests.cs` | ? Missing | Medium |
| `ODataServiceDocument` | `ODataServiceDocumentTests.cs` | ? Exists | Low |
| `ODataAsyncOperationResult<T>` | `ODataAsyncOperationResultTests.cs` | ? Missing | Medium |

### New Test Files Required

```
PanoramicData.OData.Client.Test/
??? UnitTests/
?   ??? ODataResponseTests.cs            (NEW)
?   ??? ODataDeltaResponseTests.cs       (NEW)
?   ??? ODataBatchResponseTests.cs       (NEW)
?   ??? ODataClientOptionsTests.cs       (NEW)
?   ??? ODataAsyncOperationResultTests.cs (NEW)
```

---

## 24. ODataMetadataParser (`ODataMetadataParser.cs`)

### Current Coverage Status: Partial

### Required Tests

| Parsing Scenario | Test File | Status | Priority |
|------------------|-----------|--------|----------|
| Parse EntityType | `ODataMetadataParserTests.cs` | ? Missing | High |
| Parse ComplexType | `ODataMetadataParserTests.cs` | ? Missing | High |
| Parse EnumType | `ODataMetadataParserTests.cs` | ? Missing | High |
| Parse Property types | `ODataMetadataParserTests.cs` | ? Missing | Medium |
| Parse NavigationProperty | `ODataMetadataParserTests.cs` | ? Missing | High |
| Parse EntityContainer | `ODataMetadataParserTests.cs` | ? Missing | Medium |
| Parse EntitySet | `ODataMetadataParserTests.cs` | ? Missing | Medium |
| Parse Singleton | `ODataMetadataParserTests.cs` | ? Missing | Medium |
| Parse FunctionImport | `ODataMetadataParserTests.cs` | ? Missing | Low |
| Parse ActionImport | `ODataMetadataParserTests.cs` | ? Missing | Low |
| Type parsing (Edm types) | `ODataMetadataParserTypeParsingsTests.cs` | ? Missing | Medium |

### New Test Files Required

```
PanoramicData.OData.Client.Test/
??? UnitTests/
?   ??? ODataMetadataParserTests.cs              (NEW)
?   ??? ODataMetadataParserTypeParsingsTests.cs  (NEW)
```

---

## Summary: New Test Files Required

| File | Priority | Estimated Tests |
|------|----------|-----------------|
| `ODataClientConstructorTests.cs` | High | 5 |
| `ODataClientDisposeTests.cs` | High | 4 |
| `ODataClientErrorTests.cs` | High | 8 |
| `ODataClientKeyFormattingTests.cs` | Medium | 6 |
| `ODataClientConfigurationTests.cs` | Medium | 3 |
| `ODataClientLoggingTests.cs` | Low | 4 |
| `ODataClientQueryTests.cs` | High | 15 |
| `ODataClientFunctionTests.cs` | High | 5 |
| `ODataClientActionTests.cs` | High | 4 |
| `ODataClientAsyncTests.cs` | High | 6 |
| `ODataAsyncOperationTests.cs` | High | 5 |
| `FluentODataQueryBuilderTests.cs` | High | 20 |
| `NestedExpandBuilderTests.cs` | High | 10 |
| `ODataBatchBuilderTests.cs` | High | 12 |
| `ODataChangesetBuilderTests.cs` | High | 6 |
| `ODataCrossJoinBuilderTests.cs` | High | 10 |
| `ODataExceptionTests.cs` | High | 6 |
| `ODataResponseTests.cs` | Medium | 4 |
| `ODataDeltaResponseTests.cs` | Medium | 4 |
| `ODataBatchResponseTests.cs` | High | 8 |
| `ODataClientOptionsTests.cs` | Medium | 4 |
| `ODataAsyncOperationResultTests.cs` | Medium | 3 |
| `ODataMetadataParserTests.cs` | High | 12 |
| `ODataMetadataParserTypeParsingsTests.cs` | Medium | 8 |

**Total New Tests Estimated**: ~172 additional tests

---

## Implementation Priority Order

### Phase 1: Critical Coverage Gaps (High Priority)
1. `ODataClientErrorTests.cs` - Error handling paths
2. `ODataClientConstructorTests.cs` - Core construction
3. `ODataClientDisposeTests.cs` - Resource cleanup
4. `ODataClientQueryTests.cs` - Core query operations
5. `FluentODataQueryBuilderTests.cs` - Fluent API
6. `ODataBatchResponseTests.cs` - Batch result access
7. `ODataExceptionTests.cs` - Exception types

### Phase 2: Feature Coverage (High Priority)
8. `ODataBatchBuilderTests.cs` - Batch building
9. `ODataClientAsyncTests.cs` - Async operations
10. `ODataAsyncOperationTests.cs` - Async polling
11. `NestedExpandBuilderTests.cs` - Nested expand
12. `ODataMetadataParserTests.cs` - Metadata parsing
13. `ODataClientFunctionTests.cs` - Functions
14. `ODataClientActionTests.cs` - Actions

### Phase 3: Builder Coverage (Medium Priority)
15. `ODataCrossJoinBuilderTests.cs` - Cross-join
16. `ODataChangesetBuilderTests.cs` - Changesets
17. `ODataClientKeyFormattingTests.cs` - Key formatting
18. `ODataClientConfigurationTests.cs` - Configuration

### Phase 4: Response Types (Medium Priority)
19. `ODataResponseTests.cs`
20. `ODataDeltaResponseTests.cs`
21. `ODataClientOptionsTests.cs`
22. `ODataAsyncOperationResultTests.cs`
23. `ODataMetadataParserTypeParsingsTests.cs`

### Phase 5: Low Priority
24. `ODataClientLoggingTests.cs`

---

## Running Coverage

### Using dotnet-coverage

```powershell
# Install coverage tool
dotnet tool install --global dotnet-coverage

# Run tests with coverage
dotnet coverage collect `
    --output coverage.cobertura.xml `
    --output-format cobertura `
    -- dotnet test

# Generate HTML report
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport
```

### Using Coverlet

```powershell
# Run with coverlet
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

---

## Notes

- All unit tests should use `MockHttpMessageHandler` for HTTP isolation
- Integration tests should use the test endpoints defined in `TestBase.cs`
- Follow existing test patterns (AwesomeAssertions for assertions)
- Ensure all async tests use `CancellationToken` from `TestContext.Current`
- Tests should be independent and not rely on external state
