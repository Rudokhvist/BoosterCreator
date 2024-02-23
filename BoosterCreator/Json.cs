using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using ArchiSteamFarm.Helpers.Json;
using SteamKit2;
#pragma warning disable 649
namespace BoosterCreator {
	internal static class Steam {
		internal class EResultResponse {
			[JsonInclude]
			[JsonPropertyName("success")]
			[JsonRequired]
			public EResult Result { get; private init; }

			[JsonConstructor]
			public EResultResponse() { }
		}

		internal sealed class BoosterInfo {
			[JsonInclude]
			[JsonPropertyName("appid")]
			[JsonRequired]
			internal uint AppID { get; private init; }

			[JsonInclude]
			[JsonPropertyName("name")]
			[JsonRequired]
			internal string Name { get; private init; }

			[JsonInclude]
			[JsonPropertyName("series")]
			[JsonRequired]
			internal uint Series { get; private init; }

			[JsonInclude]
			[JsonPropertyName("price")]
			[JsonRequired]
			internal uint Price { get; private init; }

			[JsonInclude]
			[JsonPropertyName("unavailable")]
			[JsonRequired]
			internal bool Unavailable { get; private init; }

			[JsonInclude]
			[JsonPropertyName("available_at_time")]
			[JsonRequired]
			internal string AvailableAtTime { get; private init; }

			[JsonConstructor]
			private BoosterInfo() {
				Name = "";
				AvailableAtTime = "";
			 }
		}


		internal sealed class BoostersResponse {
			[JsonInclude]
			[JsonPropertyName("goo_amount")]
			[JsonRequired]
			internal uint GooAmount { get; private init; }

			[JsonInclude]
			[JsonPropertyName("tradable_goo_amount")]
			[JsonRequired]
			internal uint TradableGooAmount { get; private init; }

			[JsonInclude]
			[JsonPropertyName("untradable_goo_amount")]
			[JsonRequired]
			internal uint UntradableGooAmount { get; private init; }

			[JsonInclude]
			[JsonPropertyName("purchase_result")]
			[JsonDisallowNull]
			internal EResultResponse Result { get; private init; }

			[JsonConstructor]
			private BoostersResponse() => Result = new EResultResponse();
		}
	}
}
#pragma warning restore 649
