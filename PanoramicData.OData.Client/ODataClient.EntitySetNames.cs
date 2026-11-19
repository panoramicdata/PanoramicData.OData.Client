namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - entity set name resolution.
/// </summary>
public partial class ODataClient
{
	/// <summary>
	/// Suffixes whose plural takes 'es' rather than a bare 's'.
	/// </summary>
	private static readonly string[] EsPluralSuffixes = ["s", "x", "z", "ch", "sh"];

	private string GetEntitySetName<T>()
	{
		var type = typeof(T);

		var resolvedEntitySetName = _options.EntitySetNameResolver?.Invoke(type);
		if (!string.IsNullOrWhiteSpace(resolvedEntitySetName))
		{
			return resolvedEntitySetName;
		}

		return TryGetDeclaredEntitySetName(type)
			?? (_options.AutoPluralization ? Pluralize(type.Name) : type.Name);
	}

	/// <summary>
	/// Reads the entity set name off an [EntitySet("...")] attribute, as emitted by
	/// Microsoft.OData.Client generated DTOs. Matched by name rather than by type, so the
	/// attribute's assembly does not have to be referenced.
	/// </summary>
	/// <returns>The declared name, or <see langword="null"/> if the type does not declare one.</returns>
	private static string? TryGetDeclaredEntitySetName(Type type)
	{
		var entitySetAttr = type.GetCustomAttributes(false)
			.FirstOrDefault(a => a.GetType().Name == "EntitySetAttribute");
		if (entitySetAttr is null)
		{
			return null;
		}

		var prop = entitySetAttr.GetType().GetProperty("EntitySet")
			?? entitySetAttr.GetType().GetProperty("Name");

		return prop?.GetValue(entitySetAttr) is string entitySetName && !string.IsNullOrWhiteSpace(entitySetName)
			? entitySetName
			: null;
	}

	/// <summary>
	/// Applies the simple English pluralisation used to derive an entity set name from a type name.
	/// </summary>
	private static string Pluralize(string name)
	{
		if (name.EndsWith('y'))
		{
			return name[..^1] + "ies";
		}

		return Array.Exists(EsPluralSuffixes, suffix => name.EndsWith(suffix, StringComparison.Ordinal))
			? name + "es"
			: name + "s";
	}
}
