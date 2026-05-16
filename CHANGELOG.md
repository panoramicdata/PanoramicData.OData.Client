# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [vNext]

## [10.0.71] - 2026-05-16

### Fixed
- Fix PATCH body serialization: `UpdateAsync` now passes the runtime type to `JsonContent.Create`, preventing `Dictionary<string, object?>` patch bodies from being serialized as empty `{}` objects
- Fix `ODataTypeAnnotationConverter` to exclude dictionary types (`Dictionary<,>`, `IDictionary` implementors) and `typeof(object)` from `@odata.type` annotation injection, preventing corrupt PATCH bodies that caused ASP.NET OData `Delta<T>` model binding to return null and produce 400 "A PATCH request body is required" responses

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






