﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.OData.Client;

/// <summary>
/// Performs batch processing of OData requests by grouping multiple operations in a single HTTP POST request in accordance with OData protocol
/// </summary>
public class ODataBatch
{
    private readonly ODataClient _client;
    private readonly List<Func<IODataClient, Task>> _actions = new();
    private readonly ConcurrentDictionary<object, IDictionary<string, object>> _entryMap = new();
    private readonly Dictionary<string, string> _headers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ODataBatch"/> class.
    /// </summary>
    /// <param name="baseUri">The URL base.</param>
    public ODataBatch(Uri baseUri)
        : this(new ODataClientSettings { BaseUri = baseUri })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ODataBatch"/> class.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public ODataBatch(ODataClientSettings settings)
    {
        _client = new ODataClient(settings, _entryMap);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ODataBatch"/> class.
    /// </summary>
    /// <param name="client">The OData client which settings will be used to create a batch.</param>
    public ODataBatch(IODataClient client) : this(client, false) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ODataBatch"/> class.
    /// </summary>
    /// <param name="client">The OData client which will be used to create a batch.</param>
    /// <param name="reuseSession">Flag indicating that the existing session from the <see cref="ODataClient"/>
    /// should be used rather than creating a new one.
    /// </param>
    public ODataBatch(IODataClient client, bool reuseSession)
    {
        _client = reuseSession
            ? new ODataClient((client as ODataClient), _entryMap)
            : new ODataClient((client as ODataClient).Session.Settings, _entryMap);
    }
    /// <summary>
    /// Adds an OData command to an OData batch.
    /// </summary>
    /// <param name="batch">The OData batch.</param>
    /// <param name="action">The command to add to the batch.</param>
    /// <returns></returns>
    public static ODataBatch operator +(ODataBatch batch, Func<IODataClient, Task> action)
    {
        batch._actions.Add(action);
        return batch;
    }

	/// <summary>
	/// Executes the OData batch by submitting pending requests to the OData service.
	/// </summary>
	/// <returns></returns>
	public Task ExecuteAsync() => ExecuteAsync(CancellationToken.None);

	/// <summary>
	/// Executes the OData batch by submitting pending requests to the OData service.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns></returns>
	public Task ExecuteAsync(CancellationToken cancellationToken) => _client.ExecuteBatchAsync(_actions, _headers, cancellationToken);

	/// <summary>
	/// Adds a header to be included in the HTTP request.
	/// </summary>
	/// <param name="name">The header name.</param>
	/// <param name="value">The header value.</param>
	/// <returns>Self.</returns>
	public ODataBatch WithHeader(string name, string value)
    {
        _headers.Add(name, value);
        return this;
    }

    /// <summary>
    /// Adds a collection of headers to be included in the HTTP request.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>Self.</returns>
    public ODataBatch WithHeaders(IDictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            WithHeader(header.Key, header.Value);
        }

        return this;
    }
}
