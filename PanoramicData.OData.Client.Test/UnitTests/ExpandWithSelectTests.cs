using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ExpandWithSelect and nested expand functionality.
/// Tests for GitHub Issue #4: Expand with nested $select instead of $expand.
/// </summary>
public class ExpandWithSelectTests : IDisposable
{
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public ExpandWithSelectTests()
	{
		_client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.example.com/",
			Logger = NullLogger.Instance
		});
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_client.Dispose();
		GC.SuppressFinalize(this);
	}

	#region Test Models

	private sealed class ReportBatchJob
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public ReportSchedule? ReportSchedule { get; set; }
		public List<ReportResult>? Results { get; set; }
	}

	private sealed class ReportSchedule
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? RemoteSystemInputPath { get; set; }
		public string? RemoteSystemOutputPath { get; set; }
		public bool SuppressWarningsInUi { get; set; }
		public Owner? Owner { get; set; }
	}

	private sealed class Owner
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	private sealed class ReportResult
	{
		public int Id { get; set; }
		public string Status { get; set; } = string.Empty;
	}

	#endregion

	#region ExpandWithSelect Tests

	/// <summary>
	/// Tests that ExpandWithSelect generates $expand=NavProp($select=Prop1,Prop2).
	/// This is the exact scenario from GitHub Issue #4.
	/// </summary>
	[Fact]
	public void ExpandWithSelect_SingleNavProperty_GeneratesCorrectSyntax()
	{
		// Arrange & Act
		var query = _client.For<ReportBatchJob>("ReportBatchJobs")
			.ExpandWithSelect(
				rbj => rbj.ReportSchedule,
				rs => new { rs.Id, rs.Name, rs.RemoteSystemInputPath, rs.RemoteSystemOutputPath, rs.SuppressWarningsInUi });

		var url = query.BuildUrl();

		// Assert - Should use $select, NOT $expand for scalar properties
		url.Should().Contain("$expand=ReportSchedule($select=Id,Name,RemoteSystemInputPath,RemoteSystemOutputPath,SuppressWarningsInUi)");
		url.Should().NotContain("$expand=ReportSchedule($expand=");
	}

	/// <summary>
	/// Tests ExpandWithSelect with a single scalar property.
	/// </summary>
	[Fact]
	public void ExpandWithSelect_SingleProperty_GeneratesCorrectSyntax()
	{
		// Arrange & Act
		var query = _client.For<ReportBatchJob>("ReportBatchJobs")
			.ExpandWithSelect(
				rbj => rbj.ReportSchedule,
				rs => rs.Name);

		var url = query.BuildUrl();

		// Assert
		url.Should().Contain("$expand=ReportSchedule($select=Name)");
	}

	/// <summary>
	/// Tests combining ExpandWithSelect with other query options.
	/// </summary>
	[Fact]
	public void ExpandWithSelect_CombinedWithOtherOptions_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var query = _client.For<ReportBatchJob>("ReportBatchJobs")
			.Filter("Id gt 100")
			.Select("Id,Name")
			.ExpandWithSelect(
				rbj => rbj.ReportSchedule,
				rs => new { rs.Id, rs.Name })
			.Top(10);

		var url = query.BuildUrl();

		// Assert
		url.Should().Contain("$filter=");
		url.Should().Contain("$select=Id,Name");
		url.Should().Contain("$expand=ReportSchedule($select=Id,Name)");
		url.Should().Contain("$top=10");
	}

	#endregion

	#region Expand with NestedExpandBuilder Tests

	/// <summary>
	/// Tests Expand with nested select using the builder pattern.
	/// </summary>
	[Fact]
	public void Expand_WithNestedSelect_GeneratesCorrectSyntax()
	{
		// Arrange & Act
		var query = _client.For<ReportBatchJob>("ReportBatchJobs")
			.Expand(
				rbj => rbj.ReportSchedule,
				nested => nested.Select(rs => new { rs.Id, rs.Name }));

		var url = query.BuildUrl();

		// Assert
		url.Should().Contain("$expand=ReportSchedule($select=Id,Name)");
	}

	/// <summary>
	/// Tests Expand with both nested select and nested expand.
	/// </summary>
	[Fact]
	public void Expand_WithNestedSelectAndExpand_GeneratesCorrectSyntax()
	{
		// Arrange & Act
		var query = _client.For<ReportBatchJob>("ReportBatchJobs")
			.Expand(
				rbj => rbj.ReportSchedule,
				nested => nested
					.Select(rs => new { rs.Id, rs.Name })
					.Expand(rs => rs.Owner));

		var url = query.BuildUrl();

		// Assert
		url.Should().Contain("$expand=ReportSchedule($select=Id,Name;$expand=Owner)");
	}

	/// <summary>
	/// Tests Expand with nested filter.
	/// </summary>
	[Fact]
	public void Expand_WithNestedFilter_GeneratesCorrectSyntax()
	{
		// Arrange & Act
		var query = _client.For<ReportBatchJob>("ReportBatchJobs")
			.Expand(
				rbj => rbj.Results,
				nested => nested
					.Filter("Status eq 'Completed'")
					.Top(5));

		var url = query.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Results($filter=(Status eq 'Completed');$top=5)");
	}

	/// <summary>
	/// Tests Expand with nested orderby.
	/// </summary>
	[Fact]
	public void Expand_WithNestedOrderBy_GeneratesCorrectSyntax()
	{
		// Arrange & Act
		var query = _client.For<ReportBatchJob>("ReportBatchJobs")
			.Expand(
				rbj => rbj.Results,
				nested => nested
					.OrderBy("Id desc")
					.Skip(10)
					.Top(5));

		var url = query.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Results($orderby=Id desc;$top=5;$skip=10)");
	}

	/// <summary>
	/// Tests Expand with nested select using string fields.
	/// </summary>
	[Fact]
	public void Expand_WithNestedSelectString_GeneratesCorrectSyntax()
	{
		// Arrange & Act
		var query = _client.For<ReportBatchJob>("ReportBatchJobs")
			.Expand(
				rbj => rbj.ReportSchedule,
				nested => nested.Select("Id,Name,RemoteSystemInputPath"));

		var url = query.BuildUrl();

		// Assert
		url.Should().Contain("$expand=ReportSchedule($select=Id,Name,RemoteSystemInputPath)");
	}

	/// <summary>
	/// Tests multiple expands with different nested options.
	/// </summary>
	[Fact]
	public void MultipleExpands_WithDifferentNestedOptions_GeneratesCorrectSyntax()
	{
		// Arrange & Act
		var query = _client.For<ReportBatchJob>("ReportBatchJobs")
			.ExpandWithSelect(
				rbj => rbj.ReportSchedule,
				rs => new { rs.Id, rs.Name })
			.Expand(
				rbj => rbj.Results,
				nested => nested.Top(10));

		var url = query.BuildUrl();

		// Assert
		url.Should().Contain("$expand=ReportSchedule($select=Id,Name),Results($top=10)");
	}

	#endregion

	#region Regression Tests - Original Expand Still Works

	/// <summary>
	/// Tests that original Expand with expression still works for nested navigation properties.
	/// </summary>
	[Fact]
	public void Expand_OriginalSyntax_StillWorks()
	{
		// Arrange & Act
		var query = _client.For<ReportBatchJob>("ReportBatchJobs")
			.Expand(rbj => rbj.ReportSchedule);

		var url = query.BuildUrl();

		// Assert
		url.Should().Contain("$expand=ReportSchedule");
	}

	/// <summary>
	/// Tests that original Expand with string still works.
	/// </summary>
	[Fact]
	public void Expand_StringSyntax_StillWorks()
	{
		// Arrange & Act
		var query = _client.For<ReportBatchJob>("ReportBatchJobs")
			.Expand("ReportSchedule,Results");

		var url = query.BuildUrl();

		// Assert
		url.Should().Contain("$expand=ReportSchedule,Results");
	}

	#endregion
}
