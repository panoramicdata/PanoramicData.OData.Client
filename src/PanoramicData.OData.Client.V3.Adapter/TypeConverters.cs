﻿using System.Collections.Generic;
using System.Spatial;

namespace PanoramicData.OData.Client.V3.Adapter;

public static class TypeConverters
{
	public static GeographyPoint CreateGeographyPoint(IDictionary<string, object> source) => GeographyPoint.Create(
			CoordinateSystem.Geography(source.ContainsKey("CoordinateSystem")
				? source.GetValueOrDefault<CoordinateSystem>("CoordinateSystem").EpsgId
				: null),
			source.GetValueOrDefault<double>("Latitude"),
			source.GetValueOrDefault<double>("Longitude"),
			source.GetValueOrDefault<double?>("Z"),
			source.GetValueOrDefault<double?>("M"));

	public static GeometryPoint CreateGeometryPoint(IDictionary<string, object> source) => GeometryPoint.Create(
			CoordinateSystem.Geometry(source.ContainsKey("CoordinateSystem")
				? source.GetValueOrDefault<CoordinateSystem>("CoordinateSystem").EpsgId
				: null),
			source.GetValueOrDefault<double>("Latitude"),
			source.GetValueOrDefault<double>("Longitude"),
			source.GetValueOrDefault<double?>("Z"),
			source.GetValueOrDefault<double?>("M"));

	public static object ConvertToEdmDate(object source) => source;

	private static T GetValueOrDefault<T>(this IDictionary<string, object> source, string key)
    {
        if (source.TryGetValue(key, out var value))
        {
            return (T)value;
        }
        else
        {
            return default(T);
        }
    }
}
