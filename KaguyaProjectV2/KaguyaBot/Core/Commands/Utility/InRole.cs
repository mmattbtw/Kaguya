﻿using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using KaguyaProjectV2.KaguyaBot.Core.Attributes;
using KaguyaProjectV2.KaguyaBot.Core.KaguyaEmbed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaguyaProjectV2.KaguyaBot.Core.Commands.Utility
{
    public class InRole : KaguyaBase
    {
        [UtilityCommand]
        [Command("InRole")]
        [Alias("ir")]
        [Summary("Displays an alphabetized list of who has the specified role.")]
        [Remarks("<role>")]
        public async Task Find([Remainder] string roleName)
        {
            SocketGuild guild = Context.Guild;
            IReadOnlyCollection<SocketRole> roles = guild.Roles;

            if (roles.All(x => x.Name.ToLower() != roleName.ToLower()))
            {
                var embed = new KaguyaEmbedBuilder
                {
                    Description = $"The role `{roleName.ToUpper()}` could not be found."
                };

                embed.SetColor(EmbedColor.RED);

                await ReplyAsync(embed: embed.Build());

                return;
            }

            int matchCount = roles.Count(x => x.Name.ToLower() == roleName.ToLower());

            if (matchCount > 1)
            {
                await MultipleMatchingRolesHandler(guild, roleName, roles);

                return;
            }

            SocketRole matchingRole = guild.Roles.FirstOrDefault(x => x.Name.ToLower() == roleName.ToLower());

            List<PaginatedMessage.Page> pages = Pages(guild, matchingRole);
            var pager = new PaginatedMessage
            {
                Pages = pages,
                Color = Color.Blue
            };

            await PagedReplyAsync(pager, new ReactionList
            {
                Backward = true,
                First = true,
                Forward = true,
                Jump = true,
                Last = true,
                Trash = true
            });
        }

        private async Task MultipleMatchingRolesHandler(SocketGuild guild, string roleName, IReadOnlyCollection<SocketRole> roles)
        {
            List<SocketRole> matchingRoles = roles.Where(x => x.Name.ToLower() == roleName.ToLower()).ToList();
            int matchCount = matchingRoles.Count;

            if (matchCount > 9)
                matchCount = 9;

            var emojis = new Emoji[]
            {
                new Emoji("1⃣"),
                new Emoji("2⃣"),
                new Emoji("3⃣"),
                new Emoji("4⃣"),
                new Emoji("5⃣"),
                new Emoji("6⃣"),
                new Emoji("7⃣"),
                new Emoji("8⃣"),
                new Emoji("9⃣")
            };

            var embed = new KaguyaEmbedBuilder
            {
                Description = $"I found `{matchCount.ToWords()}` roles that match this name. Please " +
                              $"select the role that you want to display.",
                Fields = new List<EmbedFieldBuilder>()
            };

            var callbacks = new List<(IEmote, Func<SocketCommandContext, SocketReaction, Task>)>();

            for (int i = 0; i < matchCount; i++)
            {
                int i1 = i;
                if (i1 == matchCount)
                    i1 = matchCount - 1;

                SocketRole role = matchingRoles.ElementAt(i1);
                List<GuildPermission> rolePerms = matchingRoles[i].Permissions.ToList();
                IEnumerable<SocketGuildUser> usersWithRole = guild.Users.Where(x => x.Roles.Contains(role));

                embed.Fields.Add(new EmbedFieldBuilder
                {
                    Name = $"Role #{i + 1}",
                    Value = $"Exact Name: `{role.Name}`\nNumber of users who have the role: {usersWithRole.Count()}" +
                            $"\nPermissions: `{rolePerms.Count}`\n" +
                            $"Created: `{role.CreatedAt.Humanize()}`\n" +
                            $"Position in role list (higher number = higher position): `{role.Position}`"
                });

                callbacks.Add((emojis[i], async (c, r) =>
                        {
                            var pager = new PaginatedMessage
                            {
                                Pages = Pages(guild, role),
                                Color = Color.Blue
                            };

                            await PagedReplyAsync(pager, new ReactionList
                            {
                                Backward = true,
                                First = true,
                                Forward = true,
                                Jump = true,
                                Last = true,
                                Trash = true
                            });
                        }
                    ));
            }

            var data = new ReactionCallbackData("", embed.Build(), false, false, TimeSpan.FromSeconds(120),
                c => c.Channel.SendMessageAsync("Role selection has timed out. Please try again."));

            data.SetCallbacks(callbacks);
            await InlineReactionReplyAsync(data);
        }

        private List<PaginatedMessage.Page> Pages(SocketGuild guild, IRole role)
        {
            IEnumerable<SocketGuildUser> usersWithRole = guild.Users.Where(x => x.Roles.Contains(role));
            List<SocketGuildUser> usersList = usersWithRole.OrderBy(x => x.Username).ToList();
            int count = usersList.Count;

            if (count == 0)
                count = 1;

            int totalPageCount = (count + 24) / 25;

            var pages = new List<PaginatedMessage.Page>();

            for (int i = 0; i < totalPageCount; i++)
                pages.Add(new PaginatedMessage.Page());

            for (int i = 0; i < count; i++)
            {
                int pageCount = (i + 24) / 25;

                if (pageCount == 0)
                    pageCount = 1;

                PaginatedMessage.Page currentPage = pages.ElementAt(pageCount - 1);

                currentPage.Title = $"All users with role {role.Name}";

                if (usersList.Count == 0)
                {
                    currentPage.Description = "`No users have the role.`";

                    break;
                }

                currentPage.Description += $"{usersList.ElementAt(i).Mention}\n";
            }

            return pages;
        }
    }
}