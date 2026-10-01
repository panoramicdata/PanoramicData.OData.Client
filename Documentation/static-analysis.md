# Static analysis triage

This repository is analysed by Codacy, which runs SonarCSharp, Lizard and PSScriptAnalyzer.
Most of what those tools report is worth acting on, and has been. This file records the
findings that are deliberately left open, so that nobody has to re-derive the reasoning the
next time the report is read.

The rule is: if a finding describes a real problem, fix it. If it describes a false positive
or a deliberate design decision, record it here rather than contorting the code to silence a
tool.

## Left open on purpose

### SonarCSharp S2360 — "Use the overloading mechanism instead of the optional parameters"

Reported on ~65 methods across the client and its builders.

Sixty-one of those optional parameters are `CancellationToken cancellationToken = default`,
which is how the .NET async convention says a cancellable API should be shaped - the framework's
own APIs are written this way, and analyzers such as CA1068 assume it. The remainder are
optional `headers`, `etag`, `pollInterval` and similar, on methods where the caller genuinely
does not always have one to supply.

Replacing them with overloads would roughly double the public surface of `ODataClient` and every
builder, and would make the API harder to use, not easier. Not acted on.

### SonarCSharp S2333 — "'partial' is gratuitous in this context"

Reported on 22 type declarations.

Every one of them is a genuine part of a type split across several files - `ODataClient` alone
spans thirteen - or, in the case of `LoggerMessages`, a type the `[LoggerMessage]` source
generator requires to be partial. Removing `partial` does not compile.

The finding appears because the analysis sees one file at a time and cannot see the other parts.
Not actionable.

### SonarCSharp S1118 — "Add a 'protected' constructor or the 'static' keyword"

### SonarCSharp S2326 — "'T' is not used in the class"

Reported on `ODataClient.LegacyHelpers.cs` and `ODataQueryBuilder.LambdaParsing.cs`.

Both are consequences of the same single-file view described above. Each file holds only static
members of a type whose other parts hold instance members, so the type can be neither static nor
constructor-less; and `ODataQueryBuilder<T>`'s type parameter is used extensively in its other
parts. Not actionable.

## Worth knowing: Lizard and raw string literals

Lizard's C# tokenizer reads the `//` in a URL as the start of a line comment even inside a raw
string literal (`"""..."""`) or an interpolated string holding a quote-bearing literal. When that
happens it swallows the string's closing delimiter and folds every following method into the one
containing it.

The effect is measurements that are wrong by an order of magnitude - a 20-line test reported at
678 lines, and a 20-line `FormatValue` reported at 409. Those were not complexity problems, they
were parse failures.

Where this bit, the code now keeps URLs out of raw strings by interpolating them from constants
(see `ODataMetadataTestBase`), and OData's string-literal quoting lives in `ODataLiteral.Quote`
rather than being spelled out inline in eight places. If a Lizard length or complexity finding
ever looks absurd, check for a `//` inside a nearby string literal before refactoring anything.
