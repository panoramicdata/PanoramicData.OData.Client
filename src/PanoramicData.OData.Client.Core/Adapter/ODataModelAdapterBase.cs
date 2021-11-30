﻿
namespace PanoramicData.OData.Client;

public abstract class ODataModelAdapterBase : IODataModelAdapter
{
    public abstract AdapterVersion AdapterVersion { get; }

    public string ProtocolVersion { get; set; }

    public object Model { get; set; }
}
