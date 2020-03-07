﻿using Discord;
using Discord.Commands;
using KaguyaProjectV2.KaguyaBot.Core.Attributes;
using KaguyaProjectV2.KaguyaBot.Core.KaguyaEmbed;
using KaguyaProjectV2.KaguyaBot.Core.Services.ConsoleLogService;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using System.Linq;
using System.Threading.Tasks;

namespace KaguyaProjectV2.KaguyaBot.Core.Commands.Administration
{
    public class DeleteAsync : KaguyaBase
    {
        [AdminCommand]
        [Command("FilterRemove")]
        [Alias("fr")]
        [Summary("Removes one phrase or a list of filtered phrases from your server's word filter. Phrases are separated by spaces. " +
                 "If a phrase is longer than one word, surround it with `\"\"`.")]
        [Remarks("<phrase> {...}\nMyPhrase \"My phrase with spaces\"")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task RemovePhrase(params string[] args)
        {
            string s = "s";
            if (args.Length == 1) s = "";

            var server = await DatabaseQueries.GetOrCreateServerAsync(Context.Guild.Id);
            var allFp = server.FilteredPhrases.ToList();

            if (args.Length == 0)
            {
                var embed0 = new KaguyaEmbedBuilder
                {
                    Description = "Please specify at least one phrase."
                };
                embed0.SetColor(EmbedColor.RED);

                await Context.Channel.SendMessageAsync(embed: embed0.Build());
                return;
            }

            foreach (string element in args)
            {
                var fp = new FilteredPhrase
                {
                    ServerId = server.ServerId,
                    Phrase = element
                };

                if (!allFp.Contains(fp)) continue;

                await DatabaseQueries.DeleteAsync(fp);
                await ConsoleLogger.LogAsync($"Server {server.ServerId} has removed the phrase \"{element}\" from their word filter.", DataStorage.JsonStorage.LogLvl.DEBUG);
            }

            var embed = new KaguyaEmbedBuilder
            {
                Description = $"Successfully removed {args.Length} phrase{s} from the filter."
            };
            embed.SetColor(EmbedColor.VIOLET);

            await ReplyAsync(embed: embed.Build());
        }
    }
}