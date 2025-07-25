using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using AngleSharp.Dom;
using ArchiSteamFarm.Helpers.Json;
using System.Collections.ObjectModel;

namespace BoosterCreator;

internal sealed class BoosterHandler : IDisposable {
	private readonly Bot Bot;
	private readonly ConcurrentDictionary<uint, DateTime?> GameIDs = new();
	private readonly Timer BoosterTimer;

	internal static ConcurrentDictionary<string, BoosterHandler?> BoosterHandlers = new();

	internal const int DelayBetweenBots = 5; //5 minutes between bots

	internal static int GetBotIndex(Bot bot) {
		//this can be pretty slow and memory-consuming on lage bot farm. Luckily, I don't care about cases with >10 bots
		List<string> botnames = [.. BoosterHandlers.Keys];
		botnames.Sort();
		int index = botnames.IndexOf(bot.BotName);

		return 1 + (index >= 0 ? index : botnames.Count);
	}

	internal BoosterHandler(Bot bot, IReadOnlyCollection<uint> gameIDs) {
		Bot = bot ?? throw new ArgumentNullException(nameof(bot));

		foreach (uint gameID in gameIDs) {
			if (GameIDs.TryAdd(gameID, DateTime.Now.AddMinutes(GetBotIndex(bot) * DelayBetweenBots))) {
				bot.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, $"Auto-attempt to make booster from {gameID} is planned at {GameIDs[gameID]!.Value.ToShortDateString()} {GameIDs[gameID]!.Value.ToShortTimeString()}"));
			} else {
				bot.ArchiLogger.LogGenericError("Unable to schedule next auto-attempt");
			}
		}

		BoosterTimer = new Timer(
			async void (_) => await AutoBooster().ConfigureAwait(false),
			null,
			TimeSpan.FromMinutes(GetBotIndex(bot) * DelayBetweenBots),
			TimeSpan.FromMinutes(GetBotIndex(bot) * DelayBetweenBots)
		);
	}

	public void Dispose() => BoosterTimer.Dispose();

	private async Task AutoBooster() {
		if (!Bot.IsConnectedAndLoggedOn) {
			return;
		}

		await CreateBooster(Bot, GameIDs).ConfigureAwait(false);
	}

	internal static async Task<string?> CreateBooster(Bot bot, ConcurrentDictionary<uint, DateTime?> gameIDs) {
		if (gameIDs.IsEmpty) {
			bot.ArchiLogger.LogNullError(null, nameof(gameIDs));

			return null;
		}

		IDocument? boosterPage = await WebRequest.GetBoosterPage(bot).ConfigureAwait(false);

		if (boosterPage == null) {
			bot.ArchiLogger.LogNullError(boosterPage);

			return Commands.FormatBotResponse(bot, string.Format(CultureInfo.CurrentCulture, Strings.ErrorFailingRequest, nameof(boosterPage)));
		}

		MatchCollection gooAmounts = Regex.Matches(boosterPage.Source.Text, "(?<=parseFloat\\( \")[0-9]+");
		Match info = Regex.Match(boosterPage.Source.Text, "\\[\\{\"[\\s\\S]*\"}]");

		if (!info.Success || (gooAmounts.Count != 3)) {
			bot.ArchiLogger.LogGenericError(string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, boosterPage));

			return Commands.FormatBotResponse(bot, string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, boosterPage));
		}

		uint gooAmount = uint.Parse(gooAmounts[0].Value, CultureInfo.InvariantCulture);
		uint tradableGooAmount = uint.Parse(gooAmounts[1].Value, CultureInfo.InvariantCulture);
		uint unTradableGooAmount = uint.Parse(gooAmounts[2].Value, CultureInfo.InvariantCulture);

		IEnumerable<Steam.BoosterInfo>? enumerableBoosters = info.Value.ToJsonObject<Collection<Steam.BoosterInfo>>();

		if (enumerableBoosters == null) {
			bot.ArchiLogger.LogNullError(enumerableBoosters);

			return Commands.FormatBotResponse(bot, string.Format(CultureInfo.CurrentCulture, Strings.ErrorParsingObject, nameof(enumerableBoosters)));
		}

		Dictionary<uint, Steam.BoosterInfo> boosterInfos = enumerableBoosters.ToDictionary(static boosterInfo => boosterInfo.AppID);
		StringBuilder response = new();

		foreach (KeyValuePair<uint, DateTime?> gameID in gameIDs) {
			if (!gameID.Value.HasValue || (DateTime.Compare(gameID.Value.Value, DateTime.Now) <= 0)) {
				await Task.Delay(500).ConfigureAwait(false);

				if (!boosterInfos.TryGetValue(gameID.Key, out Steam.BoosterInfo? bi)) {
					response.AppendLine(Commands.FormatBotResponse(bot, "Not eligible to create boosters from " + gameID.Key));
					bot.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Not eligible to create boosters from " + gameID.Key));

					//If we are not eligible - wait 8 hours, just in case game will be added to account later
					if (gameID.Value.HasValue) {
						//if source is timer, not command
						gameIDs[gameID.Key] = DateTime.Now.AddHours(8);
						bot.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Next attempt to make booster from " + gameID.Key + " is planned at " + gameIDs[gameID.Key]!.Value.ToShortDateString() + " " + gameIDs[gameID.Key]!.Value.ToShortTimeString()));
					}

					continue;
				}

				if (!uint.TryParse(bi.Price, out uint gemPrice)) {
					response.AppendLine(Commands.FormatBotResponse(bot, "Failed to create booster from " + gameID.Key));

					continue;
				}

				if (gooAmount < gemPrice) {
					response.AppendLine(Commands.FormatBotResponse(bot, "Not enough gems to create booster from " + gameID.Key));

					//If we have not enough gems - wait 8 hours, just in case gems will be added to account later
					bot.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Not enough gems to create booster from " + gameID.Key));

					if (gameID.Value.HasValue) {
						//if source is timer, not command
						gameIDs[gameID.Key] = DateTime.Now.AddHours(8);
						bot.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Next attempt to make booster from " + gameID.Key + " is planned at " + gameIDs[gameID.Key]!.Value.ToShortDateString() + " " + gameIDs[gameID.Key]!.Value.ToShortTimeString()));
					}

					continue;
				}

				if (bi.Unavailable) {
					//God, I hate this shit. But for now I have no idea how to predict/enforce correct format.

					List<string> timeFormats = [
						"d MMM @ h:mmtt",
						"MMM d @ h:mmtt",
						"d MMM, yyyy @ h:mmtt",
						"MMM d, yyyy @ h:mmtt",
					];

					DateTime availableAtTime = DateTime.MinValue;

					foreach (string timeFormat in timeFormats) {
						if (DateTime.TryParseExact(bi.AvailableAtTime, timeFormat, new CultureInfo("en-US"), DateTimeStyles.None, out availableAtTime)) {
							break;
						}
					}

					if (availableAtTime == DateTime.MinValue) {
						bot.ArchiLogger.LogGenericInfo("Unable to parse time \"" + bi.AvailableAtTime + "\", please report this.");
						availableAtTime = DateTime.Now.AddHours(8); //fallback to 8 hours in case of error
					}

					response.AppendLine(Commands.FormatBotResponse(bot, "Crafting booster from " + gameID.Key + " will be available at time: " + bi.AvailableAtTime));
					bot.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Crafting booster from " + gameID.Key + " is not available now"));

					if (gameID.Value.HasValue) {
						//if source is timer, not command
						gameIDs[gameID.Key] = availableAtTime; //convertedTime;
						bot.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Next attempt to make booster from " + gameID.Key + " is planned at " + gameIDs[gameID.Key]!.Value.ToShortDateString() + " " + gameIDs[gameID.Key]!.Value.ToShortTimeString()));
					}

					continue;
				}

				uint nTp;

				if (unTradableGooAmount > 0) {
					nTp = tradableGooAmount > gemPrice ? (uint) 1 : 3;
				} else {
					nTp = 2;
				}

				Steam.BoostersResponse? result = await WebRequest.CreateBooster(bot, bi.AppID, bi.Series, nTp).ConfigureAwait(false);

				if (result?.Result.Result != EResult.OK) {
					response.AppendLine(Commands.FormatBotResponse(bot, "Failed to create booster from " + gameID.Key));
					bot.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Failed to create booster from " + gameID.Key));

					//Some unhandled error - wait 8 hours before retry
					if (gameID.Value.HasValue) {
						//if source is timer, not command
						gameIDs[gameID.Key] = DateTime.Now.AddHours(8);
						bot.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Next attempt to make booster from " + gameID.Key + " is planned at " + gameIDs[gameID.Key]!.Value.ToShortDateString() + " " + gameIDs[gameID.Key]!.Value.ToShortTimeString()));
					}

					continue;
				}

				gooAmount = uint.Parse(result.GooAmount!, CultureInfo.InvariantCulture);
				tradableGooAmount = uint.Parse(result.TradableGooAmount!, CultureInfo.InvariantCulture);
				unTradableGooAmount = result.UntradableGooAmount;
				response.AppendLine(Commands.FormatBotResponse(bot, "Successfully created booster from " + gameID.Key));
				bot.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Successfully created booster from " + gameID.Key));

				//Buster was made - next is only available in 24 hours
				if (gameID.Value.HasValue) {
					//if source is timer, not command
					gameIDs[gameID.Key] = DateTime.Now.AddHours(24);
					bot.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Next attempt to make booster from " + gameID.Key + " is planned at " + gameIDs[gameID.Key]!.Value.ToShortDateString() + " " + gameIDs[gameID.Key]!.Value.ToShortTimeString()));
				}
			}
		}

		//Get nearest time when we should try for new booster;
		DateTime? nextTry = gameIDs.Values.Min();

		if (nextTry.HasValue) {
			//if it was not from command
			if (BoosterHandlers[bot.BotName] != null) {
				BoosterHandlers[bot.BotName]!.BoosterTimer.Change((nextTry.Value - DateTime.Now) + TimeSpan.FromMinutes(GetBotIndex(bot) * DelayBetweenBots), (nextTry.Value - DateTime.Now) + TimeSpan.FromMinutes(GetBotIndex(bot) * DelayBetweenBots));
			}
		}

		return response.Length > 0 ? response.ToString() : null;
	}
}
