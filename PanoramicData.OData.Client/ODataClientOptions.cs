namespace PanoramicData.OData.Client;

/// <summary>
/// Configuration options for the OData client.
/// </summary>
public class ODataClientOptions
{
	/// <summary>
	/// The base URL of the OData service (e.g., "https://api.example.com/odata").
	/// </summary>
	public required string BaseUrl { get; set; }

	/// <summary>
	/// Optional HttpClient to use. If not provided, a new one will be created.
	/// </summary>
	public HttpClient? HttpClient { get; set; }

	/// <summary>
	/// Request timeout. Default is 5 minutes.
	/// </summary>
	public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

	/// <summary>
	/// Action to configure request headers before each request.
	/// </summary>
	public Action<HttpRequestMessage>? ConfigureRequest { get; set; }

	/// <summary>
	/// Number of retry attempts for transient failures. Default is 3.
	/// </summary>
	public int RetryCount { get; set; } = 3;

	/// <summary>
	/// Delay between retry attempts. Default is 1 second.
	/// </summary>
	public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

	/// <summary>
	/// Gets or sets the level at which individual failed attempts that will be retried are logged.
	/// </summary>
	/// <remarks>
	/// Defaults to <see cref="LogLevel.Debug"/>, so transient failures that recover on retry do not
	/// flood the logs. Set to <see cref="LogLevel.Warning"/> to log every failed attempt prominently,
	/// or <see cref="LogLevel.None"/> to disable per-attempt logging entirely.
	/// A single <see cref="LogLevel.Warning"/> is always logged when all retries are exhausted,
	/// regardless of this setting.
	/// </remarks>
	public LogLevel RetryAttemptLogLevel { get; set; } = LogLevel.Debug;

	/// <summary>
	/// Gets or sets the options to use when serializing or deserializing JSON content.
	/// </summary>
	/// <remarks>If not set, default serialization options are used. Use this property to customize serialization
	/// behavior, such as property naming policies, converters, or formatting.</remarks>
	public JsonSerializerOptions? JsonSerializerOptions { get; set; }

	/// <summary>
	/// Gets or sets the logger used to record diagnostic and operational messages.
	/// </summary>
	/// <remarks>Assign an implementation of <see cref="ILogger"/> to enable logging. If not set, logging is
	/// disabled.</remarks>
	public ILogger? Logger { get; set; }

	/// <summary>
	/// Gets or sets the duration for which metadata should be cached.
	/// </summary>
	/// <remarks>
	/// When set to a non-null value, the client will cache metadata responses for the specified duration.
	/// Subsequent calls to GetMetadataAsync or GetMetadataXmlAsync
	/// will return the cached data until the cache expires or <see cref="ODataClient.InvalidateMetadataCache"/> is called.
	/// Set to <c>null</c> (the default) to disable caching.
	/// A common value is <c>TimeSpan.FromHours(1)</c> since OData metadata rarely changes.
	/// </remarks>
	public TimeSpan? MetadataCacheDuration { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether entity set names should be automatically pluralized
	/// when using <c>For&lt;T&gt;()</c> without an explicit entity set name.
	/// </summary>
	/// <remarks>
	/// When <c>true</c> (the default), the entity set name is derived from the type name using standard
	/// pluralization rules (e.g., <c>Person</c> → <c>People</c>, <c>Mailbox</c> → <c>Mailboxes</c>).
	/// Set to <c>false</c> to use the type name as-is, which is useful for APIs such as Exchange Online
	/// that use singular endpoint names (e.g., <c>Mailbox</c>, not <c>Mailboxes</c>).
	/// You can still override the name per-call using <c>For&lt;T&gt;("EntitySetName")</c>.
	/// Defaults to <c>true</c>.
	/// </remarks>
	public bool AutoPluralization { get; set; } = true;

	/// <summary>
	/// Gets or sets a function that resolves entity set names for the parameterless
	/// <c>For&lt;T&gt;()</c> overload.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The resolver is the first step in entity set name resolution. When it returns a non-empty
	/// value that value is used verbatim, short-circuiting the <c>[EntitySet]</c> attribute lookup
	/// and the <see cref="AutoPluralization"/> convention. Return <c>null</c>, an empty string, or
	/// whitespace to fall through to those existing conventions.
	/// </para>
	/// <para>
	/// The function is invoked on every parameterless <c>For&lt;T&gt;()</c> call, so it should be
	/// fast and free of side effects. The explicit <c>For&lt;T&gt;("EntitySetName")</c> overload
	/// never invokes the resolver. A typical use is mapping a model type to an entity set via a
	/// custom attribute:
	/// </para>
	/// <example>
	/// <code>
	/// EntitySetNameResolver = type =>
	///     type.GetCustomAttribute&lt;CollectionNameAttribute&gt;()?.Name
	/// </code>
	/// </example>
	/// </remarks>
	public Func<Type, string?>? EntitySetNameResolver { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether a 404 Not Found response should return <c>null</c>
	/// instead of throwing an <see cref="ODataNotFoundException"/>.
	/// </summary>
	/// <remarks>
	/// When <c>true</c>, methods such as <see cref="ODataClient.GetByKeyAsync{T, TKey}(TKey, ODataQueryBuilder{T}?, CancellationToken)"/>
	/// and <see cref="ODataClient.GetByKeyOrDefaultAsync{T, TKey}(TKey, ODataQueryBuilder{T}?, CancellationToken)"/>
	/// will return <c>null</c> instead of throwing when the resource is not found.
	/// This mirrors the behaviour of <c>IgnoreResourceNotFoundException</c> in Simple.OData.Client.
	/// Defaults to <c>false</c>.
	/// </remarks>
	public bool IgnoreResourceNotFoundException { get; set; }
}
