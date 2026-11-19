using System.Reflection;

namespace PanoramicData.OData.Client;

/// <summary>
/// Shared logic for resolving a LINQ <see cref="MemberExpression"/> chain into OData path segments.
/// Consolidates what were previously several independent, hand-rolled implementations across
/// <see cref="ODataQueryBuilder{T}"/> and <see cref="NestedExpandBuilder{T}"/>.
/// </summary>
internal static partial class MemberPathResolver
{
	/// <summary>
	/// The name the member is serialised under: its <c>[JsonPropertyName]</c> when present, else the CLR name.
	/// </summary>
	internal static string GetSerializedName(MemberInfo member)
		=> member.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()?.Name ?? member.Name;

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
			segments.Push((GetSerializedName(memberExpr.Member), memberExpr.Member));
			current = memberExpr.Expression;
		}

		return (segments, current);
	}

	/// <summary>
	/// Resolves a member access chain to a flat, slash-separated OData path (e.g. <c>BestFriend/FirstName</c>).
	/// </summary>
	internal static string GetFlatPath(MemberExpression member, bool useSerializedNames = true)
	{
		var (segments, _) = WalkChain(member);

		string Segment((string Name, MemberInfo Member) s) => useSerializedNames ? s.Name : s.Member.Name;

		// For small paths (common case), avoid string.Join allocation
		return segments.Count switch
		{
			0 => string.Empty,
			1 => Segment(segments.Pop()),
			_ => string.Join("/", segments.Select(Segment))
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
		=> GetLeafMember(expression)?.Name ?? string.Empty;

	/// <summary>
	/// As <see cref="GetLeafMemberNameOrEmpty"/>, but returns the <c>[JsonPropertyName]</c> when present.
	/// </summary>
	internal static string GetLeafSerializedNameOrEmpty(Expression expression)
		=> GetLeafMember(expression) is { } member ? GetSerializedName(member) : string.Empty;

	private static MemberInfo? GetLeafMember(Expression expression)
	{
		if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			expression = unary.Operand;
		}

		return (expression as MemberExpression)?.Member;
	}
}

/// <summary>
/// Represents a segment in an expand path with property type information.
/// </summary>
internal sealed record ExpandSegment(string Name, bool IsNavigation);
