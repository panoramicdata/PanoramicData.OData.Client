# Contributing

Thank you for your interest in contributing to this project!

## How to Contribute

1. **Fork** the repository
2. **Create a branch** for your feature or fix (`git checkout -b feature/my-feature`)
3. **Make your changes** following the coding standards below
4. **Write or update tests** as appropriate
5. **Ensure the build passes** with zero errors, zero warnings, and zero messages
6. **Submit a Pull Request** against the `main` branch

## Coding Standards

- All public members must have XML documentation comments
- Use `System.Text.Json` — do not introduce `Newtonsoft.Json`
- Use Refit for HTTP client interfaces
- Use file-scoped namespaces
- Use the `required` keyword for DTO properties where appropriate
- Ensure `TreatWarningsAsErrors` remains enabled
- All code must compile with zero diagnostics
- Console output in the repository's PowerShell scripts goes through `Write-BuildMessage`
  (dot-source `Build/BuildOutput.ps1`) rather than `Write-Host`, so it stays capturable

## Static analysis

Codacy runs SonarCSharp, Lizard and PSScriptAnalyzer over this repository. A handful of its
findings are false positives or deliberate design decisions;
[Documentation/static-analysis.md](Documentation/static-analysis.md) records which, and why.
Read it before refactoring code to satisfy a finding - and add to it if you decide a new
finding should stand.

## Testing

- Use xUnit v3 for all tests
- Use AwesomeAssertions for fluent assertions
- Ensure all existing tests pass before submitting a PR

## License

By contributing, you agree that your contributions will be licensed under the MIT License.