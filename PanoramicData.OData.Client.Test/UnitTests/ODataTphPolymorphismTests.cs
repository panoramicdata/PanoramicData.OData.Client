#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using PanoramicData.OData.Client.Converters;
using System.Text.Json;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for Table-Per-Hierarchy (TPH) polymorphism support via @odata.type annotation.
/// Without ODataTypeAnnotationConverter, the @odata.type annotation is NOT injected
/// into the serialized payload, causing OData servers that use TPH inheritance to
/// create the wrong subtype (or fail entirely).
/// </summary>
public class ODataTphPolymorphismTests : TestBase, IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly ODataClient _client;
    private string? _capturedBody;

    public ODataTphPolymorphismTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
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

    /// <inheritdoc/>
    public void Dispose()
    {
        _client.Dispose();
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    // Domain model for TPH inheritance tests
    public class Animal
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Dog : Animal
    {
        public string Breed { get; set; } = string.Empty;
    }

    public class Cat : Animal
    {
        public bool IsIndoor { get; set; }
    }

	private void SetupCaptureAndReturn(HttpStatusCode statusCode, string responseBody) => _mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
			{
				// ReadAsStringAsync is synchronous for in-memory content - safe here
				_capturedBody = req.Content!.ReadAsStringAsync(ct).GetAwaiter().GetResult();
			})
			.ReturnsAsync(new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
			});

	// -----------------------------------------------------------------------
	// Failing test - demonstrates that WITHOUT the converter the @odata.type
	// annotation is absent from the POST body, so the OData server cannot
	// distinguish Dog from the base Animal when using TPH inheritance.
	// -----------------------------------------------------------------------

	/// <summary>
	/// Verifies that when serializing a derived type (Dog) via the OData client,
	/// the POST body contains the @odata.type annotation required by TPH servers.
	/// This test FAILS without ODataTypeAnnotationConverter registered.
	/// </summary>
	[Fact]
    public async Task CreateAsync_DerivedType_IncludesODataTypeAnnotationInRequestBody()
    {
        // Arrange
        SetupCaptureAndReturn(HttpStatusCode.Created, """{"id": 1, "name": "Rex", "breed": "Labrador"}""");

        var dog = new Dog { Name = "Rex", Breed = "Labrador" };

        // Act - CreateAsync<Animal> but passing a Dog (polymorphism - the server must know it's a Dog)
        await _client.CreateAsync<Animal>("Animals", dog, cancellationToken: CancellationToken);

        // Assert - @odata.type must be present so the server creates a Dog row, not an Animal row
        _capturedBody.Should().NotBeNull();
        _capturedBody.Should().Contain("@odata.type",
            because: "OData TPH servers require @odata.type to identify the derived type being created");
    }

    /// <summary>
    /// Verifies that when serializing a base type directly (no polymorphism),
    /// the @odata.type annotation is NOT included in the POST body.
    /// </summary>
    [Fact]
    public async Task CreateAsync_BaseType_DoesNotIncludeODataTypeAnnotationInRequestBody()
    {
        // Arrange
        SetupCaptureAndReturn(HttpStatusCode.Created, """{"id": 1, "name": "Generic"}""");

        var animal = new Animal { Name = "Generic" };

        // Act
        await _client.CreateAsync<Animal>("Animals", animal, cancellationToken: CancellationToken);

        // Assert - no annotation when there is no polymorphism
        _capturedBody.Should().NotBeNull();
        _capturedBody.Should().NotContain("@odata.type",
            because: "base type serialization should not add an unnecessary @odata.type annotation");
    }

    /// <summary>
    /// Verifies that all properties of the derived type are serialized into the POST body,
    /// not just the base type properties. Regression guard for the manual property-copy path
    /// in ODataTypeAnnotationConverter.
    /// </summary>
    [Fact]
    public async Task CreateAsync_DerivedType_SerializesDerivedPropertiesInRequestBody()
    {
        // Arrange
        SetupCaptureAndReturn(HttpStatusCode.Created, """{"id": 1, "name": "Rex", "breed": "Labrador"}""");

        var dog = new Dog { Name = "Rex", Breed = "Labrador" };

        // Act
        await _client.CreateAsync<Animal>("Animals", dog, cancellationToken: CancellationToken);

        // Assert - derived-type properties must survive the converter's manual property copy
        _capturedBody.Should().Contain("\"breed\"",
            because: "derived type properties must be included in the serialized payload");
        _capturedBody.Should().Contain("\"name\"");
    }

    /// <summary>
    /// Verifies that null properties are omitted from the POST body (WhenWritingNull setting
    /// must be respected even when going through the converter's manual property copy path).
    /// </summary>
    [Fact]
    public async Task CreateAsync_NullProperty_IsOmittedFromRequestBody()
    {
        // Arrange
        SetupCaptureAndReturn(HttpStatusCode.Created, """{"id": 1}""");

        // Name is left as default empty string - but let's use a nullable variant
        var dog = new DogWithNullableName { Breed = "Poodle" };

        // Act
        await _client.CreateAsync<Animal>("Animals", dog, cancellationToken: CancellationToken);

        // Assert - null Name must not appear in payload
        _capturedBody.Should().NotContain("\"name\":null",
            because: "DefaultIgnoreCondition.WhenWritingNull must be honoured by the converter");
    }

    /// <summary>
    /// Verifies that a response containing an @odata.type annotation is correctly deserialized
    /// and the annotation does not cause an exception or corrupt the entity.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ResponseContainsODataTypeAnnotation_DeserializesSuccessfully()
    {
        // Arrange - server echoes back @odata.type (common in real OData servers)
        SetupCaptureAndReturn(HttpStatusCode.Created,
            """{"@odata.type":"#Animals.Dog","id":5,"name":"Buddy","breed":"Beagle"}""");

        var dog = new Dog { Name = "Buddy", Breed = "Beagle" };

        // Act - should not throw
        var created = await _client.CreateAsync<Dog>("Animals", dog, cancellationToken: CancellationToken);

        // Assert - entity data must survive deserialization with the annotation present
        created.Id.Should().Be(5);
        created.Name.Should().Be("Buddy");
        created.Breed.Should().Be("Beagle");
    }

    /// <summary>
    /// Verifies that an anonymous object used for a PATCH does NOT get @odata.type injected,
    /// as anonymous types are not part of an inheritance hierarchy.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_AnonymousPatchObject_DoesNotIncludeODataTypeAnnotation()
    {
        // Arrange
        SetupCaptureAndReturn(HttpStatusCode.OK, """{"id": 1, "name": "Rex", "breed": "Labrador"}""");

        // Act - anonymous object used for partial update (common pattern)
        await _client.UpdateAsync<Dog>("Animals", 1, new { Breed = "Golden Retriever" },
            cancellationToken: CancellationToken);

        // Assert - anonymous patch must not inject @odata.type
        _capturedBody.Should().NotContain("@odata.type",
            because: "anonymous patch objects are not polymorphic and must not include @odata.type");
        _capturedBody.Should().Contain("breed",
            because: "patch properties must still be serialized correctly");
    }

    // Extended domain model
    public class DogWithNullableName : Animal
    {
        public new string? Name { get; set; }
        public string Breed { get; set; } = string.Empty;
    }
}

