namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Composite key formatting (#50) and JsonPropertyName resolution in expressions (#34).
/// </summary>
public class CompositeKeyAndJsonNameTests : MockedODataClientTestBase
{
	private sealed class Renamed
	{
		[JsonPropertyName("ID")]
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public RenamedChild? Child { get; set; }
	}

	private sealed class RenamedChild
	{
		[JsonPropertyName("child_name")]
		public string ChildName { get; set; } = string.Empty;
	}

	/// <summary>Key_TwoIntKeys_FormatsAsNameValuePairs.</summary>
	[Fact]
	public void Key_TwoIntKeys_FormatsAsNameValuePairs()
	{
		var url = Client.For("StrategyEvents").Key(new { Strategy = 123, Id = 456 }).BuildUrl();

		url.Should().Be("StrategyEvents(Strategy=123,Id=456)");
	}

	/// <summary>Key_MixedStringAndInt_QuotesOnlyTheString.</summary>
	[Fact]
	public void Key_MixedStringAndInt_QuotesOnlyTheString()
	{
		var url = Client.For("Things").Key(new { Code = "abc", Number = 7 }).BuildUrl();

		url.Should().Be("Things(Code='abc',Number=7)");
	}

	/// <summary>Key_CompositeStringWithQuote_EscapesQuote.</summary>
	[Fact]
	public void Key_CompositeStringWithQuote_EscapesQuote()
	{
		var url = Client.For("Things").Key(new { Code = "O'Brien", Number = 7 }).BuildUrl();

		url.Should().Be("Things(Code='O''Brien',Number=7)");
	}

	/// <summary>Key_Dictionary_FormatsAsNameValuePairs.</summary>
	[Fact]
	public void Key_Dictionary_FormatsAsNameValuePairs()
	{
		var key = new Dictionary<string, object?> { ["A"] = 1, ["B"] = "x" };

		var url = Client.For("Things").Key(key).BuildUrl();

		url.Should().Be("Things(A=1,B='x')");
	}

	/// <summary>Key_SingleInt_Unchanged.</summary>
	[Fact]
	public void Key_SingleInt_Unchanged()
	{
		Client.For("Things").Key(5).BuildUrl().Should().Be("Things(5)");
	}

	/// <summary>Key_SingleString_Unchanged.</summary>
	[Fact]
	public void Key_SingleString_Unchanged()
	{
		Client.For("Things").Key("O'Brien").BuildUrl().Should().Be("Things('O''Brien')");
	}

	/// <summary>Filter_RenamedProperty_UsesJsonPropertyName.</summary>
	[Fact]
	public void Filter_RenamedProperty_UsesJsonPropertyName()
	{
		var url = new ODataQueryBuilder<Renamed>("Items", NullLogger.Instance)
			.Filter(p => p.Id == 7)
			.BuildUrl();

		Uri.UnescapeDataString(url).Should().Contain("$filter=(ID eq 7)");
	}

	/// <summary>Filter_NestedRenamedMember_UsesJsonPropertyNameForEachSegment.</summary>
	[Fact]
	public void Filter_NestedRenamedMember_UsesJsonPropertyNameForEachSegment()
	{
		var url = new ODataQueryBuilder<Renamed>("Items", NullLogger.Instance)
			.Filter(p => p.Child!.ChildName == "x")
			.BuildUrl();

		Uri.UnescapeDataString(url).Should().Contain("$filter=(Child/child_name eq 'x')");
	}

	/// <summary>Filter_UnrenamedProperty_Unchanged.</summary>
	[Fact]
	public void Filter_UnrenamedProperty_Unchanged()
	{
		var url = new ODataQueryBuilder<Renamed>("Items", NullLogger.Instance)
			.Filter(p => p.Name == "x")
			.BuildUrl();

		Uri.UnescapeDataString(url).Should().Contain("$filter=(Name eq 'x')");
	}
}
