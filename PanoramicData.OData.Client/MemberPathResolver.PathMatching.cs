namespace PanoramicData.OData.Client;

internal static partial class MemberPathResolver
{
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

	internal static void AddSelectedNames(Expression selectorBody, List<string> target)
	{
		var body = selectorBody is UnaryExpression { NodeType: ExpressionType.Convert } unary ? unary.Operand : selectorBody;

		if (body is NewExpression newExpr)
		{
			foreach (var arg in newExpr.Arguments)
			{
				var memberName = GetLeafSerializedNameOrEmpty(arg);
				if (!string.IsNullOrEmpty(memberName) && !target.Contains(memberName))
				{
					target.Add(memberName);
				}
			}
		}
		else if (body is MemberExpression member)
		{
			target.Add(GetSerializedName(member.Member));
		}
	}
}
