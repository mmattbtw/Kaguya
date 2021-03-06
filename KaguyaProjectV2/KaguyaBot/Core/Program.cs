﻿using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using DiscordBotsList.Api;
using KaguyaProjectV2.KaguyaApi;
using KaguyaProjectV2.KaguyaBot.Core.Application;
using KaguyaProjectV2.KaguyaBot.Core.Configurations;
using KaguyaProjectV2.KaguyaBot.Core.Constants;
using KaguyaProjectV2.KaguyaBot.Core.Global;
using KaguyaProjectV2.KaguyaBot.Core.Handlers;
using KaguyaProjectV2.KaguyaBot.Core.Handlers.KaguyaPremium;
using KaguyaProjectV2.KaguyaBot.Core.Handlers.TopGG;
using KaguyaProjectV2.KaguyaBot.Core.Interfaces;
using KaguyaProjectV2.KaguyaBot.Core.Osu;
using KaguyaProjectV2.KaguyaBot.Core.Services;
using KaguyaProjectV2.KaguyaBot.Core.Services.ConsoleLogServices;
using KaguyaProjectV2.KaguyaBot.Core.Services.OwnerGiveawayServices;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Context;
using KaguyaProjectV2.KaguyaBot.DataStorage.DbData.Queries;
using KaguyaProjectV2.KaguyaBot.DataStorage.JsonStorage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OsuSharp;
using TwitchLib.Api;
using Victoria;

// DO NOT ORGANIZE IMPORTS IN DEBUG MODE! YOU WILL BREAK RELEASE MODE!

namespace KaguyaProjectV2.KaguyaBot.Core
{
    internal class Program
    {
        private static IBotConfig _botConfig;
        private TwitchAPI _api;
        private DiscordShardedClient _client;
        private LavaNode _lavaNode;

        private static async Task Main(string[] args)
        {
            _botConfig = await Config.GetOrCreateConfigAsync(args);

            /*
             * This portion requires that appsettings.json is properly configured.
             * appsettings.json is database configuration information, which for task1
             * is only necessary for posting Top.GG webhook notifications to the
             * kaguya database. We don't need this during a debug session.
             */

            Task task2 = new Program().MainAsync(args);
            Task task1 = CreateHostBuilder(args).Build().RunAsync();
            
            Task.WaitAll(task1, task2);
            Task.WaitAll(task2);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
                                                                           .ConfigureWebHostDefaults(webBuilder =>
                                                                           {
                                                                               webBuilder.UseStartup<Startup>();
                                                                               webBuilder.UseUrls($"http://+:{_botConfig.TopGgWebhookPort}");
                                                                               webBuilder.UseKestrel();
                                                                           })
                                                                           .ConfigureLogging(x => x.ClearProviders());
        public async Task MainAsync(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += async (sender, eventArgs) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                await ConsoleLogger.LogAsync($"Unhandled Exception: {(Exception) eventArgs.ExceptionObject}\n" +
                                             $"Inner Exception: {((Exception) eventArgs.ExceptionObject).InnerException}", LogLvl.ERROR);
            };

            var config = new DiscordSocketConfig
            {
                MessageCacheSize = 200,
                AlwaysDownloadUsers = true,
#if DEBUG
                TotalShards = 1
#else
                TotalShards = 5
#endif
            };

            var lavaConfig = new LavaConfig
            {
                EnableResume = true,
                LogSeverity = LogSeverity.Verbose,
                ReconnectAttempts = 10,
                SelfDeaf = true
            };

            _client = new DiscordShardedClient(config);
            _lavaNode = new LavaNode(_client, lavaConfig);

            await SetupKaguya();
            using (ServiceProvider services = new SetupServices().ConfigureServices(config, _client))
            {
                try
                {
                    GlobalPropertySetup(_botConfig);

                    SetupTwitch();

                    DiscordEventLogger.InitLogger();
                    GuildLogger.InitializeGuildLogListener();

                    await TestDatabaseConnection();

                    _client = services.GetRequiredService<DiscordShardedClient>();
                    await services.GetRequiredService<CommandHandler>().InitializeAsync();

                    await _client.LoginAsync(TokenType.Bot, _botConfig.Token);
                    await _client.StartAsync();

                    await _client.SetGameAsync($"v{ConfigProperties.Version}: Booting up!");
                    _client.ShardReady += async c => { await _lavaNode.ConnectAsync(); };

                    await InitializeTimers(AllShardsLoggedIn(_client, config));

                    _lavaNode.OnLog += async message => { await ConsoleLogger.LogAsync("[Kaguya Music]: " + message.Message, LogLvl.INFO); };

                    InitializeEventHandlers();

                    if (AllShardsLoggedIn(_client, config))
                    {
                        ConfigProperties.LavaNode = _lavaNode;
                        OsuBase.Client = new OsuClient(new OsuSharpConfiguration
                        {
                            ApiKey = _botConfig.OsuApiKey
                        });
                    }

                    // Keep the app running.
                    await Task.Delay(-1);
                }
                catch (HttpException e)
                {
                    await ConsoleLogger.LogAsync("Error when logging into Discord:\n" +
                                                 "-Have you passed in the correct application arguments?\n" +
                                                 $"-Is your token correct? Exception: {e.Message}", LogLvl.ERROR);

                    Console.ReadLine();
                }
                catch (Exception e)
                {
                    await ConsoleLogger.LogAsync("Something really important broke!\n" +
                                                 $"Exception: {e.Message}\n" +
                                                 $"More information: {e}", LogLvl.ERROR);
                }
            }
        }

