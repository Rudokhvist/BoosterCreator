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
			public readonly EResult Result;

			[JsonConstructor]
			public EResultResponse() { }
		}

		internal sealed class BoosterInfo {
			[JsonInclude]
			[JsonPropertyName("appid")]
			[JsonRequired]
			internal readonly uint AppID;

			[JsonInclude]
			[JsonPropertyName("name")]
			[JsonRequired]
			internal readonly string Name;

			[JsonInclude]
			[JsonPropertyName("series")]
			[JsonRequired]
			internal readonly uint Series;

			[JsonInclude]
			[JsonPropertyName("price")]
			[JsonRequired]
			internal readonly uint Price;

			[JsonInclude]
			[JsonPropertyName("unavailable")]
			[JsonRequired]
			[JsonDisallowNull]
			internal readonly bool Unavailable;

			[JsonInclude]
			[JsonPropertyName("available_at_time")]
			[JsonRequired]
			[JsonDisallowNull]
			internal readonly string AvailableAtTime;

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
			internal readonly uint GooAmount;

			[JsonInclude]
			[JsonPropertyName("tradable_goo_amount")]
			[JsonRequired]
			internal readonly uint TradableGooAmount;

			[JsonInclude]
			[JsonPropertyName("untradable_goo_amount")]
			[JsonRequired]
			internal readonly uint UntradableGooAmount;

			[JsonInclude]
			[JsonPropertyName("purchase_result")]
			[JsonDisallowNull]
			internal readonly EResultResponse Result;

			[JsonConstructor]
			private BoostersResponse() => Result = new EResultResponse();
		}
	}
}
#pragma warning restore 649
