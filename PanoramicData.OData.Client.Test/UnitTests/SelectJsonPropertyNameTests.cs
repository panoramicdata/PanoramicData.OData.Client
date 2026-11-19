namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests that $select uses [JsonPropertyName] values rather than CLR member names.
/// </summary>
public class SelectJsonPropertyNameTests
{
	private sealed class Owner
	{
		[JsonPropertyName("full_name")]
		public string FullName { get; set; } = string.Empty;
	}

	private sealed class Widget
	{
		[JsonPropertyName("ID")]
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("owner_ref")]
		public Owner? Owner { get; set; }

		public Owner? Parent { get; set; }
	}

	private static ODataQueryBuilder<Widget> CreateBuilder()
		=> new("Widgets", NullLogger.Instance);

	/// <summary>
	/// Tests that an anonymous projection selects the serialised names.
	/// </summary>
	[Fact]
	public void Select_AnonymousType_UsesJsonPropertyNameWhenPresent()
		=> CreateBuilder().Select(w => new { w.Id, w.Name }).BuildUrl().Should().Be("Widgets?$select=ID,Name");

	/// <summary>
	/// Tests that a single-member selector selects the serialised name.
	/// </summary>
	[Fact]
	public void Select_SingleMember_UsesJsonPropertyName()
		=> CreateBuilder().Select(w => w.Id).BuildUrl().Should().Be("Widgets?$select=ID");

	/// <summary>
	/// Tests that a nested selector keeps the serialised name of its first segment.
	/// </summary>
	[Fact]
	public void Select_NestedMember_UsesJsonPropertyNameOfFirstSegment()
		=> CreateBuilder().Select(w => w.Owner!.FullName).BuildUrl().Should().Be("Widgets?$select=owner_ref");

	/// <summary>
	/// Tests that the nested $select of an expand uses serialised names.
	/// </summary>
	[Fact]
	public void ExpandWithSelect_UsesJsonPropertyNamesInNestedSelect()
		=> CreateBuilder()
			.ExpandWithSelect(w => w.Parent, o => new { o!.FullName })
			.BuildUrl().Should().Be("Widgets?$expand=Parent($select=full_name)");
}
