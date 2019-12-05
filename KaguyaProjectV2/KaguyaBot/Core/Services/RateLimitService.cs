﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Humanizer;
using Humanizer.Localisation;
using KaguyaProjectV2.KaguyaBot.Core.DataStorage.JsonStorage;
using KaguyaProjectV2.KaguyaBot.Core.Global;
using KaguyaProjectV2.KaguyaBot.Core.KaguyaEmbed;
using KaguyaProjectV2.KaguyaBot.Core.Services.ConsoleLogService;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using NodaTime.Extensions;

namespace KaguyaProjectV2.KaguyaBot.Core.Services
{
    public static class RateLimitService
    {
        public static Task RateLimitHandler()
        {
            Timer timer = new Timer(3700); //3.70 seconds
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Elapsed += async (sender, e) =>
            {
                foreach (var registeredUser in UserQueries.GetAllUsers())
                {
                    if (registeredUser.ActiveRateLimit >= 2 && !registeredUser.IsSupporter || 
                        registeredUser.ActiveRateLimit >= 4 && registeredUser.IsSupporter)
                    {
                        registeredUser.RateLimitWarnings++;
                        if (registeredUser.RateLimitWarnings > 7)
                        {
                            var _user = ConfigProperties.client.GetUser(registeredUser.Id);
                            
                            var _embed = new KaguyaEmbedBuilder
                            {
                                Description = "You have exceeded your maximum allotment of ratelimit strikes, therefore " +
                                              "you will be permanently blacklisted."
                            };
                            _embed.SetColor(EmbedColor.RED);

                            await _user.SendMessageAsync(embed: _embed.Build());

                            registeredUser.BlacklistExpiration = DateTime.MaxValue.ToOADate();
                            UserQueries.UpdateUser(registeredUser);

                            await ConsoleLogger.Log($"User [Name: {_user.Username} | ID: {_user.Id} | Supporter: {registeredUser.IsSupporter}] " +
                                                    "has been permanently blacklisted. Reason: Excessive Ratelimiting", LogLevel.WARN);
                            return;
                        }

                        var user = ConfigProperties.client.GetUser(registeredUser.Id);

                        string[] durations = new[]
                        {
                            "60s", "5m", "30m",
                            "3h", "12h", "1d",
                            "3d"
                        };

                        List<TimeSpan> timeSpans = durations.Select(RegexTimeParser.ParseToTimespan).ToList();
                        string humanizedTime = timeSpans.ElementAt(registeredUser.RateLimitWarnings - 1).Humanize();

                        registeredUser.BlacklistExpiration =
                            DateTime.Now.Add(timeSpans.ElementAt(registeredUser.RateLimitWarnings - 1)).ToOADate();

                        var embed = new KaguyaEmbedBuilder
                        {
                            Description = $"You have been ratelimited for `{humanizedTime}`\n\n" +
                                          $"For this time, you may not use any commands or earn experience points.",
                            Footer = new EmbedFooterBuilder
                            {
                                Text = $"You have {registeredUser.RateLimitWarnings} ratelimit strikes. Receiving " +
                                       $"{durations.Length - registeredUser.RateLimitWarnings} more strikes will result " +
                                       $"in a permanent blacklist."
                            }
                        };
                        embed.SetColor(EmbedColor.RED);

                        await user.SendMessageAsync(embed: embed.Build());

                        await ConsoleLogger.Log($"User [Name: {user.Username} | ID: {user.Id} | Supporter: {registeredUser.IsSupporter}] " +
                                                $"has been ratelimited. Duration: {humanizedTime}", LogLevel.INFO);
                    }
                    registeredUser.ActiveRateLimit = 0;
                    UserQueries.UpdateUser(registeredUser);
                }
            };

            return Task.CompletedTask;
        }
    }
}
