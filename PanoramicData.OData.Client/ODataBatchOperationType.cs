namespace PanoramicData.OData.Client;

/// <summary>
/// Represents the type of operation in a batch request.
/// </summary>
public enum ODataBatchOperationType
{
	/// <summary>
	/// GET operation to retrieve an entity.
	/// </summary>
	Get,

	/// <summary>
	/// POST operation to create an entity.
	/// </summary>
	Create,

	/// <summary>
	/// PATCH operation to update an entity.
	/// </summary>
	Update,

	/// <summary>
	/// DELETE operation to remove an entity.
	/// </summary>
	Delete
}
