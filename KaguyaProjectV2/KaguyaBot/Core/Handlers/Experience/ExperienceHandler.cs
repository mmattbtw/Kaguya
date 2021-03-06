﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using KaguyaProjectV2.KaguyaBot.Core.Commands.EXP;
using KaguyaProjectV2.KaguyaBot.Core.Global;
using KaguyaProjectV2.KaguyaBot.Core.Images.ExpLevelUp;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using KaguyaProjectV2.KaguyaBot.DataStorage.JsonStorage;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KaguyaProjectV2.KaguyaBot.Core.Services.ConsoleLogServices;

namespace KaguyaProjectV2.KaguyaBot.Core.Handlers.Experience
{
    public static class ExperienceHandler
    {
        public static async Task TryAddExp(User user, Server server, ICommandContext context)
        {
            // If the user can receive exp, give them between 5 and 8.
            if (!CanGetExperience(user))
                return;

            // Don't give exp to any user who is blacklisted.
            // Members in blacklisted servers also cannot earn exp.
            if (user.IsBlacklisted || server.IsBlacklisted)
                return;

            SocketTextChannel levelAnnouncementChannel = null;
            if (server.LogLevelAnnouncements != 0)
            {
                levelAnnouncementChannel = await context.Guild.GetTextChannelAsync(server.LogLevelAnnouncements) as SocketTextChannel;
            }

            if (levelAnnouncementChannel == null)
                levelAnnouncementChannel = (SocketTextChannel) context.Channel;

            double oldLevel = ReturnLevel(user);

            var r = new Random();
            int exp = r.Next(5, 8);
            int points = r.Next(1, 4);

            user.Experience += exp;
            user.Points += points;
            user.LastGivenExp = DateTime.Now.ToOADate();
            await DatabaseQueries.UpdateAsync(user);

            double newLevel = ReturnLevel(user);
            await ConsoleLogger.LogAsync($"[Global Exp]: User {user.UserId} has received {exp} exp and {points} points. " +
                                         $"[New Total: {user.Experience:N0} Exp]", LogLvl.DEBUG);

            if (!HasLeveledUp(oldLevel, newLevel))
                return;

            await ConsoleLogger.LogAsync($"[Global Exp]: User {user.UserId} has leveled up! " +
                                         $"[Level: {Math.Floor(newLevel):0} | EXP: {user.Experience:N0}]", LogLvl.INFO);

            // Don't send announcement if the channel is blacklisted, but only if it's not a level-announcements log channel.
            if (server.BlackListedChannels.Any(x => x.ChannelId == context.Channel.Id && x.ChannelId != server.LogLevelAnnouncements))
                return;

            if (!server.LevelAnnouncementsEnabled)
                return;

            var xp = new XpImage();
            if (user.ExpChatNotificationType == ExpType.GLOBAL || user.ExpChatNotificationType == ExpType.BOTH)
            {
                if (levelAnnouncementChannel != null)
                {
                    Stream xpStream = await xp.GenerateXpImageStream(user, (SocketGuildUser) context.User);
                    await levelAnnouncementChannel.SendFileAsync(xpStream, $"Kaguya_Xp_LevelUp.png", "");
                }
            }

            if (user.ExpDmNotificationType == ExpType.GLOBAL || user.ExpDmNotificationType == ExpType.BOTH)
            {
                Stream xpStream = await xp.GenerateXpImageStream(user, (SocketGuildUser) context.User);
                await context.User.SendFileAsync(xpStream, $"Kaguya_Xp_LevelUp.png", "");
            }
        }

        private static bool CanGetExperience(User user)
        {
            double twoMinutesAgo = DateTime.Now.AddSeconds(-120).ToOADate();

            return twoMinutesAgo >= user.LastGivenExp;
        }

        private static double ReturnLevel(User user) => GlobalProperties.CalculateLevelFromExp(user.Experience);
        private static bool HasLeveledUp(double oldLevel, double newLevel) => Math.Floor(oldLevel) < Math.Floor(newLevel);
    }
}