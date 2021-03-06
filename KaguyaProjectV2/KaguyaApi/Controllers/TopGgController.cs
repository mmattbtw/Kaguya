﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KaguyaProjectV2.KaguyaApi.Database.Context;
using KaguyaProjectV2.KaguyaApi.Database.Models;
using KaguyaProjectV2.KaguyaBot.Core.Extensions;
using KaguyaProjectV2.KaguyaBot.Core.Handlers.TopGG;
using KaguyaProjectV2.KaguyaBot.Core.Interfaces;
using KaguyaProjectV2.KaguyaBot.Core.Services.ConsoleLogServices;
using KaguyaProjectV2.KaguyaBot.DataStorage.JsonStorage;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KaguyaProjectV2.KaguyaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TopGgController : ControllerBase
    {
        private readonly IBotConfig _cfg;
        private readonly KaguyaDb _db;
        private readonly UpvoteNotifier _uvNotifier;

        public TopGgController(IBotConfig cfg, KaguyaDb db, UpvoteNotifier uvNotifier)
        {
            _cfg = cfg;
            _db = db;
            _uvNotifier = uvNotifier;
        }

        // POST api/<controller>
        [HttpPost("webhook")]
        public async Task Post([FromBody] TopGgWebhook baseHook, [FromHeader(Name = "Authorization")] string auth)
        {
            if (auth != _cfg.TopGgApiKey)
                return;

            var dbWebhook = new DatabaseUpvoteWebhook
            {
                BotId = baseHook.BotId.AsUlong(),
                UserId = baseHook.UserId.AsUlong(),
                UpvoteType = baseHook.Type,
                IsWeekend = baseHook.IsWeekend,
                QueryParams = baseHook.Query,
                TimeVoted = DateTime.Now.ToOADate(),
                VoteId = Guid.NewGuid().ToString()
            };

            try
            {
                await _db.InsertAsync(dbWebhook);
                _uvNotifier.Enqueue(dbWebhook);
            }
            catch (Exception e)
            {
                await ConsoleLogger.LogAsync(e, "An error occurred when trying to insert a Top.GG authorized " +
                                                $"webhook for user {dbWebhook.UserId}.");
                return;
            }
            

            await ConsoleLogger.LogAsync($"[Kaguya Api]: Authorized Top.GG Webhook received for user {dbWebhook.UserId}.", LogLvl.INFO);
        }
    }
}