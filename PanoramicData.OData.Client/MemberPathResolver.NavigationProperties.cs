using System.Reflection;

namespace PanoramicData.OData.Client;

internal static partial class MemberPathResolver
{
	/// <summary>
	/// Well-known scalar (non-navigation) reference/value types that aren't caught by
	/// <see cref="Type.IsPrimitive"/> or <see cref="Type.IsEnum"/>.
	/// </summary>
	private static readonly HashSet<Type> _scalarTypes =
	[
		typeof(string),
		typeof(DateTime),
		typeof(DateTimeOffset),
		typeof(DateOnly),
		typeof(TimeOnly),
		typeof(TimeSpan),
		typeof(Guid),
		typeof(decimal),
		typeof(byte[])
	];

	/// <summary>
	/// Determines if a property is a navigation property (vs a scalar property).
	/// Navigation properties are entity references or collections of entities.
	/// Scalar properties are primitives, strings, dates, guids, etc.
	/// </summary>
	internal static bool IsNavigationProperty(PropertyInfo property)
	{
		var propertyType = property.PropertyType;

		// Check for nullable types - get underlying type
		var underlyingType = Nullable.GetUnderlyingType(propertyType);
		if (underlyingType is not null)
		{
			propertyType = underlyingType;
		}

		// Primitives, enums, and well-known scalar types (string, dates, guid, decimal, byte[]) are scalar
		if (propertyType.IsPrimitive || propertyType.IsEnum || _scalarTypes.Contains(propertyType))
		{
			return false;
		}

		// Collections are navigation properties (string, the one scalar IEnumerable, is already handled above)
		if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyType))
		{
			return true;
		}

		// Reference types that are classes are typically navigation properties
		// (entity references like Tenant, Role, etc.)
		return propertyType.IsClass;
	}
}
