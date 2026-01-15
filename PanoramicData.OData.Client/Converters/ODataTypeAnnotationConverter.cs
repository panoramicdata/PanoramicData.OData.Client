using System.Reflection;

namespace PanoramicData.OData.Client.Converters;

/// <summary>
/// Custom JSON converter that adds the @odata.type annotation for polymorphic/derived types.
/// This is required for OData Table-Per-Hierarchy (TPH) inheritance scenarios where the server
/// needs to know the specific derived type being sent.
/// </summary>
public class ODataTypeAnnotationConverter : JsonConverterFactory
{
	/// <inheritdoc />
	public override bool CanConvert(Type typeToConvert) =>
		// Convert all class types (but not string or value types)
		typeToConvert.IsClass && typeToConvert != typeof(string);

	/// <inheritdoc />
	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var converterType = typeof(ODataTypeAnnotationConverterInner<>).MakeGenericType(typeToConvert);
		return (JsonConverter?)Activator.CreateInstance(converterType);
	}

	private sealed class ODataTypeAnnotationConverterInner<T> : JsonConverter<T> where T : class
	{
		private readonly JsonSerializerOptions _optionsWithoutThisConverter;

		public ODataTypeAnnotationConverterInner()
		{
			// Create options without this converter for internal use (to avoid infinite recursion)
			_optionsWithoutThisConverter = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
				Converters =
				{
					new JsonStringEnumConverter(),
					new ODataDateTimeConverter(),
					new ODataNullableDateTimeConverter()
				}
			};
		}

		public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			// Deserializing: skip the @odata.type annotation and use default behavior
			JsonSerializer.Deserialize<T>(ref reader, _optionsWithoutThisConverter);

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			ArgumentNullException.ThrowIfNull(value);

			writer.WriteStartObject();

			// Add @odata.type if this is a derived type
			var actualType = value.GetType();
			var declaredType = typeof(T);

			if (actualType != declaredType || ShouldAlwaysIncludeTypeAnnotation(actualType))
			{
				var typeName = TypeNameMapper.GetODataTypeName(actualType);
				writer.WriteString("@odata.type", typeName);
			}

			// Serialize all properties
			var jsonElement = JsonSerializer.SerializeToElement(value, actualType, _optionsWithoutThisConverter);

			foreach (var property in jsonElement.EnumerateObject())
			{
				property.WriteTo(writer);
			}

			writer.WriteEndObject();
		}

		private static bool ShouldAlwaysIncludeTypeAnnotation(Type type)
		{
			// Check if the type has a custom attribute indicating it should always include type annotation
			// This can be useful for scenarios where the server always expects @odata.type
			var attr = type.GetCustomAttribute<ODataTypeAnnotationAttribute>();
			return attr?.AlwaysInclude ?? false;
		}
	}
}

/// <summary>
/// Attribute to mark types that should always include @odata.type annotation,
/// even when they're not derived types.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class ODataTypeAnnotationAttribute : Attribute
{
	/// <summary>
	/// Gets or sets whether to always include the @odata.type annotation.
	/// </summary>
	public bool AlwaysInclude { get; set; } = true;

	/// <summary>
	/// Gets or sets the custom OData type name to use instead of the default.
	/// If null, the type name will be derived from the class name.
	/// </summary>
	public string? TypeName { get; set; }
}

/// <summary>
/// Helper class to map .NET type names to OData type names.
/// </summary>
internal static class TypeNameMapper
{
	/// <summary>
	/// Gets the OData type name for a .NET type.
	/// </summary>
	/// <param name="type">The .NET type.</param>
	/// <returns>The OData type name with # prefix.</returns>
	public static string GetODataTypeName(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);

		// Check for custom attribute first
		var attr = type.GetCustomAttribute<ODataTypeAnnotationAttribute>();
		if (!string.IsNullOrEmpty(attr?.TypeName))
		{
			return EnsureHashPrefix(attr.TypeName);
		}

		// Default: Use the full type name including namespace
		// Format: #Namespace.TypeName
		var typeName = type.FullName ?? type.Name;

		// Handle generic types by removing generic parameters
		if (type.IsGenericType)
		{
			var backtickIndex = typeName.IndexOf('`');
			if (backtickIndex > 0)
			{
				typeName = typeName[..backtickIndex];
			}
		}

		return EnsureHashPrefix(typeName);
	}

	private static string EnsureHashPrefix(string typeName) => typeName.StartsWith('#') ? typeName : $"#{typeName}";
}
