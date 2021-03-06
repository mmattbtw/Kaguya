﻿using Discord;
using Discord.Commands;
using KaguyaProjectV2.KaguyaBot.Core.Attributes;
using KaguyaProjectV2.KaguyaBot.Core.Global;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using System.Threading.Tasks;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models;
using Victoria;
using Victoria.Enums;

namespace KaguyaProjectV2.KaguyaBot.Core.Commands.Music
{
    public class Pause : KaguyaBase
    {
        [MusicCommand]
        [Command("Pause")]
        [Summary("Pauses the music player if it is playing.")]
        [Remarks("")]
        [RequireUserPermission(GuildPermission.Connect)]
        [RequireBotPermission(GuildPermission.Connect)]
        [RequireContext(ContextType.Guild)]
        public async Task Command()
        {
            Server server = await DatabaseQueries.GetOrCreateServerAsync(Context.Guild.Id);
            LavaNode node = ConfigProperties.LavaNode;
            LavaPlayer player = node.GetPlayer(Context.Guild);

            if (player == null)
            {
                await SendBasicErrorEmbedAsync($"There needs to be an active music player in the " +
                                               $"server for this command to work. Start one " +
                                               $"by using `{server.CommandPrefix}play <song>`!");

                return;
            }

            if (player.PlayerState == PlayerState.Playing)
            {
                await player.PauseAsync();
                await SendBasicSuccessEmbedAsync($"Successfully paused the player.");
            }
            else
            {
                await SendBasicErrorEmbedAsync($"There is no song currently playing, therefore I have nothing to pause. If " +
                                               $"you have previously used the `{server.CommandPrefix}pause` command, " +
                                               $"use the `{server.CommandPrefix}resume` command to resume the player.");
            }
        }
    }
}