﻿using Discord;
using Discord.Commands;
using KaguyaProjectV2.KaguyaBot.Core.Attributes;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using System.Threading.Tasks;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models;

namespace KaguyaProjectV2.KaguyaBot.Core.Commands.Utility
{
    public class ToggleLevels : KaguyaBase
    {
        [UtilityCommand]
        [Command("ToggleLevels")]
        [Alias("tl")]
        [Summary("Allows a server administrator to turn this server's level-up notifications " +
                 "on or off. This will override any logging preference for these notifications, assuming " +
                 "a channel is currently set for that logtype. This means that if a level-up redirect channel " +
                 "has been set via the `log` command, and this server's level notifications are disabled via " +
                 "this command, no notifications will be sent anywhere.")]
        [Remarks("")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task Command()
        {
            Server server = await DatabaseQueries.GetOrCreateServerAsync(Context.Guild.Id);
            server.LevelAnnouncementsEnabled = !server.LevelAnnouncementsEnabled;

            await DatabaseQueries.UpdateAsync(server);

            string toggleName = server.LevelAnnouncementsEnabled switch
            {
                true => "enabled",
                false => "disabled"
            };

            await SendBasicSuccessEmbedAsync($"Successfully {toggleName} this server's level-up notifications.");
        }
    }
}