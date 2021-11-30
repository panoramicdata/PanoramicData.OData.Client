﻿using Microsoft.OData;

namespace PanoramicData.OData.Client.V4.Adapter;

internal static class ODataExtensions
{
	public static ODataMessageReaderSettings ToReaderSettings(this ISession session) => session.Settings.ToReaderSettings();

	public static ODataMessageReaderSettings ToReaderSettings(this ODataClientSettings settings)
    {
        var readerSettings = new ODataMessageReaderSettings();
        // TODO ODataLib7
        if (settings.IgnoreUnmappedProperties)
		{
			readerSettings.Validations &= ~Microsoft.OData.ValidationKinds.ThrowOnUndeclaredPropertyForNonOpenType;
		}

		readerSettings.MessageQuotas.MaxReceivedMessageSize = int.MaxValue;
        readerSettings.ShouldIncludeAnnotation = x => settings.IncludeAnnotationsInResults;

        if (!settings.ReadUntypedAsString)
        {
            readerSettings.Version = ODataVersion.V401;
            readerSettings.MaxProtocolVersion = ODataVersion.V401;
            readerSettings.ReadUntypedAsString = false;
        }

        return readerSettings;
    }
}
