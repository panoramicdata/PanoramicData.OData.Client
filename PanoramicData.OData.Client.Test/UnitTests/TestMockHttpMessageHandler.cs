namespace PanoramicData.OData.Client.Test.UnitTests;

public partial class ODataQueryHelperTests
{
	/// <summary>
	/// Simple mock HTTP message handler for testing.
	/// </summary>
	private sealed class TestMockHttpMessageHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

		public TestMockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
		{
			_handler = handler;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(_handler(request));
	}
}