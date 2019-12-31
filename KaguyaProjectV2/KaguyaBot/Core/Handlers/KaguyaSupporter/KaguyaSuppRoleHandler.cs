﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord.WebSocket;
using KaguyaProjectV2.KaguyaBot.Core.Global;
using KaguyaProjectV2.KaguyaBot.Core.Services.ConsoleLogService;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using KaguyaProjectV2.KaguyaBot.DataStorage.JsonStorage;

namespace KaguyaProjectV2.KaguyaBot.Core.Handlers.KaguyaSupporter
{
    public static class KaguyaSuppRoleHandler
    {
        public static Task Start()
        {
            Timer timer = new Timer(1800000); //30 mins
            timer.Start();
            timer.AutoReset = true;
            timer.Elapsed += async (sender, e) => 
            {
                var client = ConfigProperties.client;
                var kaguyaSupportServer = client.GetGuild(546880579057221644); //Kaguya Support Discord Server

                var supporters = await UtilityQueries.GetAllActiveSupporterKeysAsync();

                foreach (var supporter in supporters)
                {
                    SocketUser socketUser = client.GetUser(supporter.UserId);

                    if (!socketUser.MutualGuilds.Contains(kaguyaSupportServer))
                        continue;

                    var supporterRole = kaguyaSupportServer.Roles.FirstOrDefault(x => x.Name.ToLower().Contains("supporter"));
                    var kaguyaSuppUser = kaguyaSupportServer.GetUser(socketUser.Id);

                    if (kaguyaSuppUser.Roles.Contains(supporterRole))
                        continue;

                    await kaguyaSuppUser.AddRoleAsync(supporterRole);
                    await ConsoleLogger.LogAsync($"Supporter {socketUser} has had their supporter role given to them in the Kaguya Support server.", LogLvl.INFO);
                }

                // Check for expired supporter tags.

                var expiredSupporters = await UtilityQueries.GetAllExpiredSupporterKeysAsync();

                foreach (var expSupporter in expiredSupporters)
                {
                    SocketUser socketUser = client.GetUser(expSupporter.UserId);
                    if (socketUser == null) continue;

                    if (!socketUser.MutualGuilds.Contains(kaguyaSupportServer))
                        continue;

                    var supporterRole = kaguyaSupportServer.Roles.FirstOrDefault(x => x.Name == "Supporter");
                    var kaguyaSuppUser = kaguyaSupportServer.GetUser(socketUser.Id);

                    if (!kaguyaSuppUser.Roles.Contains(supporterRole))
                        continue;

                    await kaguyaSuppUser.RemoveRoleAsync(supporterRole);
                    await ConsoleLogger.LogAsync($"Expired supporter {socketUser} has had their supporter role removed from them in the Kaguya Support server.", LogLvl.INFO);
                }
            };
            return Task.CompletedTask;
        }
    }
}