        public async Task SetupKaguya()
        {
            await ConsoleLogger.LogAsync($"========== KaguyaBot Version {ConfigProperties.Version} ==========",
                LogLvl.INFO, ConsoleColor.Cyan, false, false);

            await CopyDependencies();
        }

        private async Task CopyDependencies()
        {
            await ConsoleLogger.LogAsync("Beginning copy of external dependencies...", LogLvl.INFO);
            
            Assembly asm = Assembly.GetExecutingAssembly();
            
            string rootDir = FileConstants.RootDir;
            string execDir = Path.GetDirectoryName(asm.Location);
            var dirInfo = new DirectoryInfo(Path.Combine(rootDir, "ExternalDependencies"));

            foreach (FileInfo file in dirInfo.GetFiles())
            {
                string cmbPath = Path.Combine(execDir, file.Name);

                if (File.Exists(cmbPath))
                {
                    // Can't copy a file if it already exists. So, delete it.
                    File.Delete(cmbPath);
                }
                
                File.Copy(file.FullName, cmbPath);
                await ConsoleLogger.LogAsync($"Dependency {file.Name} copied to {cmbPath}", LogLvl.DEBUG);
            }

            await ConsoleLogger.LogAsync("Dependencies successfully copied.", LogLvl.INFO);
        }

        private void GlobalPropertySetup(IBotConfig botConfig)
        {
            ConfigProperties.Client = _client;
            ConfigProperties.BotConfig = botConfig;
            ConfigProperties.LogLevel = (LogLvl) botConfig.LogLevelNumber;
            ConfigProperties.TopGgApi = new AuthDiscordBotListApi(538910393918160916, botConfig.TopGgApiKey);
        }

        private async Task TestDatabaseConnection()
        {
            try
            {
                var _ = new Init();
                if (await DatabaseQueries.TestConnectionAsync())
                    await ConsoleLogger.LogAsync("Database connection successfully established.", LogLvl.INFO);
            }
            catch (Exception e)
            {
                await ConsoleLogger.LogAsync("Failed to establish database connection. " +
                                             "Have you properly configured your config file? " +
                                             $"Exception: {e.Message}", LogLvl.ERROR);
            }
        }

        private void SetupTwitch()
        {
            _api = new TwitchAPI();
            _api.Settings.ClientId = ConfigProperties.BotConfig.TwitchClientId;
            _api.Settings.AccessToken = ConfigProperties.BotConfig.TwitchAuthToken;
            ConfigProperties.TwitchApi = _api;
        }

        private async Task InitializeTimers(bool allShardsLoggedIn)
        {
            if (!allShardsLoggedIn) return;

            // We need to load the cache on program start.
            await MemoryCache.Initialize();
            await ConsoleLogger.LogAsync("Memory Cache timer initialized", LogLvl.INFO);
            await KaguyaStatsFactory.Initialize();
            await ConsoleLogger.LogAsync("Kaguya stats updater initialized", LogLvl.INFO);
            await AntiRaidService.Initialize();
            await ConsoleLogger.LogAsync("Antiraid service initialized", LogLvl.INFO);
            await AutoUnmuteHandler.Initialize();
            await ConsoleLogger.LogAsync("Unmute handler initialized", LogLvl.INFO);
#if !DEBUG
            await OwnerGiveawayMessageUpdaterService.Initialize();
            await ConsoleLogger.LogAsync("Owner giveaway message updater initialized", LogLvl.INFO);
            await KaguyaPremiumRoleHandler.Initialize();
            await ConsoleLogger.LogAsync("Kaguya Premium role handler initialized", LogLvl.INFO);
            await KaguyaPremiumExpirationHandler.Initialize();
            await ConsoleLogger.LogAsync("Kaguya Premium expiration handler initialized", LogLvl.INFO);
            await RateLimitService.Initialize();
            await ConsoleLogger.LogAsync("Ratelimit service initialized", LogLvl.INFO);
            await StatsUpdater.Initialize();
            await ConsoleLogger.LogAsync("Top.gg stats updater initialized", LogLvl.INFO);
            
            await RemindService.Initialize();
            await ConsoleLogger.LogAsync("Remind service initialized", LogLvl.INFO);
            await UpvoteExpirationNotifier.Initialize();
            await ConsoleLogger.LogAsync("Upvote expiration notification timer initialized", LogLvl.INFO);
            await GameRotationService.Initialize();
            await ConsoleLogger.LogAsync("Game rotation timer initialized", LogLvl.INFO);
#endif
            await ConsoleLogger.LogAsync("All timers initialized.", LogLvl.INFO);
        }

        private void InitializeEventHandlers()
        {
            var reactionHandler = new ReactionRoleHandler();

            KaguyaEvents.OnWarn += WarnHandler.OnWarn;
            KaguyaEvents.OnFish += async args => await FishHandler.OnFish(args);

            _client.UserJoined += GreetingService.Trigger;
            _client.UserJoined += AutoAssignedRoleHandler.Trigger;
            _client.JoinedGuild += NewOwnerNotificationService.Trigger;

            _client.ReactionAdded += OwnerGiveawayReactionService.ReactionAdded;

            _client.ReactionAdded += (cache, ch, e) => reactionHandler.ReactionChanged(cache, ch, e, true);
            _client.ReactionRemoved += (cache, ch, e) => reactionHandler.ReactionChanged(cache, ch, e, false);

            _lavaNode.OnTrackEnded += MusicService.OnTrackEnd;
        }

        private bool AllShardsLoggedIn(DiscordShardedClient client, DiscordSocketConfig config) => client.Shards.Count == config.TotalShards;
    }
}