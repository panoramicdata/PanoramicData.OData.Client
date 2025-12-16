using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PanoramicData.OData.Client.Converters;
using PanoramicData.OData.Client.Test.Models;
using System.Globalization;
using System.Text.Json;

namespace PanoramicData.OData.Client.Test.Benchmarks;

/// <summary>
/// Performance benchmarks for JSON serialization/deserialization.
/// Measures the speed of OData response parsing.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class JsonSerializationBenchmarks
{
	private readonly JsonSerializerOptions _jsonOptions;
	private readonly string _singleProductJson;
	private readonly string _multipleProductsJson;
	private readonly string _largeResultSetJson;
	private readonly string _complexPersonJson;
	private readonly Product _sampleProduct;

	/// <summary>
	/// Initializes benchmark data.
	/// </summary>
	public JsonSerializationBenchmarks()
	{
		_jsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			Converters =
			{
				new System.Text.Json.Serialization.JsonStringEnumConverter(),
				new ODataDateTimeConverter(),
				new ODataNullableDateTimeConverter()
			}
		};

		_singleProductJson = """{"ID":1,"Name":"Widget","Description":"A great widget","Price":99.99,"Rating":5,"ReleaseDate":"2024-01-15T10:30:00Z"}""";

		_multipleProductsJson = """
		{
			"@odata.context": "https://example.com/$metadata#Products",
			"@odata.count": 100,
			"value": [
				{"ID":1,"Name":"Widget 1","Price":99.99,"Rating":5},
				{"ID":2,"Name":"Widget 2","Price":149.99,"Rating":4},
				{"ID":3,"Name":"Widget 3","Price":199.99,"Rating":3},
				{"ID":4,"Name":"Widget 4","Price":249.99,"Rating":4},
				{"ID":5,"Name":"Widget 5","Price":299.99,"Rating":5}
			]
		}
		""";

		// Generate large result set
		var products = Enumerable.Range(1, 100)
			.Select(i => $@"{{""ID"":{i},""Name"":""Product {i}"",""Description"":""Description for product {i}"",""Price"":{(99.99 + i).ToString(CultureInfo.InvariantCulture)},""Rating"":{i % 5 + 1},""ReleaseDate"":""2024-01-{(i % 28 + 1):D2}T10:30:00Z""}}");
		_largeResultSetJson = $$"""
		{
			"@odata.context": "https://example.com/$metadata#Products",
			"@odata.count": 1000,
			"value": [{{string.Join(",", products)}}]
		}
		""";

		_complexPersonJson = """
		{
			"UserName": "russellwhyte",
			"FirstName": "Russell",
			"LastName": "Whyte",
			"MiddleName": null,
			"Gender": "Male",
			"Age": 35,
			"Emails": ["russell@example.com", "russell.whyte@contoso.com"],
			"AddressInfo": [
				{
					"Address": "123 Main St",
					"City": {"Name": "Seattle", "CountryRegion": "United States", "Region": "WA"}
				}
			],
			"FavoriteFeature": "Feature1",
			"Features": ["Feature1", "Feature2"],
			"Friends": [
				{"UserName": "scottketchum", "FirstName": "Scott", "LastName": "Ketchum"},
				{"UserName": "ronaldmundy", "FirstName": "Ronald", "LastName": "Mundy"}
			]
		}
		""";

		_sampleProduct = new Product
		{
			Id = 1,
			Name = "Widget",
			Description = "A great widget",
			Price = 99.99m,
			Rating = 5,
			ReleaseDate = DateTimeOffset.Parse("2024-01-15T10:30:00Z", CultureInfo.InvariantCulture)
		};
	}

	/// <summary>
	/// Benchmark for deserializing single product.
	/// </summary>
	[Benchmark(Baseline = true)]
	public Product? DeserializeSingleProduct() => JsonSerializer.Deserialize<Product>(_singleProductJson, _jsonOptions);

	/// <summary>
	/// Benchmark for deserializing OData response with 5 products.
	/// </summary>
	[Benchmark]
	public ODataResponse<Product>? DeserializeMultipleProducts()
	{
		using var doc = JsonDocument.Parse(_multipleProductsJson);
		var result = new ODataResponse<Product>();

		if (doc.RootElement.TryGetProperty("value", out var valueElement))
		{
			result.Value = JsonSerializer.Deserialize<List<Product>>(valueElement.GetRawText(), _jsonOptions) ?? [];
		}

		if (doc.RootElement.TryGetProperty("@odata.count", out var countElement))
		{
			result.Count = countElement.GetInt64();
		}

		return result;
	}

	/// <summary>
	/// Benchmark for deserializing large result set (100 products).
	/// </summary>
	[Benchmark]
	public ODataResponse<Product>? DeserializeLargeResultSet()
	{
		using var doc = JsonDocument.Parse(_largeResultSetJson);
		var result = new ODataResponse<Product>();

		if (doc.RootElement.TryGetProperty("value", out var valueElement))
		{
			result.Value = JsonSerializer.Deserialize<List<Product>>(valueElement.GetRawText(), _jsonOptions) ?? [];
		}

		if (doc.RootElement.TryGetProperty("@odata.count", out var countElement))
		{
			result.Count = countElement.GetInt64();
		}

		return result;
	}

	/// <summary>
	/// Benchmark for deserializing complex person entity.
	/// </summary>
	[Benchmark]
	public Person? DeserializeComplexPerson() => JsonSerializer.Deserialize<Person>(_complexPersonJson, _jsonOptions);

	/// <summary>
	/// Benchmark for serializing single product.
	/// </summary>
	[Benchmark]
	public string SerializeSingleProduct() => JsonSerializer.Serialize(_sampleProduct, _jsonOptions);

	/// <summary>
	/// Benchmark for parsing JSON document only.
	/// </summary>
	[Benchmark]
	public JsonDocument ParseJsonDocument()
	{
		var doc = JsonDocument.Parse(_multipleProductsJson);
		doc.Dispose();
		return doc;
	}
}
