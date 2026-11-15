namespace PanoramicData.OData.Client;

/// <summary>
/// ODataClient - the <see cref="ODataClientOptions.IsReadOnly"/> guard.
/// </summary>
public partial class ODataClient
{
	private static readonly HttpRequestOptionsKey<bool> ReadOnlyVerifiedKey = new("PanoramicData.OData.Client.ReadOnlyVerified");

	private void ThrowIfReadOnlyRefuses(HttpRequestMessage request)
	{
		if (!_options.IsReadOnly
			|| IsReadMethod(request.Method)
			|| (request.Options.TryGetValue(ReadOnlyVerifiedKey, out var verified) && verified))
		{
			return;
		}

		var path = request.RequestUri?.OriginalString.Split('?')[0];
		throw new InvalidOperationException($"The client is read-only, so {request.Method} {path} was not sent.");
	}

	private void ThrowIfReadOnlyRefuses(ODataBatchBuilder batch)
	{
		if (_options.IsReadOnly && batch.GetAllOperations().Any(operation => operation.OperationType != ODataBatchOperationType.Get))
		{
			throw new InvalidOperationException("The client is read-only, so a batch containing changes was not sent.");
		}
	}

	private static void MarkReadOnlyVerified(HttpRequestMessage request)
		=> request.Options.Set(ReadOnlyVerifiedKey, true);

	private static bool IsReadMethod(HttpMethod method)
		=> method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options;
}
