﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.OData;

namespace PanoramicData.OData.Client.V3.Adapter;

internal class ODataRequestMessage : IODataRequestMessageAsync
{
	private MemoryStream _stream;
	private readonly Dictionary<string, string> _headers = new();

	public ODataRequestMessage()
	{
	}

	public string GetHeader(string headerName)
		=> _headers.TryGetValue(headerName, out var value) ? value : null;

	public void SetHeader(string headerName, string headerValue) => _headers.Add(headerName, headerValue);

	public Stream GetStream()
		=> _stream ??= new MemoryStream();

	public Task<Stream> GetStreamAsync()
	{
		var completionSource = new TaskCompletionSource<Stream>();
		completionSource.SetResult(GetStream());
		return completionSource.Task;
	}

	public IEnumerable<KeyValuePair<string, string>> Headers => _headers;

	public Uri Url { get; set; }

	public string Method { get; set; }
}
