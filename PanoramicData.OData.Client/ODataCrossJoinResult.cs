namespace PanoramicData.OData.Client;

/// <summary>
/// Represents a cross-join result row containing entities from multiple entity sets.
/// </summary>
public class ODataCrossJoinResult
{
	/// <summary>
	/// Gets the entities in this result row, keyed by entity set name.
	/// </summary>
	public Dictionary<string, JsonElement> Entities { get; } = [];

	/// <summary>
	/// Gets the entity from a specific entity set as the specified type.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entitySetName">The entity set name.</param>
	/// <param name="options">Optional JSON serializer options.</param>
	/// <returns>The entity, or default if not found.</returns>
	public T? GetEntity<T>(string entitySetName, JsonSerializerOptions? options = null)
	{
		if (!Entities.TryGetValue(entitySetName, out var element))
		{
			return default;
		}

		return element.Deserialize<T>(options);
	}

	/// <summary>
	/// Checks if the result contains an entity from the specified entity set.
	/// </summary>
	/// <param name="entitySetName">The entity set name.</param>
	/// <returns>True if the entity exists.</returns>
	public bool HasEntity(string entitySetName) => Entities.ContainsKey(entitySetName);
}
