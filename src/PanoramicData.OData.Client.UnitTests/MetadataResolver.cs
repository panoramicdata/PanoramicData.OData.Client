﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PanoramicData.OData.Client.Tests
{
	public static class MetadataResolver
	{

		private static string GetResourceAsString(string resourceName)
		{
			var assembly = Assembly.GetExecutingAssembly();
			var resourceNames = assembly.GetManifestResourceNames();
			var completeResourceName = resourceNames.FirstOrDefault(o => o.EndsWith("." + resourceName, StringComparison.CurrentCultureIgnoreCase));
			using var resourceStream = assembly.GetManifestResourceStream(completeResourceName);
			var reader = new StreamReader(resourceStream);
			return reader.ReadToEnd();
		}

		public static string GetMetadataDocument(string documentName) => GetResourceAsString(@"Resources." + documentName);
	}
}
