namespace PanoramicData.OData.Client;

/// <summary>
/// Resolution of $select and $expand member paths from lambda selectors, and assembly of
/// the nested $expand tree those paths describe.
/// </summary>
public partial class ODataQueryBuilder<T> where T : class
{
	private static List<string> GetMemberNames(Expression<Func<T, object?>> selector)
	{
		if (selector.Body is NewExpression newExpr)
		{
			var results = new List<string>();
			foreach (var arg in newExpr.Arguments)
			{
				var memberPath = GetMemberPathFromExpression(arg);
				if (!string.IsNullOrEmpty(memberPath))
				{
					var firstSegment = memberPath.Split('/')[0];
					if (!results.Contains(firstSegment))
					{
						results.Add(firstSegment);
					}
				}
			}

			return results;
		}

		if (selector.Body is MemberExpression member)
		{
			var memberPath = GetMemberPathFromExpression(member);
			var firstSegment = memberPath.Split('/')[0];
			return [firstSegment];
		}

		if (selector.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
		{
			var memberPath = GetMemberPathFromExpression(unaryMember);
			var firstSegment = memberPath.Split('/')[0];
			return [firstSegment];
		}

		return [];
	}

	private static string GetMemberPathFromExpression(Expression expression)
	{
		if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			expression = unary.Operand;
		}

		if (expression is MemberExpression member)
		{
			return GetMemberPath(member);
		}

		return string.Empty;
	}

	private static string GetMemberName(Expression<Func<T, object?>> selector) =>
		MemberPathResolver.TryGetPathLoose(selector.Body, out var name)
			? name
			: throw new ArgumentException("Invalid selector expression");

	/// <summary>
	/// Gets full member paths from an expand expression.
	/// For example, p => new { p.BestFriend, p.BestFriend!.Trips } returns ["BestFriend", "BestFriend/Trips"].
	/// </summary>
	private static List<string> GetExpandMemberPaths(Expression<Func<T, object?>> selector)
	{
		var pathInfos = GetExpandMemberPathsWithInfo(selector);
		return pathInfos.Select(p => p.Path).ToList();
	}

	/// <summary>
	/// Gets full member paths from an expand expression along with property type information.
	/// For example, p => new { p.BestFriend, p.BestFriend!.Trips } returns paths with navigation/scalar info.
	/// </summary>
	private static List<ExpandPathInfo> GetExpandMemberPathsWithInfo(Expression<Func<T, object?>> selector)
	{
		var results = new List<ExpandPathInfo>();

		if (selector.Body is NewExpression newExpr)
		{
			foreach (var arg in newExpr.Arguments)
			{
				var pathInfo = GetExpandPathInfoFromExpression(arg);
				if (pathInfo is not null && !results.Any(r => r.Path == pathInfo.Path))
				{
					results.Add(pathInfo);
				}
			}

			return results;
		}

		var singlePathInfo = GetExpandPathInfoFromExpression(selector.Body);
		if (singlePathInfo is not null)
		{
			results.Add(singlePathInfo);
		}

		return results;
	}

	/// <summary>
	/// Extracts expand path information from an expression, including property type info.
	/// </summary>
	private static ExpandPathInfo? GetExpandPathInfoFromExpression(Expression expression)
	{
		if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
		{
			expression = unary.Operand;
		}

		if (expression is not MemberExpression member)
		{
			return null;
		}

		var segments = MemberPathResolver.GetExpandSegments(member);

		return segments is null ? null : new ExpandPathInfo(segments);
	}

