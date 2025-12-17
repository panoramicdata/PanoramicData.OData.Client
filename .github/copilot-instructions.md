# GitHub Copilot Instructions for PanoramicData.OData.Client

This document provides guidance for GitHub Copilot when working with this repository.

## Project Overview

PanoramicData.OData.Client is a lightweight, modern OData V4 client library for .NET 10+.

## Code Style Guidelines

- Follow existing code conventions in the codebase
- Use C# 14.0 features where appropriate
- All public APIs must have XML documentation comments
- Use `ConfigureAwait(false)` on all async calls
- Prefer `readonly` fields where possible
- Use collection expressions `[]` for empty collections

## Architecture

- `ODataClient` is the main entry point, split across partial classes:
  - `ODataClient.cs` - Core functionality and HTTP handling
  - `ODataClient.Query.cs` - Query operations (GET)
  - `ODataClient.Crud.cs` - CRUD operations (POST, PATCH, PUT, DELETE)
  - `ODataClient.Batch.cs` - Batch operations
  - `ODataClient.Metadata.cs` - Metadata operations
  - `ODataClient.Legacy.cs` - Legacy fluent API compatibility

- `ODataQueryBuilder<T>` - Fluent query builder with expression parsing

## Testing

- Unit tests use mocked HTTP handlers
- Integration tests use public OData sample services
- Use `AwesomeAssertions` for test assertions

## Performance Considerations

See [Performance Analysis Guide](Documentation/performance-analysis.md) for:
- How to run performance analysis
- Benchmark usage
- Common hotspots and optimization strategies

## Related Documentation

- [README.md](README.md) - Main documentation
- [Documentation/](Documentation/) - Feature-specific documentation
- [Run-Benchmarks.ps1](Run-Benchmarks.ps1) - Performance benchmarking
