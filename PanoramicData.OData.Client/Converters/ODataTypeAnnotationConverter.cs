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
        // Convert all class types (but not string or value types).
        // If the type declares its own [JsonConverter] attribute, step aside and let it
        // handle serialization - our factory lives in the Converters collection which
        // has higher STJ precedence than a type-level attribute, so without this guard
        // we would silently override the user's converter.
        // Exclude OData framework types like Delta<T> to avoid interfering with server-side deserialization.
        typeToConvert.IsClass
        && typeToConvert != typeof(string)
        && !typeToConvert.IsDefined(typeof(JsonConverterAttribute), inherit: false)
        && !IsODataFrameworkType(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(ODataTypeAnnotationConverterInner<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }

    /// <summary>
    /// Determines if the given type is an OData framework type that should not be processed
    /// by this converter to avoid interference with server-side OData deserialization.
    /// </summary>
    private static bool IsODataFrameworkType(Type type)
    {
        // Check if it's a Delta<T> type from Microsoft.AspNetCore.OData
        if (type.IsGenericType && type.GetGenericTypeDefinition().Name == "Delta`1")
        {
            return true;
        }

        // Check namespace to avoid other OData framework types
        var namespaceName = type.Namespace;
        if (namespaceName != null)
        {
            return namespaceName.StartsWith("Microsoft.AspNetCore.OData.", StringComparison.Ordinal) ||
                   namespaceName.StartsWith("Microsoft.OData.", StringComparison.Ordinal);
        }

        return false;
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

            var actualType = value.GetType();
            var declaredType = typeof(T);

            if ((actualType != declaredType && !IsAnonymousType(actualType)) || ShouldAlwaysIncludeTypeAnnotation(actualType))
            {
                var typeName = TypeNameMapper.GetODataTypeName(actualType);
                writer.WriteString("@odata.type", typeName);
            }

            // Serialize all properties using the actual runtime type
            var jsonElement = JsonSerializer.SerializeToElement(value, actualType, _optionsWithoutThisConverter);

            foreach (var property in jsonElement.EnumerateObject())
            {
                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        private static bool ShouldAlwaysIncludeTypeAnnotation(Type type)
        {
            var attr = type.GetCustomAttribute<ODataTypeAnnotationAttribute>();
            return attr?.AlwaysInclude ?? false;
        }

        // Compiler-generated anonymous types have names like "<>f__AnonymousType0" - they are
        // not part of any inheritance hierarchy and must never receive @odata.type.
        private static bool IsAnonymousType(Type type) =>
            type.Namespace is null
            && type.Name.StartsWith('<')
            && type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false);
    }
}

/// <summary>
/// Attribute to mark types that should always include the @odata.type annotation,
/// even when not used polymorphically.
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
    /// If null, the type name is derived from the .NET type's full name.
    /// </summary>
    public string? TypeName { get; set; }
}

/// <summary>
/// Maps .NET type names to OData @odata.type annotation values.
/// </summary>
internal static class TypeNameMapper
{
    /// <summary>
    /// Gets the OData type name for a .NET type, with a leading # prefix.
    /// </summary>
    public static string GetODataTypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var attr = type.GetCustomAttribute<ODataTypeAnnotationAttribute>();

        if (!string.IsNullOrEmpty(attr?.TypeName))
        {
            return EnsureHashPrefix(attr.TypeName);
        }

        var typeName = type.FullName ?? type.Name;

        if (type.IsGenericType)
        {
            var backtickIndex = typeName.IndexOf('`', StringComparison.Ordinal);
            if (backtickIndex > 0)
            {
                typeName = typeName[..backtickIndex];
            }
        }

        return EnsureHashPrefix(typeName);
    }

    private static string EnsureHashPrefix(string typeName) =>
        typeName.StartsWith('#') ? typeName : $"#{typeName}";
}
