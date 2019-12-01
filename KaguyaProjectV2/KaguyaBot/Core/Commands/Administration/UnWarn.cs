﻿using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using KaguyaProjectV2.KaguyaBot.Core.Attributes;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KaguyaProjectV2.KaguyaBot.Core.KaguyaEmbed;

namespace KaguyaProjectV2.KaguyaBot.Core.Commands.Administration
{
    public class UnWarn : InteractiveBase<ShardedCommandContext>
    {
        [AdminCommand]
        [Command("unwarn")]
        [Alias("uw")]
        [Summary("Removes a warning from a user. A list of the user's 4 most recent warnings (9 if server is premium) will be displayed in chat. The " +
                 "moderator executing this command may then choose which warnings to remove by clicking on the supplied reactions.")]
        [Remarks("<user>")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task UnWarnUser(IGuildUser user)
        {
            var server = ServerQueries.GetServer(Context.Guild.Id);
            var warnings = ServerQueries.GetWarnedUser(Context.Guild.Id, user.Id);
            var fields = new List<EmbedFieldBuilder>();

            int warnCount = warnings.Count;

            if (warnCount > 4 && !server.IsPremium)
                warnCount = 4;
            if (warnCount > 9 && server.IsPremium)
                warnCount = 9;

            if (warnings.Count == 0)
            {
                var reply = new KaguyaEmbedBuilder
                {
                    Description = $"{user.Username} has no warnings to remove!"
                };
                reply.SetColor(EmbedColor.RED);

                await ReplyAsync(embed: reply.Build());
                return;
            }

            for (int i = 0; i < warnCount; i++)
            {
                var field = new EmbedFieldBuilder
                {
                    Name = $"Warning #{i + 1}",
                    Value = $"Reason: `{warnings.ElementAt(i).Reason}`"
                };

                fields.Add(field);
            }

            var embed = new KaguyaEmbedBuilder
            {
                Title = $"Warnings for {user}",
                Fields = fields,
                Footer = new EmbedFooterBuilder
                {
                    Text = "Select a reaction to remove the warning."
                }
            };

            await ReactionReply(warnings, embed.Build(), warnCount);
        }

        private async Task ReactionReply(List<WarnedUser> warnings, Embed embed, int warnCount)
        {
            var emojis = new Emoji[] { new Emoji("1⃣"), new Emoji("2⃣"), new Emoji("3⃣"),
                new Emoji("4⃣"),  new Emoji("5⃣"),  new Emoji("6⃣"),  new Emoji("7⃣"),
                new Emoji("8⃣"),  new Emoji("9⃣")
            };

            var data = new ReactionCallbackData("", embed, false, false, TimeSpan.FromSeconds(300), c => 
                c.Channel.SendMessageAsync(embed: TimeoutEmbed()));
            var callbacks = new List<(IEmote, Func<SocketCommandContext, SocketReaction, Task>)>();

            for (int j = 0; j < warnCount; j++)
            {
                int j1 = j;
                callbacks.Add((emojis[j], (c, r) =>
                {
                    ServerQueries.RemoveWarnedUser(warnings.ElementAt(j1));
                    return c.Channel.SendMessageAsync($"{r.User.Value.Mention} `Successfully removed warning #{j1 + 1}`");
                }));
            }

            data.SetCallbacks(callbacks);
            await InlineReactionReplyAsync(data);
        }

        private static Embed TimeoutEmbed()
        {
            KaguyaEmbedBuilder timeoutEmbed = new KaguyaEmbedBuilder
            {
                Description = "Warn remove has timed out. (5 minutes)"
            };

            timeoutEmbed.SetColor(EmbedColor.RED);
            return timeoutEmbed.Build();
        }
    }
}