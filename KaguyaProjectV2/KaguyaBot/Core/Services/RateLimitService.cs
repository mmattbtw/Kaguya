﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using KaguyaProjectV2.KaguyaBot.Core.Global;
using KaguyaProjectV2.KaguyaBot.Core.KaguyaEmbed;
using KaguyaProjectV2.KaguyaBot.Core.Services.ConsoleLogServices;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using KaguyaProjectV2.KaguyaBot.DataStorage.JsonStorage;

namespace KaguyaProjectV2.KaguyaBot.Core.Services
{
    public static class RateLimitService
    {
        private const int DURATION_MS = 4250; // 4.25 seconds
        private const int THRESHOLD_REG = 3;
        private const int THRESHOLD_PREMIUM = 5;

        public static Task Initialize()
        {
            var timer = new Timer(DURATION_MS);
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Elapsed += async (sender, e) =>
            {
                List<User> users = await DatabaseQueries.GetAllAsync<User>(x => x.ActiveRateLimit > 0 && x.UserId != 146092837723832320);

                foreach (User registeredUser in users)
                {
                    if (registeredUser.LastRatelimited < DateTime.Now.Add(TimeSpan.FromDays(-30)).ToOADate() &&
                        registeredUser.RateLimitWarnings > 0)
                    {
                        registeredUser.RateLimitWarnings = 0;
                        await ConsoleLogger.LogAsync($"User [ID: {registeredUser.UserId}] has had their Ratelimit Warnings reset " +
                                                     $"due to not being ratelimited for 30 days.", LogLvl.INFO);
                    }

                    // The user has been rate limited within the last 30 seconds.
                    // Don't accidentally double-rate-limit them.
                    if (registeredUser.LastRatelimited > DateTime.Now.AddSeconds(-30).ToOADate() &&
                        registeredUser.RateLimitWarnings > 0)
                        return;

                    if (registeredUser.ActiveRateLimit >= THRESHOLD_REG && !registeredUser.IsPremium ||
                        registeredUser.ActiveRateLimit >= THRESHOLD_PREMIUM && registeredUser.IsPremium)
                    {
                        registeredUser.LastRatelimited = DateTime.Now.ToOADate();
                        registeredUser.RateLimitWarnings++;
                        if (registeredUser.RateLimitWarnings > 7 && registeredUser.ActiveRateLimit > 0)
                        {
                            SocketUser socketUser = ConfigProperties.Client.GetUser(registeredUser.UserId);

                            var maxRlimitEmbed = new KaguyaEmbedBuilder(EmbedColor.RED)
                            {
                                Description = "You have exceeded your maximum allotment of ratelimit strikes, therefore " +
                                              "you will be permanently blacklisted."
                            };

                            try
                            {
                                await socketUser.SendMessageAsync(embed: maxRlimitEmbed.Build());
                            }
                            catch (HttpException)
                            {
                                await ConsoleLogger.LogAsync($"Attempted to DM user {socketUser.Id} about " +
                                                             $"acheiving the maximum allotted ratelimit strikes, " +
                                                             $"but a Discord.Net.HttpException was thrown.", LogLvl.WARN);
                            }

                            var bl = new UserBlacklist
                            {
                                UserId = socketUser.Id,
                                Expiration = DateTime.MaxValue.ToOADate(),
                                Reason = "Ratelimit service: Automatic permanent blacklist for surpassing " +
                                         "7 ratelimit strikes in one month.",
                                User = registeredUser
                            };

                            registeredUser.ActiveRateLimit = 0;
                            await DatabaseQueries.UpdateAsync(registeredUser);
                            await DatabaseQueries.InsertOrReplaceAsync(bl);

                            await ConsoleLogger.LogAsync(
                                $"User [Name: {socketUser.Username} | ID: {socketUser.Id} | Supporter: {registeredUser.IsPremium}] " +
                                "has been permanently blacklisted. Reason: Excessive Ratelimiting", LogLvl.WARN);

                            return;
                        }

                        SocketUser user = ConfigProperties.Client.GetUser(registeredUser.UserId);

                        if (user == null)
                            return;

                        string[] durations =
                        {
                            "60s",
                            "5m",
                            "30m",
                            "3h",
                            "12h",
                            "1d",
                            "3d"
                        };

                        List<TimeSpan> timeSpans = durations.Select(RegexTimeParser.ParseToTimespan).ToList();
                        string humanizedTime = timeSpans.ElementAt(registeredUser.RateLimitWarnings - 1).Humanize();

                        var tempBlacklist = new UserBlacklist
                        {
                            UserId = user.Id,
                            Expiration = (DateTime.Now + timeSpans.ElementAt(registeredUser.RateLimitWarnings - 1)).ToOADate(),
                            Reason = $"Ratelimit service: Automatic {timeSpans.ElementAt(registeredUser.RateLimitWarnings - 1)} " +
                                     $"temporary blacklist for surpassing a ratelimit strike",
                            User = registeredUser
                        };

                        await DatabaseQueries.InsertOrReplaceAsync(tempBlacklist);

                        var curRlimEmbed = new KaguyaEmbedBuilder
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

                        curRlimEmbed.SetColor(EmbedColor.RED);

                        bool dm = true;

                        try
                        {
                            await user.SendMessageAsync(embed: curRlimEmbed.Build());
                        }
                        catch (Exception)
                        {
                            dm = false;
                        }

                        await ConsoleLogger.LogAsync($"User [Name: {user?.Username} | ID: {user?.Id} | Supporter: {registeredUser.IsPremium}] " +
                                                     $"has been ratelimited. Duration: {humanizedTime} Direct Message Sent: {dm}", LogLvl.INFO);
                    }

                    if (registeredUser.ActiveRateLimit > 0)
                    {
                        registeredUser.ActiveRateLimit = 0;
                        await DatabaseQueries.UpdateAsync(registeredUser);
                    }
                }
            };

            return Task.CompletedTask;
        }
    }
}