﻿using System;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Context;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Models;
using LinqToDB;
using LinqToDB.Data;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using KaguyaProjectV2.KaguyaBot.Core.Interfaces;
using TwitchLib.Client.Events;

namespace KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries
{
    public static class ServerQueries
    {
        public static async Task<Server> GetOrCreateServerAsync(ulong Id)
        {
            using (var db = new KaguyaDb())
            {
                bool exists = db.Servers.Any(x => x.Id == Id);

                if (!exists)
                {
                    await db.InsertAsync(new Server
                    {
                        Id = Id
                    });
                }

                return await (db.Servers
                    .LoadWith(x => x.AntiRaid)
                    .LoadWith(x => x.AutoAssignedRoles)
                    .LoadWith(x => x.BlackListedChannels)
                    .LoadWith(x => x.FilteredPhrases)
                    .LoadWith(x => x.MutedUsers)
                    .LoadWith(x => x.ServerExp)
                    .LoadWith(x => x.WarnedUsers)
                    .LoadWith(x => x.MutedUsers)
                    .Where(s => s.Id == Id).FirstAsync());
            }
        }

        public static async Task UpdateServerAsync(Server server)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertOrReplaceAsync(server);
            }
        }

        public static void UpdateServers(IEnumerable<Server> servers)
        {
            using (var db = new KaguyaDb())
            {
                var options = new BulkCopyOptions
                {
                    BulkCopyType = BulkCopyType.ProviderSpecific
                };
                db.BulkCopy(options, servers);
            }
        }

        /// <summary>
        /// Adds a FilteredPhrase object to the database. Duplicates are skipped automatically.
        /// </summary>
        /// <param name="fpObject">FilteredPhrase object to add.</param>
        public static async Task AddFilteredPhrase(FilteredPhrase fpObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertAsync(fpObject);
            }
        }

        /// <summary>
        /// Removes a filtered phrase object from the database.
        /// </summary>
        /// <param name="fpObject">FilteredPhrase object to remove.</param>
        public static void RemoveFilteredPhrase(FilteredPhrase fpObject)
        {
            using (var db = new KaguyaDb())
            {
                db.Delete(fpObject);
            }
        }

        /// <summary>
        /// Adds a blacklisted channel object to the database.
        /// </summary>
        /// <param name="blObj">The BlackListedChannl object to add.</param>
        public static async Task AddBlacklistedChannel(BlackListedChannel blObj)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertOrReplaceAsync(blObj);
            }
        }

        /// <summary>
        /// Inserts every element of the collection into the database.
        /// </summary>
        /// <param name="blObjLst"></param>
        /// <returns></returns>
        public static async Task AddBlacklistedChannels(IEnumerable<BlackListedChannel> blObjLst)
        {
            using (var db = new KaguyaDb())
            {
                foreach (var obj in blObjLst)
                {
                    await db.InsertAsync(obj);
                }
            }
        }

        /// <summary>
        /// Removes a blacklisted channel from the server's list of blacklisted channels.
        /// </summary>
        /// <param name="blObj">The blacklisted channel object to remove from the database.</param>
        /// <returns></returns>
        public static async Task RemoveBlacklistedChannelAsync(BlackListedChannel blObj)
        {
            using (var db = new KaguyaDb())
            {
                await db.DeleteAsync(blObj);
            }
        }

        /// <summary>
        /// Deletes all currently blacklisted channels for a server.
        /// </summary>
        /// <param name="server">The server who's channel blacklist we are clearing.</param>
        /// <returns></returns>
        public static async Task ClearBlacklistedChannelsAsync(Server server)
        {
            using (var db = new KaguyaDb())
            {
                foreach (var channel in server.BlackListedChannels)
                {
                    await db.DeleteAsync(channel);
                }
            }
        }

        public static async Task AddAutoAssignedRole(AutoAssignedRole arObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertAsync(arObject);
            }
        }

        public static async Task RemoveAutoAssignedRole(AutoAssignedRole arObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.DeleteAsync(arObject);
            }
        }

        public static async Task<IEnumerable<MutedUser>> GetCurrentlyMutedUsers()
        {
            using (var db = new KaguyaDb())
            {
                return await (from m in db.MutedUsers
                    select m).ToListAsync();
            }
        }

        public static async Task AddMutedUser(MutedUser muObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertAsync(muObject);
            }
        }

        public static async Task RemoveMutedUser(MutedUser muObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.DeleteAsync(muObject);
            }
        }

        public static async Task<List<MutedUser>> GetMutedUsersForServer(ulong serverId)
        {
            using (var db = new KaguyaDb())
            {
                return await (from m in db.MutedUsers
                    where m.ServerId == serverId
                    select m).ToListAsync();
            }
        }
        /// <summary>
        /// Returns one MutedUser object for a specific individual in a specific guild.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="serverId"></param>
        /// <returns></returns>
        public static MutedUser GetSpecificMutedUser(ulong userId, ulong serverId)
        {
            using (var db = new KaguyaDb())
            {
                return GetOrCreateServerAsync(serverId).Result.MutedUsers.FirstOrDefault(x => x.UserId == userId);
            }
        }

        /// <summary>
        /// Replaces an existing muted user object with an updated MutedUser object.
        /// </summary>
        /// <param name="muObject">The object you want to send to the database as a replacement.</param>
        /// <returns></returns>
        public static async Task ReplaceMutedUser(MutedUser muObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.UpdateAsync(from m in db.MutedUsers
                    where m.ServerId == muObject.ServerId &&
                          m.UserId == muObject.UserId
                    select m);
            }
        }

        public static async Task AddOrReplaceWarnSettingAsync(WarnSetting wsObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertOrReplaceAsync(wsObject);
            }
        }

        public static async Task RemoveWarnSettingAsync(WarnSetting wsObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.DeleteAsync(wsObject);
            }
        }
        public static async Task<WarnSetting> GetWarnConfigForServerAsync(ulong Id)
        {
            using (var db = new KaguyaDb())
            {
                return await (from c in db.WarnActions
                    where c.ServerId == Id
                    select c).FirstOrDefaultAsync();
            }
        }

        public static async Task AddWarnedUserAsync(WarnedUser wuObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertAsync(wuObject);
            }
        }

        public static async Task RemoveWarnedUserAsync(WarnedUser wuObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.DeleteAsync(wuObject);
            }
        }

        public static async Task<List<WarnedUser>> GetWarningsForUserAsync(ulong serverId, ulong userId)
        {
            using (var db = new KaguyaDb())
            {
                return await (from w in db.WarnedUsers
                    where w.ServerId == serverId && w.UserId == userId
                    select w).ToListAsync();
            }
        }

        public static async Task AddTwitchChannelAsync(TwitchChannel tcObj)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertAsync(tcObj);
            }
        }

        public static async Task RemoveTwitchChannel(TwitchChannel tcObj)
        {
            using (var db = new KaguyaDb())
            {
                await db.DeleteAsync(tcObj);
            }
        }

        public static async Task<List<TwitchChannel>> GetTwitchChannelsForServer(ulong serverId)
        {
            using (var db = new KaguyaDb())
            {
                return await (from t in db.TwitchChannels
                    where t.ServerId == serverId
                    select t).ToListAsync();
            }
        }

        public static async Task AddServerSpecificExpForUser(ServerExp expObj)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertAsync(expObj);
            }
        }

        /// <summary>
        /// Replaces the old EXP object with a new, updated one. This
        /// will remove and replace based on the userId and serverId
        /// properties of each object. This is used specifically for
        /// the server-specific EXP table. This does not affect global EXP.
        /// </summary>
        /// <param name="newExpObj">The object to insert, the updated version of oldExpObj.</param>
        /// <returns></returns>
        public static async Task UpdateServerExp(ServerExp newExpObj)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertOrReplaceAsync(newExpObj);
            }
        }

        public static async Task RemoveServerExp(ServerExp expObj)
        {
            using (var db = new KaguyaDb())
            {
                await db.DeleteAsync(expObj);
            }
        }

        /// <summary>
        /// Returns what rank the user is on the server-specific EXP "leaderboard". If they are
        /// the highest EXP holder, this value would be 1.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static int GetServerExpRankForUser(Server server, User user)
        {
            using (var db = new KaguyaDb())
            {
                return server.ServerExp.OrderByDescending(x => x.Exp).ToList().FindIndex(x => x.UserId == user.Id) + 1;
            }
        }

        /// <summary>
        /// Adds a new praise object to the database.
        /// </summary>
        /// <param name="praiseObj"></param>
        /// <returns></returns>
        public static async Task AddPraiseAsync(Praise praiseObj)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertAsync(praiseObj);
            }
        }

        public static async Task RemovePraiseAsync(Praise praiseObj)
        {
            using (var db = new KaguyaDb())
            {
                await db.DeleteAsync(praiseObj);
            }
        }

        public static async Task<List<Praise>> GetPraiseAsync(ulong userId, ulong serverId)
        {
            using (var db = new KaguyaDb())
            {
                return await (from r in db.Praise
                    where r.UserId == userId && r.ServerId == serverId
                    select r).ToListAsync();
            }
        }

        public static double GetLastPraiseTime(ulong userId, ulong serverId)
        {
            using (var db = new KaguyaDb())
            {
                return (from r in db.Praise.OrderByDescending(x => x.TimeGiven)
                    where r.GivenBy == userId && r.ServerId == serverId
                    select r).First()?.TimeGiven ?? 0;
            }
        }

        public static async Task AddAntiRaidAsync(AntiRaidConfig arObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertAsync(arObject);
            }
        }

        public static async Task RemoveAntiRaidAsync(AntiRaidConfig arObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.DeleteAsync(arObject);
            }
        }

        /// <summary>
        /// Removes the AntiRaid object for this server from the database, if it exists.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static async Task RemoveAntiRaidAsync(Server server)
        {
            var arObject = server.AntiRaid?.ToList().FirstOrDefault();

            using (var db = new KaguyaDb())
            {
                if (arObject != null)
                {
                    await db.DeleteAsync(arObject);
                }
            }
        }

        public static async Task UpdateAntiRaidAsync(AntiRaidConfig arObject)
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertOrReplaceAsync(arObject);
            }
        }

        /// <summary>
        /// Inserts the <see cref="IKaguyaQueryable{T}"/> object into the database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static async Task InsertOrReplaceAsync<T>(T arg) where T : class, IKaguyaQueryable<T>, IKaguyaUnique<T>
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertOrReplaceAsync(arg);
            }
        }

        /// <summary>
        /// Inerts a new <see cref="IKaguyaQueryable{T}"/> object into the database. Do not use if wanting to
        /// update.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static async Task InsertAsync<T>(T arg) where T : class, IKaguyaQueryable<T>
        {
            using (var db = new KaguyaDb())
            {
                await db.InsertAsync(arg);
            }
        }

        /// <summary>
        /// Returns a <see cref="List{T}"/> of objects that belongs to the user, but is not necessarily
        /// mapped to the user object directly. <see cref="searchable"/> refers to the object in the database that
        /// we want to retreive, for example, someone's fish or something else that belongs to them.
        /// </summary>
        /// <typeparam name="T">The <see cref="searchable"/> object that we are looking for. Must inherit from
        /// <see cref="IKaguyaQueryable{T}"/>, <see cref="IKaguyaUnique{T}"/> and <see cref="IUserSearchable{T}"/></typeparam>
        /// <param name="searchable"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task<List<T>> FindCollectionForUserAsync<T>(T searchable, User user) 
            where T : class, 
            IKaguyaQueryable<T>, 
            IKaguyaUnique<T>, 
            IUserSearchable<T>
        {
            using (var db = new KaguyaDb())
            {
                return await (from t in db.GetTable<T>()
                    where t.UserId == user.Id
                    select t).ToListAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns a <see cref="T"/> object that belongs to the user, but is not necessarily
        /// mapped to the user object directly. <see cref="searchable"/> refers to the object in the database that
        /// we want to retreive, for example, someone's fish or something else that belongs to them.
        /// </summary>
        /// <typeparam name="T">The <see cref="searchable"/> object that we are looking for. Must inherit from
        /// <see cref="IKaguyaQueryable{T}"/>, <see cref="IKaguyaUnique{T}"/> and <see cref="IUserSearchable{T}"/></typeparam>
        /// <param name="searchable">The <see cref="T"/> object to search for.</param>
        /// <param name="user">The user whom we are retreiving the <see cref="T"/> for.</param>
        /// <returns></returns>
        public static async Task<T> FindForUserAsync<T>(T searchable, ulong userId) where T : 
            class,
            IKaguyaQueryable<T>,
            IKaguyaUnique<T>,
            IUserSearchable<T>
        {
            using (var db = new KaguyaDb())
            {
                return await (from t in db.GetTable<T>()
                    where t.UserId == userId
                    select t).FirstOrDefaultAsync();
            }
        }

        public static async Task<T> FindForServerAsync<T>(T searchable, ulong serverId) where T :
            class,
            IKaguyaQueryable<T>,
            IKaguyaUnique<T>,
            IServerSearchable<T>
        {
            using (var db = new KaguyaDb())
            {
                return await (from t in db.GetTable<T>()
                    where t.ServerId == serverId
                    select t).FirstOrDefaultAsync();
            }
        }

        // TODO: Add summaries and do the same for IEnumerable<T> and User-related queries!
        public static async Task<T> FindForServerAsync<T>(T searchable, Server server) where T :
        class,
        IKaguyaQueryable<T>,
        IKaguyaUnique<T>,
        IServerSearchable<T>
        {
            using (var db = new KaguyaDb())
            {
                return await (from t in db.GetTable<T>()
                    where t.ServerId == server.Id
                    select t).FirstOrDefaultAsync();
            }
        }

        public static Task BulkCopy<T>(IEnumerable<T> args) where T : 
            class, 
            IKaguyaQueryable<T>,
            IKaguyaUnique<T>
        {
            using (var db = new KaguyaDb())
            {
                db.BulkCopy(args);
            }
            return Task.CompletedTask;
        }

    }
}
