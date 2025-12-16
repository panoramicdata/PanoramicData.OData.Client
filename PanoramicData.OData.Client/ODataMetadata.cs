namespace PanoramicData.OData.Client;

/// <summary>
/// Represents OData service metadata (CSDL).
/// </summary>
public class ODataMetadata
{
	/// <summary>
	/// Gets or sets the schema namespace.
	/// </summary>
	public string Namespace { get; set; } = string.Empty;

	/// <summary>
	/// Gets the list of entity types defined in the schema.
	/// </summary>
	public List<ODataEntityType> EntityTypes { get; } = [];

	/// <summary>
	/// Gets the list of complex types defined in the schema.
	/// </summary>
	public List<ODataComplexType> ComplexTypes { get; } = [];

	/// <summary>
	/// Gets the list of enum types defined in the schema.
	/// </summary>
	public List<ODataEnumType> EnumTypes { get; } = [];

	/// <summary>
	/// Gets the list of entity sets defined in the entity container.
	/// </summary>
	public List<ODataEntitySet> EntitySets { get; } = [];

	/// <summary>
	/// Gets the list of singletons defined in the entity container.
	/// </summary>
	public List<ODataSingleton> Singletons { get; } = [];

	/// <summary>
	/// Gets the list of function imports defined in the entity container.
	/// </summary>
	public List<ODataFunctionImport> FunctionImports { get; } = [];

	/// <summary>
	/// Gets the list of action imports defined in the entity container.
	/// </summary>
	public List<ODataActionImport> ActionImports { get; } = [];

	/// <summary>
	/// Gets an entity type by name.
	/// </summary>
	/// <param name="name">The entity type name (without namespace).</param>
	/// <returns>The entity type, or null if not found.</returns>
	public ODataEntityType? GetEntityType(string name)
		=> EntityTypes.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Gets an entity set by name.
	/// </summary>
	/// <param name="name">The entity set name.</param>
	/// <returns>The entity set, or null if not found.</returns>
	public ODataEntitySet? GetEntitySet(string name)
		=> EntitySets.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Represents an OData entity type.
/// </summary>
public class ODataEntityType
{
	/// <summary>
	/// Gets or sets the entity type name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the base type name (for derived types).
	/// </summary>
	public string? BaseType { get; set; }

	/// <summary>
	/// Gets or sets whether this is an abstract type.
	/// </summary>
	public bool IsAbstract { get; set; }

	/// <summary>
	/// Gets or sets whether this is an open type (allows dynamic properties).
	/// </summary>
	public bool IsOpenType { get; set; }

	/// <summary>
	/// Gets or sets whether this is a media entity (has stream content).
	/// </summary>
	public bool HasStream { get; set; }

	/// <summary>
	/// Gets the key properties for this entity type.
	/// </summary>
	public List<string> Key { get; } = [];

	/// <summary>
	/// Gets the properties defined on this entity type.
	/// </summary>
	public List<ODataProperty> Properties { get; } = [];

	/// <summary>
	/// Gets the navigation properties defined on this entity type.
	/// </summary>
	public List<ODataNavigationProperty> NavigationProperties { get; } = [];

	/// <summary>
	/// Gets a property by name.
	/// </summary>
	/// <param name="name">The property name.</param>
	/// <returns>The property, or null if not found.</returns>
	public ODataProperty? GetProperty(string name)
		=> Properties.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Gets a navigation property by name.
	/// </summary>
	/// <param name="name">The navigation property name.</param>
	/// <returns>The navigation property, or null if not found.</returns>
	public ODataNavigationProperty? GetNavigationProperty(string name)
		=> NavigationProperties.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Represents an OData complex type.
/// </summary>
public class ODataComplexType
{
	/// <summary>
	/// Gets or sets the complex type name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the base type name (for derived types).
	/// </summary>
	public string? BaseType { get; set; }

	/// <summary>
	/// Gets or sets whether this is an abstract type.
	/// </summary>
	public bool IsAbstract { get; set; }

	/// <summary>
	/// Gets or sets whether this is an open type.
	/// </summary>
	public bool IsOpenType { get; set; }

