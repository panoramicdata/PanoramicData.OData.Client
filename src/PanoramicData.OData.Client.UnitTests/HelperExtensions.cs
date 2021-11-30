﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.OData.Client.Tests
{
    internal static class HelperExtensions
    {
        /// <summary>
        /// Helper extension to derive the collection type from an instance type. Use to work with anonymous types as entity classes in tests. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="oDataClient"></param>
        /// <param name="_"></param>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public static IBoundClient<T> For<T>(this IODataClient oDataClient, T _, string collectionName)
            where T : class
           => oDataClient.For<T>(collectionName);
    }
}
