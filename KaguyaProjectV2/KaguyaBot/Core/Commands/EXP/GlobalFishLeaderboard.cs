using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using KaguyaProjectV2.KaguyaBot.Core.Attributes;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using System.Threading.Tasks;
using Discord.WebSocket;
using KaguyaProjectV2.KaguyaBot.Core.KaguyaEmbed;
using KaguyaProjectV2.KaguyaBot.Core.Extensions;
using KaguyaProjectV2.KaguyaBot.Core.Global;

namespace KaguyaProjectV2.KaguyaBot.Core.Commands.EXP
{
    public class GlobalFishLeaderboard : KaguyaBase
    {
        [ExpCommand]
        [Command("FishLeaderboard")]
        [Alias("flb")]
        [Summary("Allows you to see the leaderboard of Kaguya's top fishermen!")]
        [Remarks("")]
        public async Task Command()
        {
            List<User> players = await DatabaseQueries.GetLimitAsync<User>(10, x => x.FishExp > 0, x => x.FishExp, true);
            DiscordShardedClient client = ConfigProperties.Client;
            var embed = new KaguyaEmbedBuilder();
            embed.Title = "Kaguya Fishing Leaderboard";

            int i = 0;
            foreach (User player in players)
            {
                i++;
                SocketUser socketUser = client.GetUser(player.UserId);
                List<Fish> fish = await DatabaseQueries.GetAllForUserAsync<Fish>(player.UserId,
                    x => x.FishType != FishType.BAIT_STOLEN);

                embed.Fields.Add(new EmbedFieldBuilder
                {
                    Name = $"{i}. {socketUser?.ToString().Split('#').First() ?? $"[Unknown User: {player.UserId}]"}",
                    Value = $"Fish Level: `{player.FishLevel():0}` | Fish Exp: `{player.FishExp:N0}` | " +
                            $"Fish Caught: `{fish.Count:N0}`"
                });
            }

            await SendEmbedAsync(embed);
        }
    }
}