/// <summary>
/// Direct unit tests for ODataTypeAnnotationConverter serialization and deserialization behaviour,
/// guarding against regressions in the converter itself independently of the HTTP client.
/// </summary>
public class ODataTypeAnnotationConverterTests
{
    private readonly JsonSerializerOptions _options;

    public ODataTypeAnnotationConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new ODataTypeAnnotationConverter() }
        };
    }

    // Domain model
    public class Vehicle { public int Id { get; set; } public string Make { get; set; } = string.Empty; }
    public class Car : Vehicle { public int Doors { get; set; } }
    public class Truck : Vehicle { public decimal PayloadTons { get; set; } }

    [ODataTypeAnnotation(AlwaysInclude = true)]
    public class ElectricCar : Car { public int RangeKm { get; set; } }

    [ODataTypeAnnotation(TypeName = "#Fleet.HeavyTruck")]
    public class HeavyTruck : Truck { public int Axles { get; set; } }

    /// <summary>
    /// Derived type serialized as base should include @odata.type at the start of the object.
    /// </summary>
    [Fact]
    public void Serialize_DerivedTypeAsDeclaredBase_ODataTypeAnnotationIsFirst()
    {
        var car = new Car { Id = 1, Make = "Ford", Doors = 4 };

        var json = JsonSerializer.Serialize<Vehicle>(car, _options);

        // @odata.type must appear before any data properties (OData spec requirement)
        json.Should().StartWith("{\"@odata.type\"");
    }

    /// <summary>
    /// Base type serialized as itself must NOT include @odata.type.
    /// </summary>
    [Fact]
    public void Serialize_BaseTypeAsItself_NoODataTypeAnnotation()
    {
        var vehicle = new Vehicle { Id = 1, Make = "Generic" };

        var json = JsonSerializer.Serialize<Vehicle>(vehicle, _options);

        json.Should().NotContain("@odata.type");
    }

    /// <summary>
    /// AlwaysInclude = true forces @odata.type even when the declared and runtime types match.
    /// </summary>
    [Fact]
    public void Serialize_AlwaysIncludeAttribute_IncludesAnnotationEvenForNonPolymorphicCall()
    {
        var car = new ElectricCar { Id = 1, Make = "Tesla", Doors = 4, RangeKm = 500 };

        var json = JsonSerializer.Serialize<ElectricCar>(car, _options);

        json.Should().Contain("@odata.type");
    }

    /// <summary>
    /// Custom TypeName on the attribute is used verbatim (with # prefix).
    /// </summary>
    [Fact]
    public void Serialize_CustomTypeName_UsesAttributeValue()
    {
        var truck = new HeavyTruck { Id = 1, Make = "Volvo", Axles = 6 };

        var json = JsonSerializer.Serialize<Vehicle>(truck, _options);

        json.Should().Contain("\"@odata.type\":\"#Fleet.HeavyTruck\"");
    }

    /// <summary>
    /// All properties of the derived type must appear in the output - guards the manual
    /// property-copy loop inside the converter's Write method.
    /// </summary>
    [Theory]
    [InlineData("\"id\"")]
    [InlineData("\"make\"")]
    [InlineData("\"doors\"")]
    public void Serialize_DerivedType_AllPropertiesPresent(string expectedProperty)
    {
        var car = new Car { Id = 7, Make = "BMW", Doors = 2 };

        var json = JsonSerializer.Serialize<Vehicle>(car, _options);

        json.Should().Contain(expectedProperty);
    }

    /// <summary>
    /// Deserializing JSON that includes an @odata.type annotation must not throw and must
    /// correctly populate the entity - guards the Read path of the converter.
    /// </summary>
    [Fact]
    public void Deserialize_JsonWithODataTypeAnnotation_IgnoresAnnotationWithoutError()
    {
        var json = """{"@odata.type":"#Fleet.Car","id":3,"make":"Audi","doors":4}""";

        var result = JsonSerializer.Deserialize<Car>(json, _options);

        result.Should().NotBeNull();
        result!.Id.Should().Be(3);
        result.Make.Should().Be("Audi");
        result.Doors.Should().Be(4);
    }

    /// <summary>
    /// Round-trip: serialize a derived type as base, then deserialize as the derived type.
    /// All property values must be preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_DerivedType_PreservesAllPropertyValues()
    {
        var original = new Car { Id = 42, Make = "Honda", Doors = 4 };

        var json = JsonSerializer.Serialize<Vehicle>(original, _options);
        var result = JsonSerializer.Deserialize<Car>(json, _options);

        result.Should().NotBeNull();
        result!.Id.Should().Be(original.Id);
        result.Make.Should().Be(original.Make);
        result.Doors.Should().Be(original.Doors);
    }

    /// <summary>
    /// CamelCase naming policy must be applied to properties even when going through
    /// the converter's manual serialization path (regression guard for naming policy propagation).
    /// </summary>
    [Fact]
    public void Serialize_DerivedType_UsesCamelCaseNaming()
    {
        var truck = new Truck { Id = 1, Make = "Scania", PayloadTons = 20 };

        var json = JsonSerializer.Serialize<Vehicle>(truck, _options);

        json.Should().Contain("\"payloadTons\"", because: "camelCase naming policy must be applied");
        json.Should().NotContain("\"PayloadTons\"");
    }

    /// <summary>
    /// A type decorated with [JsonConverter] must use its own converter, not ODataTypeAnnotationConverter.
    /// STJ Converters-collection precedence is higher than a type-level [JsonConverter] attribute,
    /// so CanConvert() must explicitly yield for such types to avoid silently overriding user converters.
    /// </summary>
    [Fact]
    public void Serialize_TypeWithOwnJsonConverterAttribute_UsesItsOwnConverter()
    {
        var sensor = new Sensor { Id = 99, Reading = 3.14 };

        var json = JsonSerializer.Serialize(sensor, _options);

        // SensorConverter writes a flat string "SENSOR:<Id>", not a JSON object
        json.Should().Be("\"SENSOR:99\"",
            because: "the type's own [JsonConverter] must not be overridden by ODataTypeAnnotationConverter");
    }

    /// <summary>
    /// A type decorated with [JsonConverter] must also deserialize via its own converter.
    /// </summary>
    [Fact]
    public void Deserialize_TypeWithOwnJsonConverterAttribute_UsesItsOwnConverter()
    {
        var json = "\"SENSOR:42\"";

        var result = JsonSerializer.Deserialize<Sensor>(json, _options);

        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
    }

    // A type with its own [JsonConverter] - simulates e.g. a value-object or discriminated union
    // that a user has already custom-serialized.
    [JsonConverter(typeof(SensorConverter))]
    public class Sensor
    {
        public int Id { get; set; }
        public double Reading { get; set; }
    }

    public class SensorConverter : JsonConverter<Sensor>
    {
        public override Sensor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString() ?? string.Empty;
            var id = int.Parse(value.Replace("SENSOR:", string.Empty), System.Globalization.CultureInfo.InvariantCulture);
            return new Sensor { Id = id };
        }

        public override void Write(Utf8JsonWriter writer, Sensor value, JsonSerializerOptions options) =>
            writer.WriteStringValue($"SENSOR:{value.Id}");
    }

    /// <summary>
    /// A TypeName that already starts with '#' must not get a second '#' prefix.
    /// </summary>
    [Fact]
    public void Serialize_CustomTypeNameAlreadyHasHashPrefix_DoesNotDoublePrefix()
    {
        var truck = new HeavyTruck { Id = 1, Make = "Volvo", Axles = 6 };

        var json = JsonSerializer.Serialize<Vehicle>(truck, _options);

        json.Should().Contain("\"@odata.type\":\"#Fleet.HeavyTruck\"");
        json.Should().NotContain("\"@odata.type\":\"##");
    }

    /// <summary>
    /// A TypeName supplied without a leading '#' must have one added automatically.
    /// </summary>
    [Fact]
    public void Serialize_CustomTypeNameWithoutHashPrefix_AddsHashPrefix()
    {
        var bus = new Bus { Id = 1, Make = "Mercedes", Capacity = 50 };

        var json = JsonSerializer.Serialize<Vehicle>(bus, _options);

        json.Should().Contain("\"@odata.type\":\"#Fleet.PublicBus\"",
            because: "TypeName without # prefix must have # prepended automatically");
        json.Should().NotContain("\"@odata.type\":\"Fleet.PublicBus\"");
    }

    /// <summary>
    /// Three levels of inheritance: @odata.type must still reflect the concrete runtime type,
    /// not an intermediate class.
    /// </summary>
    [Fact]
    public void Serialize_ThreeLevelInheritance_ODataTypeAnnotationReflectsConcreteType()
    {
        var sportsCar = new SportsCar { Id = 1, Make = "Ferrari", Doors = 2, TopSpeedKph = 320 };

        var json = JsonSerializer.Serialize<Vehicle>(sportsCar, _options);

        // Must name the concrete leaf type, not Car or Vehicle
        json.Should().Contain("SportsCar",
            because: "@odata.type must reflect the actual runtime type at any depth of inheritance");
        json.Should().Contain("\"topSpeedKph\"");
    }

    // Additional domain model for new tests
    [ODataTypeAnnotation(TypeName = "Fleet.PublicBus")] // no leading #
    public class Bus : Vehicle { public int Capacity { get; set; } }

    public class SportsCar : Car { public int TopSpeedKph { get; set; } } // three levels deep
}
#pragma warning restore CS1591
