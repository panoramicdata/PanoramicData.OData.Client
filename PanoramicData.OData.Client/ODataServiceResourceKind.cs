namespace PanoramicData.OData.Client;

/// <summary>
/// The kind of resource in an OData service document.
/// </summary>
public enum ODataServiceResourceKind
{
	/// <summary>
	/// An entity set (collection of entities).
	/// </summary>
	EntitySet,

	/// <summary>
	/// A singleton (single entity instance).
	/// </summary>
	Singleton,

	/// <summary>
	/// A function import (unbound function).
	/// </summary>
	FunctionImport,

	/// <summary>
	/// A nested service document.
	/// </summary>
	ServiceDocument
}
