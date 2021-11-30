﻿using System;
using System.Collections.Generic;

namespace PanoramicData.OData.Client;

public interface IODataAdapter
{
    AdapterVersion AdapterVersion { get; }

    ODataPayloadFormat DefaultPayloadFormat { get; }

    string ProtocolVersion { get; set; }

    object Model { get; set; }

    string GetODataVersionString();

    IMetadata GetMetadata();

    ICommandFormatter GetCommandFormatter();

    IResponseReader GetResponseReader();

    IRequestWriter GetRequestWriter(Lazy<IBatchWriter> deferredBatchWriter);

    IBatchWriter GetBatchWriter(IDictionary<object, IDictionary<string, object>> batchEntries);
}
