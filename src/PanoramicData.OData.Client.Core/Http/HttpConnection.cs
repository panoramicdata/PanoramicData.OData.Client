﻿using System;
using System.Net.Http;

namespace PanoramicData.OData.Client;

public class HttpConnection : IDisposable
{
    private HttpMessageHandler _messageHandler;

    public HttpClient HttpClient { get; private set; }

    public HttpConnection(ODataClientSettings settings)
    {
        _messageHandler = CreateMessageHandler(settings);
        HttpClient = CreateHttpClient(settings, _messageHandler);
    }

    public void Dispose()
    {
        if (_messageHandler != null)
        {
            _messageHandler.Dispose();
            _messageHandler = null;
        }

        if (HttpClient != null)
        {
            HttpClient.Dispose();
            HttpClient = null;
        }
    }

    private static HttpClient CreateHttpClient(ODataClientSettings settings, HttpMessageHandler messageHandler)
    {
        if (settings.HttpClient != null)
		{
			return settings.HttpClient;
		}

		if (settings.RequestTimeout >= TimeSpan.FromMilliseconds(1))
		{
			return new HttpClient(messageHandler) { Timeout = settings.RequestTimeout };
		}

		return new HttpClient(messageHandler);
    }

    private static HttpMessageHandler CreateMessageHandler(ODataClientSettings settings)
    {
        if (settings.OnCreateMessageHandler != null)
        {
            return settings.OnCreateMessageHandler();
        }
        else
        {
            var clientHandler = new HttpClientHandler();

            if (settings.Credentials != null)
            {
                clientHandler.Credentials = settings.Credentials;
                clientHandler.PreAuthenticate = true;
            }

            settings.OnApplyClientHandler?.Invoke(clientHandler);

            return clientHandler;
        }
    }
}
