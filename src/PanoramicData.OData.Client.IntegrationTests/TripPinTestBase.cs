﻿using System;
using System.Threading.Tasks;

namespace PanoramicData.OData.Client.Tests
{
    public class TripPinTestBase : TestBase
    {
        protected TripPinTestBase(string serviceUri, ODataPayloadFormat payloadFormat)
            : base(serviceUri, payloadFormat)
        {
        }

#pragma warning disable 1998
        protected async override Task DeleteTestData()
        {
            try
            {
            }
            catch (Exception)
            {
            }
        }
#pragma warning restore 1998
    }
}
