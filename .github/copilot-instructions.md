# GitHub Copilot Instructions for PanoramicData.OData.Client

This document provides guidance for GitHub Copilot when working with this repository.

## Project Overview

PanoramicData.OData.Client is a lightweight, modern OData V4 client library for .NET 10+.

## Changelog Management

**IMPORTANT**: When making changes to the codebase, update the changelog:

1. **For every notable change**, add an entry to `CHANGELOG.md` under the `[vNext]` section
2. Use the provided script: `.\Add-ChangelogEntry.ps1 -Category <Category> -Message "<Description>"`
3. Categories follow [Keep a Changelog](https://keepachangelog.com/en/1.1.0/):
   - `Added` - New features
   - `Changed` - Changes in existing functionality
   - `Deprecated` - Soon-to-be removed features
   - `Removed` - Removed features
   - `Fixed` - Bug fixes
   - `Security` - Security vulnerability fixes

4. **What to document**:
   - New public APIs or methods
   - Breaking changes (mark clearly)
   - Bug fixes that affect behavior
   - Performance improvements
   - New configuration options
   - Removed or deprecated features

5. **What NOT to document**:
   - Internal refactoring with no API changes
   - Code style/formatting changes
   - Test-only changes
   - Documentation-only changes (unless significant)

6. **Entry format**: Use imperative mood, be concise
   - Good: "Add `TryGetResult<T>()` method to `ODataBatchResponse`"
   - Bad: "Added a new method for trying to get results"

The `[vNext]` placeholder is automatically replaced with the actual version number during publishing via `Publish.ps1`.

## Code Style Guidelines

- Follow existing code conventions in the codebase
- Use C# 14.0 features where appropriate
- All public APIs must have XML documentation comments
- Use `ConfigureAwait(false)` on all async calls
- Prefer `readonly` fields where possible
- Use collection expressions `[]` for empty collections

### IDE2003: Blank Line Required After Closing Brace

**IMPORTANT**: Always add a blank line after a closing brace `}` when followed by any code that is NOT another closing brace.

? **Wrong:**
```csharp
if (condition)
{
    DoSomething();
}
DoSomethingElse(); // IDE2003 violation - missing blank line
```

? **Correct:**
```csharp
if (condition)
{
    DoSomething();
}

DoSomethingElse(); // Blank line after }
```

? **Exception - consecutive closing braces are fine:**
```csharp
if (condition)
{
    foreach (var item in items)
    {
        Process(item);
    }
}
```

This applies after: `if`, `else`, `for`, `foreach`, `while`, `do`, `switch`, `try`, `catch`, `finally`, `using`, `lock`, methods, properties, classes, etc.

## Architecture

- `ODataClient` is the main entry point, split across partial classes:
  - `ODataClient.cs` - Core functionality and HTTP handling
  - `ODataClient.Query.cs` - Query operations (GET)
  - `ODataClient.Crud.cs` - CRUD operations (POST, PATCH, PUT, DELETE)
  - `ODataClient.Batch.cs` - Batch operations
  - `ODataClient.Metadata.cs` - Metadata operations
  - `ODataClient.Legacy.cs` - Legacy fluent API compatibility

- `ODataQueryBuilder<T>` - Fluent query builder with expression parsing
- `FluentODataQueryBuilder` - Non-generic fluent builder for dynamic scenarios
- `ODataBatchBuilder` - Fluent batch request builder
- `ODataChangesetBuilder` - Atomic changeset builder

## Testing

- Unit tests use mocked HTTP handlers
- Integration tests use public OData sample services
- Use `AwesomeAssertions` for test assertions
- Test classes inherit from `TestBase` for common utilities

## Performance Considerations

See [Performance Analysis Guide](Documentation/performance-analysis.md) for:
- How to run performance analysis
- Benchmark usage
- Common hotspots and optimization strategies

## Related Documentation

- [README.md](README.md) - Main documentation
- [CHANGELOG.md](CHANGELOG.md) - Version history and changes
- [Documentation/](Documentation/) - Feature-specific documentation
- [Run-Benchmarks.ps1](Run-Benchmarks.ps1) - Performance benchmarking
- [Add-ChangelogEntry.ps1](Add-ChangelogEntry.ps1) - Add changelog entries
- [Publish.ps1](Publish.ps1) - Package publishing
