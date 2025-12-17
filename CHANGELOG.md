# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [vNext]

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



