﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using PanoramicData.OData.Client.V4.Adapter;
using Xunit;

namespace PanoramicData.OData.Client.Tests.Core
{
	public class ActionWithEnumTests : CoreTestBase
	{
		public override string MetadataFile => "ActionWithEnum.xml";
		public override IFormatSettings FormatSettings => new ODataV4Format();

		private enum Rank
		{
			First,
			Second,
			Third,
		}

		[Fact]
		public async Task ActionWithEnum()
		{
			var requestWriter = new RequestWriter(_session, await _client.GetMetadataAsync<IEdmModel>(), null);
			var result = await requestWriter.CreateActionRequestAsync("Entity", "MakeFromParam", null,
						new Dictionary<string, object>() { { "Name", "Entity Name" }, { "Rank", Rank.Second } }, false);
			Assert.Equal("POST", result.Method);
		}
	}
}
