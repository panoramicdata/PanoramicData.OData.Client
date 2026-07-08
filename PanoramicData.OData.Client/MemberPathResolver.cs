using System.Reflection;

namespace PanoramicData.OData.Client;

/// <summary>
/// Shared logic for resolving a LINQ <see cref="MemberExpression"/> chain into OData path segments.
/// Consolidates what were previously several independent, hand-rolled implementations across
/// <see cref="ODataQueryBuilder{T}"/> and <see cref="NestedExpandBuilder{T}"/>.
/// </summary>
internal static class MemberPathResolver
{
	/// <summary>
	/// Walks a member access chain (e.g. <c>p.BestFriend.FirstName</c>) from leaf to root,
	/// returning each segment's name and <see cref="MemberInfo"/>, plus the terminal
	/// (non-<see cref="MemberExpression"/>) root expression - typically the lambda parameter,
	/// a closure <see cref="ConstantExpression"/>, or <see langword="null"/>.
	/// </summary>
	internal static (Stack<(string Name, MemberInfo Member)> Segments, Expression? Root) WalkChain(MemberExpression member)
	{
		var segments = new Stack<(string Name, MemberInfo Member)>();
		Expression? current = member;

		while (current is MemberExpression memberExpr)
		{
			segments.Push((memberExpr.Member.Name, memberExpr.Member));
			current = memberExpr.Expression;
		}

		return (segments, current);
	}

	/// <summary>
	/// Resolves a member access chain to a flat, slash-separated OData path (e.g. <c>BestFriend/FirstName</c>).
	/// </summary>
	internal static string GetFlatPath(MemberExpression member)
	{
		var (segments, _) = WalkChain(member);

		// For small paths (common case), avoid string.Join allocation
		return segments.Count switch
		{
			0 => string.Empty,
			1 => segments.Pop().Name,
			_ => string.Join("/", segments.Select(s => s.Name))
		};
	}

	/// <summary>
	/// Resolves a member access chain to a root-to-leaf list of <see cref="ExpandSegment"/>,
	/// each flagged as navigation (entity reference/collection) or scalar. Returns
	/// <see langword="null"/> only when <paramref name="member"/>'s chain is empty, which
	/// cannot occur given a non-null <see cref="MemberExpression"/> input - preserved to
	/// mirror the original implementation's guard exactly.
	/// </summary>
	internal static List<ExpandSegment>? GetExpandSegments(MemberExpression member)
	{
		var (chain, _) = WalkChain(member);

		if (chain.Count == 0)
		{
			return null;
		}

		var segments = new List<ExpandSegment>(chain.Count);

		foreach (var (name, memberInfo) in chain)
		{
			segments.Add(memberInfo is PropertyInfo propInfo
				? new ExpandSegment(propInfo.Name, IsNavigationProperty(propInfo))
				: new ExpandSegment(name, false));
		}

		return segments;
	}

	/// <summary>
	/// Resolves the leaf (immediate) member name of a single-hop selector body (e.g. <c>p.Orders</c>),
	/// unwrapping a single <see cref="ExpressionType.Convert"/> (such as the null-forgiving operator)
	/// if present. Does NOT walk nested chains - returns just the last segment's name for a
	/// multi-hop selector (e.g. <c>p.Nav.Prop</c> resolves to <c>"Prop"</c>). Returns
	/// <see cref="string.Empty"/> if <paramref name="expression"/> isn't (optionally Convert-wrapped)
	/// a <see cref="MemberExpression"/>.
	/// </summary>
	internal static string GetLeafMemberNameOrEmpty(Expression expression)
	{
		if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			expression = unary.Operand;
		}

		return expression is MemberExpression member ? member.Member.Name : string.Empty;
	}

	/// <summary>
	/// Resolves the leaf (immediate) member name of a single-hop selector body, using a looser
	/// unwrap than <see cref="GetLeafMemberNameOrEmpty"/>: matches ANY <see cref="UnaryExpression"/>
	/// wrapping a <see cref="MemberExpression"/>, not just <see cref="ExpressionType.Convert"/>, and
	/// does not itself unwrap before the primary check (only the fallback branch inspects a
	/// Unary's <see cref="UnaryExpression.Operand"/>). Kept deliberately distinct from
	/// <see cref="GetLeafMemberNameOrEmpty"/> - the two gates coincide for every expression shape
	/// the C# compiler currently emits here, but must not be silently unified. Does NOT walk nested
	/// chains - returns just the last segment's name for a multi-hop selector.
	/// </summary>
	internal static bool TryGetLeafMemberNameLoose(Expression expression, out string name)
	{
		if (expression is MemberExpression member)
		{
			name = member.Member.Name;
			return true;
		}

		if (expression is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
		{
			name = unaryMember.Member.Name;
			return true;
		}

		name = string.Empty;
		return false;
	}

	/// <summary>
	/// Resolves the full, slash-separated OData path of a selector body, using the same loose
	/// Unary-gated unwrap as <see cref="TryGetLeafMemberNameLoose"/>. Unlike that method, this
	/// walks the full chain via <see cref="GetFlatPath"/> instead of returning just the leaf
	/// segment (e.g. <c>p.BestFriend.FirstName</c> resolves to <c>"BestFriend/FirstName"</c>).
	/// Kept deliberately distinct from <see cref="TryGetLeafMemberNameLoose"/> for the same
	/// reason that method is kept distinct from <see cref="GetLeafMemberNameOrEmpty"/> - the
	/// gates coincide today but must not be silently unified.
	/// </summary>
	internal static bool TryGetPathLoose(Expression expression, out string path)
	{
		if (expression is MemberExpression member)
		{
			path = GetFlatPath(member);
			return true;
		}

		if (expression is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
		{
			path = GetFlatPath(unaryMember);
			return true;
		}

		path = string.Empty;
		return false;
	}

	/// <summary>
	/// Well-known scalar (non-navigation) reference/value types that aren't caught by
	/// <see cref="Type.IsPrimitive"/> or <see cref="Type.IsEnum"/>.
	/// </summary>
	private static readonly HashSet<Type> _scalarTypes =
	[
		typeof(string),
		typeof(DateTime),
		typeof(DateTimeOffset),
		typeof(DateOnly),
		typeof(TimeOnly),
		typeof(TimeSpan),
		typeof(Guid),
		typeof(decimal),
		typeof(byte[])
	];

	/// <summary>
	/// Determines if a property is a navigation property (vs a scalar property).
	/// Navigation properties are entity references or collections of entities.
	/// Scalar properties are primitives, strings, dates, guids, etc.
	/// </summary>
	internal static bool IsNavigationProperty(PropertyInfo property)
	{
		var propertyType = property.PropertyType;

		// Check for nullable types - get underlying type
		var underlyingType = Nullable.GetUnderlyingType(propertyType);
		if (underlyingType is not null)
		{
			propertyType = underlyingType;
		}

		// Primitives, enums, and well-known scalar types (string, dates, guid, decimal, byte[]) are scalar
		if (propertyType.IsPrimitive || propertyType.IsEnum || _scalarTypes.Contains(propertyType))
		{
			return false;
		}

		// Collections are navigation properties (string, the one scalar IEnumerable, is already handled above)
		if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyType))
		{
			return true;
		}

		// Reference types that are classes are typically navigation properties
		// (entity references like Tenant, Role, etc.)
		return propertyType.IsClass;
	}
}

/// <summary>
/// Represents a segment in an expand path with property type information.
/// </summary>
internal sealed record ExpandSegment(string Name, bool IsNavigation);