	/// <summary>
	/// Gets the properties defined on this complex type.
	/// </summary>
	public List<ODataProperty> Properties { get; } = [];
}

/// <summary>
/// Represents an OData property.
/// </summary>
public class ODataProperty
{
	/// <summary>
	/// Gets or sets the property name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the property type (e.g., "Edm.String", "Edm.Int32", "Collection(Edm.String)").
	/// </summary>
	public string Type { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the property is nullable.
	/// </summary>
	public bool IsNullable { get; set; } = true;

	/// <summary>
	/// Gets or sets the maximum length (for string/binary types).
	/// </summary>
	public int? MaxLength { get; set; }

	/// <summary>
	/// Gets or sets the precision (for decimal/temporal types).
	/// </summary>
	public int? Precision { get; set; }

	/// <summary>
	/// Gets or sets the scale (for decimal types).
	/// </summary>
	public int? Scale { get; set; }

	/// <summary>
	/// Gets or sets the default value.
	/// </summary>
	public string? DefaultValue { get; set; }

	/// <summary>
	/// Gets whether this property is a collection type.
	/// </summary>
	public bool IsCollection => Type.StartsWith("Collection(", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Gets the element type for collection properties.
	/// </summary>
	public string? ElementType => IsCollection
		? Type[11..^1] // Remove "Collection(" prefix and ")" suffix
		: null;
}

/// <summary>
/// Represents an OData navigation property.
/// </summary>
public class ODataNavigationProperty
{
	/// <summary>
	/// Gets or sets the navigation property name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the target type (e.g., "Product", "Collection(Order)").
	/// </summary>
	public string Type { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the navigation property is nullable.
	/// </summary>
	public bool IsNullable { get; set; } = true;

	/// <summary>
	/// Gets or sets the partner navigation property name.
	/// </summary>
	public string? Partner { get; set; }

	/// <summary>
	/// Gets or sets whether the navigation target can be contained.
	/// </summary>
	public bool ContainsTarget { get; set; }

	/// <summary>
	/// Gets whether this is a collection navigation property.
	/// </summary>
	public bool IsCollection => Type.StartsWith("Collection(", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Gets the target entity type name.
	/// </summary>
	public string TargetType => IsCollection
		? Type[11..^1] // Remove "Collection(" prefix and ")" suffix
		: Type;
}

/// <summary>
/// Represents an OData enum type.
/// </summary>
public class ODataEnumType
{
	/// <summary>
	/// Gets or sets the enum type name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the underlying type (default: Edm.Int32).
	/// </summary>
	public string UnderlyingType { get; set; } = "Edm.Int32";

	/// <summary>
	/// Gets or sets whether this is a flags enum.
	/// </summary>
	public bool IsFlags { get; set; }

	/// <summary>
	/// Gets the enum members.
	/// </summary>
	public List<ODataEnumMember> Members { get; } = [];
}

/// <summary>
/// Represents an OData enum member.
/// </summary>
public class ODataEnumMember
{
	/// <summary>
	/// Gets or sets the member name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the member value.
	/// </summary>
	public long? Value { get; set; }
}

/// <summary>
/// Represents an OData entity set.
/// </summary>
public class ODataEntitySet
{
	/// <summary>
	/// Gets or sets the entity set name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the entity type name this set contains.
	/// </summary>
	public string EntityType { get; set; } = string.Empty;

	/// <summary>
	/// Gets the navigation property bindings.
	/// </summary>
	public List<ODataNavigationPropertyBinding> NavigationPropertyBindings { get; } = [];
}

/// <summary>
/// Represents a navigation property binding in an entity set.
/// </summary>
public class ODataNavigationPropertyBinding
{
	/// <summary>
	/// Gets or sets the path to the navigation property.
	/// </summary>
	public string Path { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the target entity set name.
	/// </summary>
	public string Target { get; set; } = string.Empty;
}

/// <summary>
/// Represents an OData singleton.
/// </summary>
public class ODataSingleton
{
	/// <summary>
	/// Gets or sets the singleton name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the entity type name.
	/// </summary>
	public string Type { get; set; } = string.Empty;

	/// <summary>
	/// Gets the navigation property bindings.
	/// </summary>
	public List<ODataNavigationPropertyBinding> NavigationPropertyBindings { get; } = [];
}

/// <summary>
/// Represents an OData function import.
/// </summary>
public class ODataFunctionImport
{
	/// <summary>
	/// Gets or sets the function import name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the function reference.
	/// </summary>
	public string Function { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the entity set path (if returning entities).
	/// </summary>
	public string? EntitySet { get; set; }

	/// <summary>
	/// Gets or sets whether to include in service document.
	/// </summary>
	public bool IncludeInServiceDocument { get; set; }
}

/// <summary>
/// Represents an OData action import.
/// </summary>
public class ODataActionImport
{
	/// <summary>
	/// Gets or sets the action import name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the action reference.
	/// </summary>
	public string Action { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the entity set path (if returning entities).
	/// </summary>
	public string? EntitySet { get; set; }
}
