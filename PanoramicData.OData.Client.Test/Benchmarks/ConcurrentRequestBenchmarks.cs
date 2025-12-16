using System.Diagnostics.CodeAnalysis;
using System.Net;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.Benchmarks;

/// <summary>
/// Performance benchmarks for concurrent request handling.
/// Measures the client's performance under concurrent load.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "BenchmarkDotNet manages lifecycle via GlobalSetup/GlobalCleanup")]
public class ConcurrentRequestBenchmarks
{
	private Mock<HttpMessageHandler> _mockHandler = null!;
	private HttpClient _httpClient = null!;
	private ODataClient _client = null!;
	private readonly string _responseJson;

	/// <summary>
	/// Initializes the benchmark.
	/// </summary>
	public ConcurrentRequestBenchmarks()
	{
		_responseJson = """
		{
			"@odata.context": "https://test.odata.org/$metadata#Products",
			"value": [
				{"ID":1,"Name":"Widget 1","Price":99.99,"Rating":5},
				{"ID":2,"Name":"Widget 2","Price":149.99,"Rating":4},
				{"ID":3,"Name":"Widget 3","Price":199.99,"Rating":3}
			]
		}
		""";
	}

	/// <summary>
	/// Sets up mock HTTP client for each benchmark iteration.
	/// </summary>
	[GlobalSetup]
	public void Setup()
	{
		_mockHandler = new Mock<HttpMessageHandler>();
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(_responseJson, System.Text.Encoding.UTF8, "application/json")
			});

		_httpClient = new HttpClient(_mockHandler.Object)
		{
			BaseAddress = new Uri("https://test.odata.org/")
		};

		_client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0
		});
	}

	/// <summary>
	/// Cleans up resources.
	/// </summary>
	[GlobalCleanup]
	public void Cleanup()
	{
		_client?.Dispose();
		_httpClient?.Dispose();
	}

	/// <summary>
	/// Benchmark for single sequential request.
	/// </summary>
	[Benchmark(Baseline = true)]
	public async Task<ODataResponse<Product>> SingleRequest()
	{
		var query = _client.For<Product>("Products");
		return await _client.GetAsync(query);
	}

	/// <summary>
	/// Benchmark for 10 concurrent requests.
	/// </summary>
	[Benchmark]
	public async Task<ODataResponse<Product>[]> Concurrent10Requests()
	{
		var tasks = Enumerable.Range(0, 10)
			.Select(_ =>
			{
				var query = _client.For<Product>("Products");
				return _client.GetAsync(query);
			})
			.ToArray();

		return await Task.WhenAll(tasks);
	}

	/// <summary>
	/// Benchmark for 50 concurrent requests.
	/// </summary>
	[Benchmark]
	public async Task<ODataResponse<Product>[]> Concurrent50Requests()
	{
		var tasks = Enumerable.Range(0, 50)
			.Select(_ =>
			{
				var query = _client.For<Product>("Products");
				return _client.GetAsync(query);
			})
			.ToArray();

		return await Task.WhenAll(tasks);
	}

	/// <summary>
	/// Benchmark for 100 concurrent requests.
	/// </summary>
	[Benchmark]
	public async Task<ODataResponse<Product>[]> Concurrent100Requests()
	{
		var tasks = Enumerable.Range(0, 100)
			.Select(_ =>
			{
				var query = _client.For<Product>("Products");
				return _client.GetAsync(query);
			})
			.ToArray();

		return await Task.WhenAll(tasks);
	}

	/// <summary>
	/// Benchmark for sequential requests (for comparison).
	/// </summary>
	[Benchmark]
	public async Task<List<ODataResponse<Product>>> Sequential10Requests()
	{
		var results = new List<ODataResponse<Product>>();
		for (var i = 0; i < 10; i++)
		{
			var query = _client.For<Product>("Products");
			results.Add(await _client.GetAsync(query));
		}

		return results;
	}
}