	/// <summary>
	/// Builds nested expand syntax from a collection of member paths.
	/// Converts paths like ["BestFriend", "BestFriend/Trips", "Friends"] 
	/// into "BestFriend($expand=Trips),Friends".
	/// Handles scalar properties with $select instead of $expand.
	/// </summary>
	private static List<string> BuildNestedExpandFields(List<string> memberPaths)
	{
		// This overload is kept for backward compatibility but uses the new implementation
		// by treating all segments as navigation properties
		var rootNodes = new Dictionary<string, ExpandNode>();

		foreach (var path in memberPaths)
		{
			var segments = path.Split('/');
			var expandSegments = segments.Select(s => new ExpandSegment(s, true)).ToList();
			AddPathToTreeWithInfo(rootNodes, expandSegments, 0);
		}

		var result = new List<string>();
		foreach (var node in rootNodes.Values)
		{
			result.Add(node.ToODataSyntax());
		}

		return result;
	}

	/// <summary>
	/// Builds nested expand syntax from expand path information that includes property type info.
	/// </summary>
	private static List<string> BuildNestedExpandFieldsWithInfo(List<ExpandPathInfo> pathInfos)
	{
		var rootNodes = new Dictionary<string, ExpandNode>();

		foreach (var pathInfo in pathInfos)
		{
			AddPathToTreeWithInfo(rootNodes, pathInfo.Segments, 0);
		}

		var result = new List<string>();
		foreach (var node in rootNodes.Values)
		{
			result.Add(node.ToODataSyntax());
		}

		return result;
	}

	private static void AddPathToTree(Dictionary<string, ExpandNode> nodes, string[] segments, int index)
	{
		if (index >= segments.Length)
		{
			return;
		}

		var segment = segments[index];

		if (!nodes.TryGetValue(segment, out var node))
		{
			node = new ExpandNode(segment, isNavigation: true);
			nodes[segment] = node;
		}

		// Continue with remaining segments as children
		AddPathToTree(node.Children, segments, index + 1);
	}

	private static void AddPathToTreeWithInfo(Dictionary<string, ExpandNode> nodes, List<ExpandSegment> segments, int index)
	{
		if (index >= segments.Count)
		{
			return;
		}

		var segment = segments[index];

		if (!nodes.TryGetValue(segment.Name, out var node))
		{
			node = new ExpandNode(segment.Name, segment.IsNavigation);
			nodes[segment.Name] = node;
		}

		// Continue with remaining segments as children
		AddPathToTreeWithInfo(node.Children, segments, index + 1);
	}

	/// <summary>
	/// Represents an expand path with its segments and property type information.
	/// </summary>
	private sealed class ExpandPathInfo
	{
		public List<ExpandSegment> Segments { get; }
		public string Path => string.Join("/", Segments.Select(s => s.Name));

		public ExpandPathInfo(List<ExpandSegment> segments)
		{
			Segments = segments;
		}
	}

	/// <summary>
	/// Represents a node in the expand tree structure.
	/// </summary>
	private sealed class ExpandNode
	{
		public string Name { get; }
		public bool IsNavigation { get; }
		public Dictionary<string, ExpandNode> Children { get; } = [];

		public ExpandNode(string name, bool isNavigation)
		{
			Name = name;
			IsNavigation = isNavigation;
		}

		public string ToODataSyntax()
		{
			if (Children.Count == 0)
			{
				return Name;
			}

			// Separate children into navigation properties (use $expand) and scalar properties (use $select)
			var navigationChildren = Children.Values.Where(c => c.IsNavigation).ToList();
			var scalarChildren = Children.Values.Where(c => !c.IsNavigation).ToList();

			var options = new List<string>();

			// Add $select for scalar children
			if (scalarChildren.Count > 0)
			{
				var selectFields = string.Join(",", scalarChildren.Select(c => c.Name));
				options.Add($"$select={selectFields}");
			}

			// Add $expand for navigation children
			if (navigationChildren.Count > 0)
			{
				var expandFields = string.Join(",", navigationChildren.Select(c => c.ToODataSyntax()));
				options.Add($"$expand={expandFields}");
			}

			if (options.Count == 0)
			{
				return Name;
			}

			return $"{Name}({string.Join(";", options)})";
		}
	}
}
