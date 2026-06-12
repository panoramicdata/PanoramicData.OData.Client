# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [10.0.87] - 2026-06-12

### Added
- Add `RetryAttemptLogLevel` option to `ODataClientOptions` (default `Debug`) controlling the level at which individual failed attempts that will be retried are logged - set to `Warning` to log every attempt prominently, or `None` to disable per-attempt logging entirely
- Log a single `Warning` when all retries are exhausted (new EventIds 22 and 23), including the transient-exception path which previously logged nothing on the final failed attempt

### Changed
- Individual failed attempts that will be retried are now logged at `Debug` by default instead of `Warning`, so transient failures that recover no longer flood consumer logs

## [10.0.86] - 2026-06-03

### Fixed
- Fix non-nullable enum properties in LINQ filter expressions emitting integer values instead of quoted OData enum member names

## [10.0.85] - 2026-06-03

### Fixed
- Render enum literals in LINQ filter expressions as quoted OData enum member names instead of underlying numeric values

## [10.0.84] - 2026-05-21

### Fixed
- Remove duplicate `Content-Transfer-Encoding: binary` header in batch requests; `HttpMessageContent` constructor already adds it, so the extra call in `ODataClient.Batch.cs` was redundant

### Tests
- Add `Batch_OperationContent_ShouldIncludeRequiredHeadersExactlyOnce` to assert batch part headers appear exactly once in the serialized multipart body
- Add `DateTimeKind.Utc` assertion to `Read_SimpleDateFormat_ParsesCorrectly` to make intent explicit and guard against future converter changes
- Add regression coverage for inline enum literals and captured enum variables in filter expressions

## [10.0.83] - 2026-05-19

### Fixed
- Parse all `<Schema>` elements in CSDL metadata documents; previously only the first schema was read, causing entity types and entity sets to be missing for services (e.g. Northwind) that split their definitions across multiple schemas

## [10.0.82] - 2026-05-19

### Fixed
- Fix `"Invalid request URI"` when a provided `HttpClient` has no `BaseAddress` set - the client now automatically sets `BaseAddress` from `options.BaseUrl` if the provided `HttpClient` does not already have one

## [10.0.81] - 2026-05-19

### Fixed
- Fix batch requests throwing `"This operation is not supported for a relative URI"` - batch operation URLs are relative by design; `HttpMessageContent` now resolves them against the client base URL before building the request line and `Host` header

## [10.0.80] - 2026-05-19

### Fixed
- Fix `GetFirstOrDefaultAsync` and `GetSingleAsync` with a key set: now deserializes the response as a single JSON object instead of expecting a `{"value":[...]}` collection wrapper, matching the actual response shape of single-entity endpoints. Also no longer appends `$top` which is rejected by some APIs (e.g. Exchange Online) on single-entity URLs

## [10.0.78] - 2026-05-19

### Added
- Add `AutoPluralization` option to `ODataClientOptions` (default `true`) - set to `false` to use type names as-is, avoiding automatic pluralization for APIs such as Exchange Online that use singular endpoint names (e.g. `Mailbox` instead of `Mailboxes`)
- Respect `[EntitySet("...")]` attribute on generated DTOs (e.g. from Microsoft.OData.Client tooling) when deriving entity set names in `For<T>()`, without requiring a reference to `Microsoft.OData.Client`

## [10.0.75] - 2026-05-19

### Added
- Add `FindEntriesAsync()` to `ODataQueryBuilder<T>` as a Simple.OData.Client-compatible alias for `GetAllAsync()`, returning `IEnumerable<T>` directly - enabling the `.NavigateTo(expr).As<T>().FindEntriesAsync()` chain without needing `.Value`

## [10.0.74] - 2026-05-19

### Added
- Add non-generic `NavigateTo(expr)` overload to `ODataQueryBuilder<T>` returning `FluentODataQueryBuilder`, `As<TResult>()` on `FluentODataQueryBuilder` for re-typing, and `FindEntriesAsync()` alias - enabling Simple.OData.Client-compatible NavigateTo/As/FindEntriesAsync chain
- Add `NavigateTo<TNav>()` method to `ODataQueryBuilder<T>` and `NavigateTo()` to `FluentODataQueryBuilder`, enabling navigation to dependent collections via EntitySet(key)/NavigationProperty URL paths

## [10.0.72] - 2026-05-18

### Fixed
- Fix IgnoreResourceNotFoundException being ignored in fluent .For(...).Key(...).GetEntryAsync() - now returns null on 404 as expected

## [10.0.71] - 2026-05-16

