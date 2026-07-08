using System.Linq;
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
}
