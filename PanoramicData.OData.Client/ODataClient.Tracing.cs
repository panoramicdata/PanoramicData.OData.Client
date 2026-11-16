namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - request and response tracing.
/// </summary>
public partial class ODataClient
{
	private async Task LogRequestTraceAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (!_logger.IsEnabled(LogLevel.Trace))
		{
			return;
		}

		var sb = new StringBuilder();
		sb.AppendLine("=== HTTP Request ===");
		sb.AppendLine(CultureInfo.InvariantCulture, $"{request.Method} {request.RequestUri}");

		sb.AppendLine("--- Request Headers ---");
		foreach (var header in request.Headers)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"{header.Key}: {HttpExtensions.RedactIfSensitive(header.Key, header.Value)}");
		}

		if (request.Content is not null)
		{
			foreach (var header in request.Content.Headers)
			{
				sb.AppendLine(CultureInfo.InvariantCulture, $"{header.Key}: {HttpExtensions.RedactIfSensitive(header.Key, header.Value)}");
			}

			sb.AppendLine("--- Request Body ---");
			var body = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
			sb.AppendLine(body);
		}

		LoggerMessages.LogRequestTrace(_logger, sb.ToString());
	}

	private async Task LogResponseTraceAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (!_logger.IsEnabled(LogLevel.Trace))
		{
			return;
		}

		var sb = new StringBuilder();
		sb.AppendLine("=== HTTP Response ===");
		sb.AppendLine(CultureInfo.InvariantCulture, $"Status: {(int)response.StatusCode} {response.ReasonPhrase}");

		sb.AppendLine("--- Response Headers ---");
		foreach (var header in response.Headers)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"{header.Key}: {HttpExtensions.RedactIfSensitive(header.Key, header.Value)}");
		}

		foreach (var header in response.Content.Headers)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"{header.Key}: {HttpExtensions.RedactIfSensitive(header.Key, header.Value)}");
		}

		sb.AppendLine("--- Response Body ---");
		var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		sb.AppendLine(body);
		LoggerMessages.LogResponseTrace(_logger, sb.ToString());
	}

	private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
	{
		var clone = new HttpRequestMessage(request.Method, request.RequestUri);

		foreach (var header in request.Headers)
		{
			clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		if (request.Content is not null)
		{
			var content = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
			clone.Content = new StringContent(content, Encoding.UTF8, request.Content.Headers.ContentType?.MediaType ?? "application/json");
		}

		return clone;
	}
}