### Fixed
- Fix PATCH body serialization: `UpdateAsync` now passes the runtime type to `JsonContent.Create`, preventing `Dictionary<string, object?>` patch bodies from being serialized as empty `{}` objects
- Fix `ODataTypeAnnotationConverter` to exclude dictionary types (`Dictionary<,>`, `IDictionary` implementors) and `typeof(object)` from `@odata.type` annotation injection, preventing corrupt PATCH bodies that caused ASP.NET OData `Delta<T>` model binding to return null and produce 400 "A PATCH request body is required" responses

## [10.0.69] - 2026-04-11

### Added
- Add `QueryOptions(string)` method to `ODataQueryBuilder<T>` and `FluentODataQueryBuilder` for verbatim vendor-specific query parameters (e.g. `PropertySet=Minimum,AddressList`) without quoting

## [10.0.68] - 2026-05-01

### Added
- Add `GetByKeyOrDefaultAsync` method - always returns null on 404 without requiring `IgnoreResourceNotFoundException` option
- Add `IgnoreResourceNotFoundException` option to `ODataClientOptions` - returns null instead of throwing `ODataNotFoundException` on 404 responses

## [10.0.67] - 2026-04-23

### Fixed
- Fix `ODataTypeAnnotationConverter` type detection to exclude OData framework types (including `Delta<T>`), preventing ASP.NET Core OData PATCH `Delta<T>` model binding from being intercepted

## [10.0.66] - 2026-04-23

### Fixed
- Fix `ODataTypeAnnotationConverter` to identify and exclude `Delta<T>` and other OData framework types, preventing `ArgumentNullException` when a `Delta` parameter is null in PATCH operations

## [10.0.65] - 2026-04-22

### Added
- Add `ODataTypeAnnotationConverter` - automatically injects `@odata.type` into POST/PATCH bodies when serializing a derived type, enabling polymorphic `CreateAsync` calls against OData servers using Table-Per-Hierarchy (TPH) inheritance
- Add `ODataTypeAnnotationAttribute` - optional attribute to override the auto-derived type name (e.g. `TypeName = "#MyNamespace.Employee"`) or force annotation inclusion on non-polymorphic types (`AlwaysInclude = true`)

## [10.0.60] - 2026-03-29

### Fixed
- Fix DateTime formatting consistency - FormatFunctionParameterValue and FormatArrayElementValue now respect DateTimeKind
- Fix DateTime filter formatting for Unspecified kind - now formats without Z suffix to match OData Edm.DateTime type, preventing timezone conversion errors
- Fix date-only string parsing to treat as UTC instead of local time, preventing timezone conversion errors

## [10.0.46] - 2025-12-19

## [10.0.43] - 2025-12-17

### Added
- Add fluent execution methods (GetAsync, GetAllAsync, GetFirstOrDefaultAsync, GetSingleAsync, GetSingleOrDefaultAsync, GetCountAsync) directly on ODataQueryBuilder<T> for streamlined query execution
- CODE_COVERAGE.md with comprehensive test coverage plan for full code coverage

## [10.0.40-beta] - 2025-01-20

### Added
- NestedExpandBuilder for configuring nested expand options (select, expand, filter, orderby, top, skip)
- ExpandWithSelect method for expand with nested select syntax (fixes #4)
- Fluent batch API with clean method chaining (`CreateBatch().Get<T>().Create().Delete().ExecuteAsync()`)
- `Changeset(Action<ODataChangesetBuilder>)` pattern for atomic batch operations
- Index-based result access on `ODataBatchResponse` (`response[0]`, `response.GetResult<T>(0)`)
- `HasErrors` property on `ODataBatchResponse`
- `TryGetResult<T>(int index, out T? result)` for safe result access
- Nested expand expression support (`p => new { p.Parent, p.Parent!.Children }` produces `$expand=Parent($expand=Children)`)
- `Function()` and `Apply()` methods to `FluentODataQueryBuilder` for feature parity
- Changelog system with `Add-ChangelogEntry.ps1` script
- Automatic version replacement in `Publish.ps1`

### Changed
- Split multi-type files into one type per file for better maintainability
- `ODataBatchBuilder` methods now return builder for fluent chaining (breaking change from string operation IDs)
- `ODataChangesetBuilder` methods now return builder for fluent chaining
- `CreateChangeset()` replaced with `Changeset(Action<ODataChangesetBuilder>)` pattern

### Removed
- Non-fluent batch API methods that returned operation IDs

## [10.0.36-beta] - 2025-01-15

### Added
- Initial public beta release
- OData V4 query builder with LINQ expression support
- Full CRUD operations (Create, Read, Update, Delete)
- Batch request support with changesets
- Delta query support for change tracking
- Metadata parsing and caching
- Service document retrieval
- Singleton entity support
- Stream property support
- Entity reference management
- Cross-join queries
- Async long-running operation support
- Retry policies with configurable delays
- ETag-based optimistic concurrency
- Comprehensive logging via `ILogger`
- Fluent and typed query APIs






