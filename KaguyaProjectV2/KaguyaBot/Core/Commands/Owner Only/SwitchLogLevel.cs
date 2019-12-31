﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using KaguyaProjectV2.KaguyaBot.Core.Attributes;
using KaguyaProjectV2.KaguyaBot.Core.Global;
using KaguyaProjectV2.KaguyaBot.Core.KaguyaEmbed;
using KaguyaProjectV2.KaguyaBot.DataStorage.JsonStorage;

namespace KaguyaProjectV2.KaguyaBot.Core.Commands.Owner_Only
{
    public class SwitchLogLevel : ModuleBase<ShardedCommandContext>
    {
        [OwnerCommand]
        [Command("swaploglevel")]
        [Alias("swaplog", "sll")]
        [Summary("Switches the current console log severity to the specified severity. " +
                 "Valid severities are `Trace, Debug, Info, Warn, Error`")]
        [Remarks("<severity>")]
        public async Task SwapLogLevel(string level)
        {
            var curLog = ConfigProperties.logLevel;

            string validSeverities = "Trace, Debug, Info, Warn, Error";

            switch (level.ToLower())
            {
                case "trace": ConfigProperties.logLevel = LogLvl.TRACE; break;
                case "debug": ConfigProperties.logLevel = LogLvl.DEBUG; break;
                case "info": ConfigProperties.logLevel = LogLvl.INFO; break;
                case "warn": ConfigProperties.logLevel = LogLvl.WARN; break;
                case "error": ConfigProperties.logLevel = LogLvl.ERROR; break;
                default: throw new ArgumentOutOfRangeException($"Valid logtypes are `{validSeverities}`", new Exception());
            }

            var embed = new KaguyaEmbedBuilder
            {
                Description = $"Successfully changed LogLevel from `{curLog.Humanize()}` to `{ConfigProperties.logLevel.Humanize()}`",
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Note: This loglevel will return back to `{curLog.Humanize()}` after a restart."
                }
            };
            embed.SetColor(EmbedColor.VIOLET);

            await ReplyAsync(embed: embed.Build());
        }
    }
}
