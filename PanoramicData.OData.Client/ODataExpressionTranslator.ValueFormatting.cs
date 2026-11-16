using System.Reflection;

namespace PanoramicData.OData.Client;

/// <summary>
/// Member path, value evaluation and literal formatting for ODataQueryBuilder.
/// </summary>
internal static partial class ODataExpressionTranslator
{
	internal static string GetMemberPath(MemberExpression member) => MemberPathResolver.GetFlatPath(member);

	private static object? GetValue(Expression expression) => expression switch
	{
		ConstantExpression constant => constant.Value,
		MemberExpression member => GetMemberValue(member),
		_ => EvaluateExpression(expression) // Use optimized evaluation
	};

	private static object? GetMemberValue(MemberExpression member)
	{
		// Use the same optimized reflection-based evaluation
		if (TryEvaluateWithReflection(member, out var result))
		{
			return result;
		}

		// Fallback to compilation for complex cases
		var objectMember = Expression.Convert(member, typeof(object));
		var getterLambda = Expression.Lambda<Func<object>>(objectMember);
		var getter = getterLambda.Compile();
		return getter();
	}

	private static string FormatValue(object? value, Type? expectedType = null)
		=> TryFormatAsEnum(value, expectedType, out var formattedEnum)
			? formattedEnum
			: FormatLiteral(value);

	/// <summary>
	/// Formats a value the expression's target type says is an enum, whether it arrived as the
	/// enum itself or as its underlying numeric value.
	/// </summary>
	/// <returns><see langword="true"/> if the value was formatted as an enum.</returns>
	private static bool TryFormatAsEnum(object? value, Type? expectedType, out string formatted)
	{
		formatted = string.Empty;

		if (expectedType is not null)
		{
			expectedType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;
		}

		if (value is null || expectedType?.IsEnum != true)
		{
			return false;
		}

		if (value is Enum enumValue)
		{
			formatted = $"'{enumValue}'";
			return true;
		}

		return TryFormatEnumFromUnderlyingValue(expectedType, value, out formatted);
	}

	private static string FormatLiteral(object? value)
		=> value switch
		{
			null => "null",
			string s => ODataLiteral.Quote(s),
			bool b => b.ToString().ToLowerInvariant(),
			DateTime dt => FormatDateTime(dt),
			DateTimeOffset dto => $"{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}",
			Guid g => g.ToString(),
			Enum e => $"'{e}'",
			_ => value.ToString() ?? "null"
		};

	private static bool TryFormatEnumFromUnderlyingValue(Type enumType, object value, out string formatted)
	{
		formatted = string.Empty;

		try
		{
			var enumValue = Enum.ToObject(enumType, value);
			formatted = $"'{enumValue}'";
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string FormatDateTime(DateTime dt)
	{
		// Convert to UTC before formatting with Z suffix
		var utc = dt.Kind switch
		{
			DateTimeKind.Utc => dt,
			DateTimeKind.Local => dt.ToUniversalTime(),
			DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc), // Assume UTC for unspecified
			_ => dt.ToUniversalTime()
		};
		return $"{utc:yyyy-MM-ddTHH:mm:ssZ}";
	}

	internal static string FormatFunctionParameters(object parameters)
	{
		var type = parameters.GetType();

		// Get or create cached PropertyInfo array
		var props = ODataExpressionLookups.PropertyCache.GetValue(type, t =>
			t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

		var paramStrings = props
			.Select(p => (Name: p.Name, Value: p.GetValue(parameters)))
			.Where(p => p.Value is not null)
			.Select(p =>
			{
				var formattedValue = FormatFunctionParameterValue(p.Value);
				return $"{char.ToLowerInvariant(p.Name[0])}{p.Name[1..]}={formattedValue}";
			});

		return string.Join(",", paramStrings);
	}

	private static string FormatFunctionParameterValue(object? value) => value switch
	{
		null => "null",
		string s => ODataLiteral.Quote(s),
		bool b => b.ToString().ToLowerInvariant(),
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		double d => d.ToString(CultureInfo.InvariantCulture),
		decimal dec => dec.ToString(CultureInfo.InvariantCulture),
		DateTime dt => FormatDateTime(dt),
		DateTimeOffset dto => $"{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}",
		Guid g => g.ToString(),
		Enum e => $"'{e}'",
		Array arr => FormatArrayParameter(arr),
		System.Collections.IEnumerable enumerable => FormatEnumerableParameter(enumerable),
		_ => value?.ToString() ?? "null"
	};

	private static string FormatArrayElementValue(object? value) => value switch
	{
		null => "null",
		string s => s,
		bool b => b.ToString().ToLowerInvariant(),
		int i => i.ToString(CultureInfo.InvariantCulture),
		long l => l.ToString(CultureInfo.InvariantCulture),
		double d => d.ToString(CultureInfo.InvariantCulture),
		decimal dec => dec.ToString(CultureInfo.InvariantCulture),
		DateTime dt => FormatDateTime(dt),
		DateTimeOffset dto => $"{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}",
		Guid g => g.ToString(),
		Enum e => e.ToString(),
		_ => value?.ToString() ?? "null"
	};

	private static string FormatArrayParameter(Array arr)
	{
		var elements = new List<string>();
		foreach (var item in arr)
		{
			elements.Add(FormatArrayElementValue(item));
		}

		return $"[{string.Join(",", elements)}]";
	}

	private static string FormatEnumerableParameter(System.Collections.IEnumerable enumerable)
	{
		var elements = new List<string>();
		foreach (var item in enumerable)
		{
			elements.Add(FormatArrayElementValue(item));
		}

		return $"[{string.Join(",", elements)}]";
	}
}
