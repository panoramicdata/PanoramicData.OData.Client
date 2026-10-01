using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PanoramicData.OData.Client;

/// <summary>
/// Lookup tables shared by every closed construction of <see cref="ODataQueryBuilder{T}"/>.
/// </summary>
/// <remarks>
/// These live on a non-generic type on purpose. A static field declared inside
/// <c>ODataQueryBuilder&lt;T&gt;</c> gets a separate instance per closed generic type, so the
/// operator table would be rebuilt for every entity type and the reflection cache would be
/// partitioned per entity type - never sharing a hit between, say, a builder over Product and
/// a builder over Order that both format the same anonymous parameter type.
/// </remarks>
internal static class ODataExpressionLookups
{
	/// <summary>
	/// Frozen dictionary for O(1) operator lookups - initialized once, thread-safe.
	/// </summary>
	internal static readonly FrozenDictionary<ExpressionType, string> OperatorMap = new Dictionary<ExpressionType, string>
	{
		[ExpressionType.Equal] = "eq",
		[ExpressionType.NotEqual] = "ne",
		[ExpressionType.GreaterThan] = "gt",
		[ExpressionType.GreaterThanOrEqual] = "ge",
		[ExpressionType.LessThan] = "lt",
		[ExpressionType.LessThanOrEqual] = "le",
		[ExpressionType.AndAlso] = "and",
		[ExpressionType.OrElse] = "or"
	}.ToFrozenDictionary();

	/// <summary>
	/// Cache for PropertyInfo arrays by type - anonymous types used in Function() calls.
	/// Uses ConditionalWeakTable to allow garbage collection of types.
	/// </summary>
	internal static readonly ConditionalWeakTable<Type, PropertyInfo[]> PropertyCache = [];
}
