using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Plugins.Interfaces;
using System.Text.Json;

namespace BoosterCreator {
	[Export(typeof(IPlugin))]
	public sealed class BoosterCreator : IBotModules, IBotCommand2 {
		public string Name => nameof(BoosterCreator);
		public Version Version => typeof(BoosterCreator).Assembly.GetName().Version ?? new Version("0");

		public Task OnLoaded() {
			ASF.ArchiLogger.LogGenericInfo("BoosterCreator ASF Plugin by Out (https://steamcommunity.com/id/outzzz) | fork by Rudokhvist");
			return Task.CompletedTask;
		}

		public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) => await Commands.Response(bot, access, steamID, message, args).ConfigureAwait(false);

		public async Task OnBotInitModules(Bot bot, IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
			if (additionalConfigProperties == null) {
				return;
			}

			foreach (KeyValuePair<string, JsonElement> configProperty in additionalConfigProperties) {
				switch (configProperty.Key) {
					case "GamesToBooster" when configProperty.Value.ValueKind == JsonValueKind.Array && (configProperty.Value.GetArrayLength() > 0): {
						if (BoosterHandler.BoosterHandlers.TryGetValue(bot.BotName, out BoosterHandler? value) && (value != null)) {
								value!.Dispose();
							BoosterHandler.BoosterHandlers[bot.BotName] = null;
						}

						bot.ArchiLogger.LogGenericInfo("Games To Booster : " + string.Join(",", configProperty.Value));
							IReadOnlyCollection<uint>? gameIDs = new HashSet<uint>(configProperty.Value.EnumerateArray().Select(static elem => elem.GetUInt32()));
						if (gameIDs == null) {
							bot.ArchiLogger.LogNullError(gameIDs);
						} else {
							await Task.Run(() => BoosterHandler.BoosterHandlers[bot.BotName] = new BoosterHandler(bot, gameIDs)).ConfigureAwait(false);
						}
						break;
					}
				}
			}
		}
	}
}
