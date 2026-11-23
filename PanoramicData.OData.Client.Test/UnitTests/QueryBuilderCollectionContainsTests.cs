using System.Linq.Expressions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Collection Contains in a filter must translate to an in clause for lists, arrays and explicit Enumerable.Contains.
/// Under C# 14 an array receiver binds to MemoryExtensions.Contains over a ReadOnlySpan.
/// </summary>
public class QueryBuilderCollectionContainsTests
{
	private static readonly int[] Source = [4, 5, 6, 7];

	private static string Decoded(Expression<Func<Product, bool>> filter)
		=> Uri.UnescapeDataString(
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
				.Filter(filter)
				.BuildUrl());

	/// <summary>
	/// Filter WithListContains GeneratesInClause.
	/// </summary>
	[Fact]
	public void Filter_WithListContains_GeneratesInClause()
	{
		List<int> ids = [1, 2, 3];

		Decoded(p => ids.Contains(p.Id)).Should().Contain("ID in (1,2,3)");
	}

	/// <summary>
	/// Filter WithArrayContains GeneratesInClause.
	/// </summary>
	[Fact]
	public void Filter_WithArrayContains_GeneratesInClause()
	{
		int[] ids = [1, 2, 3];

		Decoded(p => ids.Contains(p.Id)).Should().Contain("ID in (1,2,3)");
	}

	/// <summary>
	/// Filter WithStringArrayContains GeneratesInClause.
	/// </summary>
	[Fact]
	public void Filter_WithStringArrayContains_GeneratesInClause()
	{
		string[] names = ["Widget", "Gadget"];

		Decoded(p => names.Contains(p.Name)).Should().Contain("Name in ('Widget','Gadget')");
	}

	/// <summary>
	/// Filter WithChunkedArrayContains GeneratesInClause.
	/// </summary>
	[Fact]
	public void Filter_WithChunkedArrayContains_GeneratesInClause()
	{
		var chunk = Source.Chunk(2).First();

		Decoded(p => chunk.Contains(p.Id)).Should().Contain("ID in (4,5)");
	}

	/// <summary>
	/// Filter WithExplicitEnumerableContains GeneratesInClause.
	/// </summary>
	[Fact]
	public void Filter_WithExplicitEnumerableContains_GeneratesInClause()
	{
		int[] ids = [1, 2, 3];

		Decoded(p => Enumerable.Contains(ids, p.Id)).Should().Contain("ID in (1,2,3)");
	}

	/// <summary>
	/// Filter WithEmptyArrayContains GeneratesFalse.
	/// </summary>
	[Fact]
	public void Filter_WithEmptyArrayContains_GeneratesFalse()
	{
		int[] ids = [];

		Decoded(p => ids.Contains(p.Id)).Should().Contain("false");
	}
}
