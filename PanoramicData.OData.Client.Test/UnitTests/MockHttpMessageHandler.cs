namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Simple mock HTTP message handler for testing.
/// </summary>
public class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
{
	/// <summary>
	/// Sends an HTTP request asynchronously and returns the corresponding HTTP response.
	/// </summary>
	/// <param name="request">The HTTP request message to send. Cannot be null.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
	/// <returns>A task that represents the asynchronous send operation. The task result contains the HTTP response message.</returns>
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(handler(request));
}
