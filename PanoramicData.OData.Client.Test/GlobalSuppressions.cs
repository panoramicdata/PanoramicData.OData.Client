// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
	"Naming",
	"CA1707:Identifiers should not contain underscores",
	Justification = "Not true for unit tests"
)]

[assembly: SuppressMessage(
	"Naming",
	"CA1712:Do not prefix enum values with type name",
	Justification = "Comes from the remote model",
	Scope = "type",
	Target = "~T:PanoramicData.OData.Client.Test.Models.Feature"
)]
[assembly: SuppressMessage(
	"Performance",
	"CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons",
	Justification = "This is the purpose of this unit test",
	Scope = "type",
	Target = "~T:PanoramicData.OData.Client.Test.UnitTests.QueryBuilderStringFunctionTests"
)]
