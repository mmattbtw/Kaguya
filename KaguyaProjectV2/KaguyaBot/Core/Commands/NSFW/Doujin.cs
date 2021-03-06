﻿using Discord;
using Discord.Commands;
using Humanizer;
using KaguyaProjectV2.KaguyaBot.Core.Attributes;
using KaguyaProjectV2.KaguyaBot.Core.KaguyaEmbed;
using NHentaiSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHentaiSharp.Search;
using SearchResult = NHentaiSharp.Search.SearchResult;

namespace KaguyaProjectV2.KaguyaBot.Core.Commands.NSFW
{
    public class Doujin : KaguyaBase
    {
        [PremiumUserCommand]
        [NsfwCommand]
        [Command("Doujin")]
        [Summary("Displays a random doujin in chat.")]
        [Remarks("")]
        public async Task Command()
        {
            bool isBlacklisted = true;
            var wildcardBlacklist = new[]
            {
                "loli",
                "con",
                "shota",
                "rape"
            };

            string[] tags = new[]
            {
                SearchClient.GetExcludeTag("rape"),
                SearchClient.GetExcludeTag("loli"),
                SearchClient.GetExcludeTag("lolicon"),
                SearchClient.GetExcludeTag("furry"),
                SearchClient.GetExcludeTag("vore"),
                SearchClient.GetExcludeTag("gore"),
                SearchClient.GetExcludeTag("shotacon")
            };

            while (isBlacklisted)
            {
                var r = new Random();

                SearchResult result = await SearchClient.SearchWithTagsAsync(tags.ToArray());
                int page = r.Next(0, result.numPages) + 1; // Page count begins at 1.

                result = await SearchClient.SearchWithTagsAsync(tags.ToArray(), page);
                GalleryElement selection = result.elements[r.Next(0, result.elements.Length)];

                string tagString = selection.tags.Aggregate("", (current, tag) => current + $"`{tag.name}`, ");
                tagString = tagString.Substring(0, tagString.Length - 2);

                if (wildcardBlacklist.Any(tagString.Contains))
                    continue;

                isBlacklisted = false;

                var embed = new KaguyaEmbedBuilder
                {
                    Title = $"{selection.englishTitle}",
                    Description = $"[[Source]]({selection.url})",
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder
                        {
                            Name = "Favorites",
                            Value = selection.numFavorites.ToString("N0")
                        },
                        new EmbedFieldBuilder
                        {
                            Name = "Date Uploaded",
                            Value = selection.uploadDate.Humanize()
                        },
                        new EmbedFieldBuilder
                        {
                            Name = "Pages",
                            Value = selection.numPages
                        },
                        new EmbedFieldBuilder
                        {
                            Name = "Tags",
                            Value = tagString
                        }
                    },
                    ImageUrl = selection.thumbnail.imageUrl.ToString()
                };

                await ReplyAsync(embed: embed.Build());
            }
        }
    }
}