namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient pagination handling.
/// </summary>
public class ODataClientPaginationTests : MockedODataClientTestBase
{
	/// <summary>
	/// Tests GetAllAsync follows nextLink to get all pages.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_FollowsNextLink()
	{
		// Arrange
		var callCount = 0;
		SetupSendAsync()
			.ReturnsAsync(() =>
			{
				callCount++;
				return callCount switch
				{
					1 => new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"value": [{ "ID": 1, "Name": "Item1" }],
							"@odata.nextLink": "https://test.odata.org/Products?$skip=1"
						}
						""")
					},
					2 => new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"value": [{ "ID": 2, "Name": "Item2" }],
							"@odata.nextLink": "https://test.odata.org/Products?$skip=2"
						}
						""")
					},
					_ => new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"value": [{ "ID": 3, "Name": "Item3" }]
						}
						""")
					}
				};
			});

		// Act
		var query = Client.For<Product>("Products");
		var response = await Client.GetAllAsync(query, CancellationToken.None);

		// Assert
		callCount.Should().Be(3);
		response.Value.Should().HaveCount(3);
		response.Value[0].Name.Should().Be("Item1");
		response.Value[1].Name.Should().Be("Item2");
		response.Value[2].Name.Should().Be("Item3");
	}

	/// <summary>
	/// Tests GetAllAsync preserves count from first response.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_PreservesTotalCount()
	{
		// Arrange
		var callCount = 0;
		SetupSendAsync()
			.ReturnsAsync(() =>
			{
				callCount++;
				return callCount == 1
					? new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"@odata.count": 50,
							"value": [{ "ID": 1, "Name": "Item1" }],
							"@odata.nextLink": "https://test.odata.org/Products?$skip=1"
						}
						""")
					}
					: new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("""
						{
							"value": [{ "ID": 2, "Name": "Item2" }]
						}
						""")
					};
			});

		// Act
		var query = Client.For<Product>("Products").Count();
		var response = await Client.GetAllAsync(query, CancellationToken.None);

		// Assert
		response.Count.Should().Be(50);
		response.Value.Should().HaveCount(2);
	}

	/// <summary>
	/// Tests GetAllAsync handles empty result set.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_EmptyResult_ReturnsEmpty()
	{
		// Arrange
		SetupSendAsync()
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"value": []}""")
			});

		// Act
		var query = Client.For<Product>("Products");
		var response = await Client.GetAllAsync(query, CancellationToken.None);

		// Assert
		response.Value.Should().BeEmpty();
	}

	/// <summary>
	/// Tests GetAllAsync respects cancellation token.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_Cancellation_ThrowsOperationCanceled()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		var callCount = 0;

		SetupSendAsync()
			.ReturnsAsync(() =>
			{
				callCount++;
				if (callCount == 2)
				{
					cts.Cancel();
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent($$"""
					{
						"value": [{ "ID": {{callCount}}, "Name": "Item{{callCount}}" }],
						"@odata.nextLink": "https://test.odata.org/Products?$skip={{callCount}}"
					}
					""")
				};
			});

		// Act
		var query = Client.For<Product>("Products");
		var act = async () => await Client.GetAllAsync(query, cts.Token);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
