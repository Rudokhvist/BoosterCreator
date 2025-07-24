﻿using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;
using ArchiSteamFarm.Web.Responses;

namespace BoosterCreator;

internal static class WebRequest {
	internal static async Task<IDocument?> GetBoosterPage(Bot bot) {
		Uri request = new(ArchiWebHandler.SteamCommunityURL, "/tradingcards/boostercreator?l=english");
		HtmlDocumentResponse? boosterPageResponse = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

		return boosterPageResponse?.Content;
	}

	internal static async Task<Steam.BoostersResponse?> CreateBooster(Bot bot, uint appID, uint series, uint nTradabilityPreference) {
		if (appID == 0) {
			bot.ArchiLogger.LogNullError(null, nameof(appID));

			return null;
		}

		Uri request = new(ArchiWebHandler.SteamCommunityURL, "/tradingcards/ajaxcreatebooster");

		// Extra entry for sessionID
		Dictionary<string, string> data = new(4) {
			{ "appid", appID.ToString(CultureInfo.InvariantCulture) },
			{ "series", series.ToString(CultureInfo.InvariantCulture) },
			{ "tradability_preference", nTradabilityPreference.ToString(CultureInfo.InvariantCulture) }
		};

		ObjectResponse<Steam.BoostersResponse>? createBoosterResponse = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<Steam.BoostersResponse>(request, data: data).ConfigureAwait(false);

		return createBoosterResponse?.Content;
	}
}
